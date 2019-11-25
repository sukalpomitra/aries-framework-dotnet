﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Credentials;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.PoolApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osma.Mobile.App.ViewModels.Credentials;

namespace AgentFramework.Core.Runtime
{
    /// <summary>
    /// Proof Service
    /// </summary>
    /// <seealso cref="AgentFramework.Core.Contracts.IProofService" />
    public class DefaultProofService : IProofService
    {
        /// <summary>
        /// The event aggregator
        /// </summary>
        protected readonly IEventAggregator EventAggregator;

        /// <summary>
        /// The connection service
        /// </summary>
        protected readonly IConnectionService ConnectionService;

        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;

        /// <summary>
        /// The provisioning service
        /// </summary>
        protected readonly IProvisioningService ProvisioningService;

        /// <summary>
        /// The ledger service
        /// </summary>
        protected readonly ILedgerService LedgerService;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<DefaultProofService> Logger;

        /// <summary>
        /// The tails service
        /// </summary>
        protected readonly ITailsService TailsService;

        /// <summary>
        /// The messageservice service
        /// </summary>
        protected readonly IMessageService MessageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultProofService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="recordService">The record service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="ledgerService">The ledger service.</param>
        /// <param name="tailsService">The tails service.</param>
        /// <param name="messageService">The message service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultProofService(
            IEventAggregator eventAggregator,
            IConnectionService connectionService,
            IWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILedgerService ledgerService,
            ITailsService tailsService,
            IMessageService messageService,
            ILogger<DefaultProofService> logger)
        {
            EventAggregator = eventAggregator;
            TailsService = tailsService;
            MessageService = messageService;
            ConnectionService = connectionService;
            RecordService = recordService;
            ProvisioningService = provisioningService;
            LedgerService = ledgerService;
            Logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task<(ProofRequestMessage, ProofRecord)> CreateProofRequestAsync(
            IAgentContext agentContext, ProofRequest proofRequest,
            string connectionId)
        {
            if (string.IsNullOrWhiteSpace(proofRequest.Nonce))
                throw new ArgumentNullException(nameof(proofRequest.Nonce), "Nonce must be set.");

            return await CreateProofRequestAsync(agentContext, proofRequest.ToJson(), connectionId);
        }

        /// <inheritdoc />
        public virtual async Task<(ProofRequestMessage, ProofRecord)> CreateProofRequestAsync(
            IAgentContext agentContext, string proofRequestJson,
            string connectionId = null)
        {
            Logger.LogInformation(LoggingEvents.CreateProofRequest, "ConnectionId {0}", connectionId);

            var threadId = Guid.NewGuid().ToString();

            if (connectionId != null)
            {
                var connection = await ConnectionService.GetAsync(agentContext, connectionId);

                if (connection.State != ConnectionState.Connected)
                    throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                        $"Connection state was invalid. Expected '{ConnectionState.Connected}', found '{connection.State}'");
            }

            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                RequestJson = proofRequestJson
            };
            proofRecord.SetTag(TagConstants.Role, TagConstants.Requestor);
            proofRecord.SetTag(TagConstants.LastThreadId, threadId);

            await RecordService.AddAsync(agentContext.Wallet, proofRecord);

            return (new ProofRequestMessage {Id = threadId, ProofRequestJson = proofRequestJson}, proofRecord);
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessProofAsync(IAgentContext agentContext, ProofMessage proof)
        {
            var proofJson = proof.ProofJson;

            var proofRecord = await this.GetByThreadIdAsync(agentContext, proof.GetThreadId());

            if (proofRecord.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof state was invalid. Expected '{ProofState.Requested}', found '{proofRecord.State}'");

            proofRecord.ProofJson = proofJson;
            await proofRecord.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(agentContext.Wallet, proofRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = proofRecord.Id,
                MessageType = proof.Type,
                ThreadId = proof.GetThreadId()
            });

            return proofRecord.Id;
        }

        /// <inheritdoc />
        public virtual async Task<string> ProcessProofRequestAsync(IAgentContext agentContext,
            ProofRequestMessage proofRequest, ConnectionRecord connection, bool isVcOidc)
        {
            var requestJson = proofRequest.ProofRequestJson;

            
            // Write offer record to local wallet
            var proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestJson = requestJson,
                ConnectionId = isVcOidc ? null : connection.Id,
                State = ProofState.Requested
            };
            proofRecord.SetTag(TagConstants.LastThreadId, proofRequest.GetThreadId());
            proofRecord.SetTag(TagConstants.Role, TagConstants.Holder);

            if (!connection.Sso && !isVcOidc)
            {
                await RecordService.AddAsync(agentContext.Wallet, proofRecord);

                EventAggregator.Publish(new ServiceMessageProcessingEvent
                {
                    RecordId = proofRecord.Id,
                    MessageType = proofRequest.Type,
                    ThreadId = proofRequest.GetThreadId()
                });
            } else
            {
                if (isVcOidc)
                {
                    connection.Endpoint = new AgentEndpoint { Uri = proofRequest.ServiceDecorator.ServiceEndpoint?.ToString() };
                }
                await SelectCredentialsForProofAsync(agentContext, proofRecord, connection);
            }

            return proofRecord.Id;
        }

        private async Task SelectCredentialsForProofAsync(IAgentContext agentContext, ProofRecord proof, ConnectionRecord connection)
        {
            var requestJson = (JObject)JsonConvert.DeserializeObject(proof.RequestJson);
            JObject _requestedAttributes = (JObject)requestJson["requested_attributes"];
            JObject _requestedPredicates = (JObject)requestJson["requested_predicates"];
            IList<string> _requestedAttributesKeys = _requestedAttributes?.Properties().Select(p => p.Name).ToList();
            IList<string> _requestedPredicatesKeys = _requestedPredicates?.Properties().Select(p => p.Name).ToList();
            JToken cred_def_id = null;
            try
            {
                cred_def_id = _requestedAttributes[_requestedAttributesKeys[0]]["restrictions"][0]["cred_def_id"];
            }
            catch (Exception)
            {
                cred_def_id = null;
            }
            var credentials = new List<CredentialRecord>();
            if (cred_def_id != null)
            {
                credentials = await RecordService.SearchAsync<CredentialRecord>(agentContext.Wallet,
                    SearchQuery.And(SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Issued.ToString("G")),
                    SearchQuery.Equal(nameof(CredentialRecord.CredentialDefinitionId), cred_def_id.ToString())), null, 100);
            } else
            {
                credentials = await RecordService.SearchAsync<CredentialRecord>(agentContext.Wallet,
                    SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Issued.ToString("G")), null, 100);
            }
            bool credentialFound = false;
            if (credentials.Count > 0)
            {
                Dictionary<string, RequestedAttribute> requestedAttributes = new Dictionary<string, RequestedAttribute>();
                Dictionary<string, RequestedAttribute> requestedPredicates = new Dictionary<string, RequestedAttribute>();
                foreach (var credential in credentials)
                {
                    if (!credentialFound)
                    {
                        IEnumerable<CredentialAttribute> Attributes = credential.CredentialAttributesValues
                        .Select(p =>
                            new CredentialAttribute()
                            {
                                Name = p.Name,
                                Value = p.Value?.ToString(),
                                Type = "Text"
                            })
                        .ToList();


                        foreach (var item in _requestedAttributesKeys)
                        {
                            foreach (var attrib in Attributes)
                            {
                                if (_requestedAttributes[item]["name"].ToString() == attrib.Name)
                                {
                                    RequestedAttribute requestedAttribute = new RequestedAttribute();
                                    requestedAttribute.CredentialId = credential.CredentialId;
                                    requestedAttribute.Revealed = true;
                                    requestedAttribute.Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
                                    requestedAttributes.Add(item, requestedAttribute);
                                    credentialFound = true;
                                }
                            }
                            if (!credentialFound)
                            {
                                requestedAttributes.Clear();
                            }
                        }

                        foreach (var item in _requestedPredicatesKeys)
                        {
                            RequestedAttribute requestedAttribute = new RequestedAttribute();
                            requestedAttribute.CredentialId = credential.CredentialId;
                            requestedAttribute.Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
                            requestedPredicates.Add(item, requestedAttribute);
                        }
                    }
                }

                if (credentialFound)
                {
                    RequestedCredentials requestedCredentials = new RequestedCredentials();
                    requestedCredentials.RequestedAttributes = requestedAttributes;
                    requestedCredentials.RequestedPredicates = requestedPredicates;
                    var proofJson = await CreateProofJsonAsync(agentContext, requestedCredentials, proof.RequestJson);
                    var threadId = proof.GetTag(TagConstants.LastThreadId);

                    var proofMsg = new ProofMessage
                    {
                        ProofJson = proofJson
                    };

                    proofMsg.ThreadFrom(threadId);
                    await MessageService.SendAsync(agentContext.Wallet, proofMsg, connection);
                }
            }
        }

        /// <inheritdoc />
        public virtual async Task<(ProofMessage, ConnectionRecord)> CreateProofAsync(IAgentContext agentContext, 
            string proofRequestId, RequestedCredentials requestedCredentials)
        {
            var record = await GetAsync(agentContext, proofRequestId);
            var connection = await ConnectionService.GetAsync(agentContext, record.ConnectionId);

            if (record.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof state was invalid. Expected '{ProofState.Requested}', found '{record.State}'");

            var proofJson = await CreateProofJsonAsync(agentContext, requestedCredentials, record.RequestJson);
            if (proofJson.Contains("\"rev_reg_id\":null"))
            {
                String[] separator = { "\"rev_reg_id\":null" };
                String[] proofJsonList = proofJson.Split(separator, StringSplitOptions.None);
                proofJson = proofJsonList[0] + "\"rev_reg_id\":null,\"timestamp\":null}]}";
            }
            record.ProofJson = proofJson;
            await record.TriggerAsync(ProofTrigger.Accept);
            await RecordService.UpdateAsync(agentContext.Wallet, record);

            var threadId = record.GetTag(TagConstants.LastThreadId);

            var proofMsg = new ProofMessage
            {
                ProofJson = proofJson
            };

            proofMsg.ThreadFrom(threadId);

            return (proofMsg, connection);
        }

        private async Task<string> CreateProofJsonAsync(IAgentContext agentContext,
            RequestedCredentials requestedCredentials, string requestJson)
        {
            var provisioningRecord = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            var credentialObjects = new List<CredentialInfo>();
            foreach (var credId in requestedCredentials.GetCredentialIdentifiers())
            {
                credentialObjects.Add(
                    JsonConvert.DeserializeObject<CredentialInfo>(
                        await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credId)));
            }

            var schemas = await BuildSchemasAsync(await agentContext.Pool,
                credentialObjects
                    .Select(x => x.SchemaId)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(await agentContext.Pool,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct());

            var revocationStates = await BuildRevocationStatesAsync(await agentContext.Pool,
                credentialObjects,
                requestedCredentials);

            var proofJson = await AnonCreds.ProverCreateProofAsync(agentContext.Wallet, requestJson,
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            return proofJson;
        }

        /// <inheritdoc />
        public virtual async Task<string> CreateProofAsync(IAgentContext agentContext,
            ProofRequest proofRequest, RequestedCredentials requestedCredentials)
        {
            var provisioningRecord = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            var credentialObjects = new List<CredentialInfo>();
            foreach (var credId in requestedCredentials.GetCredentialIdentifiers())
            {
                credentialObjects.Add(
                    JsonConvert.DeserializeObject<CredentialInfo>(
                        await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credId)));
            }

            var schemas = await BuildSchemasAsync(await agentContext.Pool,
                credentialObjects
                    .Select(x => x.SchemaId)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(await agentContext.Pool,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct());

            var revocationStates = await BuildRevocationStatesAsync(await agentContext.Pool,
                credentialObjects,
                requestedCredentials);

            var proofJson = await AnonCreds.ProverCreateProofAsync(agentContext.Wallet, proofRequest.ToJson(),
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            return proofJson;
        }

        /// <inheritdoc />
        public virtual async Task RejectProofRequestAsync(IAgentContext agentContext, string proofRequestId)
        {
            var request = await GetAsync(agentContext, proofRequestId);

            if (request.State != ProofState.Requested)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof record state was invalid. Expected '{ProofState.Requested}', found '{request.State}'");

            await request.TriggerAsync(ProofTrigger.Reject);
            await RecordService.UpdateAsync(agentContext.Wallet, request);
        }

        /// <inheritdoc />
        public virtual async Task<bool> VerifyProofAsync(IAgentContext agentContext, string proofRequestJson, string proofJson)
        {
            var proof = JsonConvert.DeserializeObject<PartialProof>(proofJson);

            var schemas = await BuildSchemasAsync(await agentContext.Pool,
                proof.Identifiers
                    .Select(x => x.SchemaId)
                    .Where(x => x != null)
                    .Distinct());

            var definitions = await BuildCredentialDefinitionsAsync(await agentContext.Pool,
                proof.Identifiers
                    .Select(x => x.CredentialDefintionId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationDefinitions = await BuildRevocationRegistryDefinitionsAsync(await agentContext.Pool,
                proof.Identifiers
                    .Select(x => x.RevocationRegistryId)
                    .Where(x => x != null)
                    .Distinct());

            var revocationRegistries = await BuildRevocationRegistryDetlasAsync(await agentContext.Pool,
                proof.Identifiers
                    .Where(x => x.RevocationRegistryId != null));

            return await AnonCreds.VerifierVerifyProofAsync(proofRequestJson, proofJson, schemas,
                definitions, revocationDefinitions, revocationRegistries);
        }

        /// <inheritdoc />
        public virtual async Task<bool> VerifyProofAsync(IAgentContext agentContext, string proofRecId)
        {
            var proofRecord = await GetAsync(agentContext, proofRecId);

            if (proofRecord.State != ProofState.Accepted)
                throw new AgentFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof record state was invalid. Expected '{ProofState.Accepted}', found '{proofRecord.State}'");

            return await VerifyProofAsync(agentContext, proofRecord.RequestJson, proofRecord.ProofJson);
        }

        /// <inheritdoc />
        public virtual Task<List<ProofRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null,
            int count = 100) => RecordService.SearchAsync<ProofRecord>(agentContext.Wallet, query, null, count);

        /// <inheritdoc />
        public virtual async Task<ProofRecord> GetAsync(IAgentContext agentContext, string proofRecId)
        {
            Logger.LogInformation(LoggingEvents.GetProofRecord, "ProofRecordId {0}", proofRecId);

            return await RecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRecId) ??
                   throw new AgentFrameworkException(ErrorCode.RecordNotFound, "Proof record not found");
        }

        /// <inheritdoc />
        public virtual async Task<List<Credential>> ListCredentialsForProofRequestAsync(IAgentContext agentContext,
            ProofRequest proofRequest, string attributeReferent)
        {
            using (var search =
                await AnonCreds.ProverSearchCredentialsForProofRequestAsync(agentContext.Wallet, proofRequest.ToJson()))
            {
                var searchResult = await search.NextAsync(attributeReferent, 100);
                return JsonConvert.DeserializeObject<List<Credential>>(searchResult);
            }
        }

        #region Private Methods

        private async Task<string> BuildSchemasAsync(Pool pool, IEnumerable<string> schemaIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in schemaIds)
            {
                var ledgerSchema = await LedgerService.LookupSchemaAsync(pool, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildCredentialDefinitionsAsync(Pool pool, IEnumerable<string> credentialDefIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var schemaId in credentialDefIds)
            {
                var ledgerDefinition = await LedgerService.LookupDefinitionAsync(pool, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerDefinition.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationStatesAsync(Pool pool,
            IEnumerable<CredentialInfo> credentialObjects,
            RequestedCredentials requestedCredentials)
        {
            var allCredentials = new List<RequestedAttribute>();
            allCredentials.AddRange(requestedCredentials.RequestedAttributes.Values);
            allCredentials.AddRange(requestedCredentials.RequestedPredicates.Values);

            var result = new Dictionary<string, Dictionary<string, JObject>>();
            foreach (var requestedCredential in allCredentials)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                var credential = credentialObjects.First(x => x.Referent == requestedCredential.CredentialId);
                if (credential.RevocationRegistryId == null)
                    continue;

                var timestamp = requestedCredential.Timestamp ??
                                throw new Exception(
                                    "Timestamp must be provided for credential that supports revocation");

                if (result.ContainsKey(credential.RevocationRegistryId) &&
                    result[credential.RevocationRegistryId].ContainsKey($"{timestamp}"))
                {
                    continue;
                }

                var registryDefinition =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool,
                        credential.RevocationRegistryId);

                var delta = await LedgerService.LookupRevocationRegistryDeltaAsync(pool,
                    credential.RevocationRegistryId, -1, timestamp);

                var tailsfile = await TailsService.EnsureTailsExistsAsync(pool, credential.RevocationRegistryId);
                var tailsReader = await TailsService.OpenTailsAsync(tailsfile);

                var state = await AnonCreds.CreateRevocationStateAsync(tailsReader, registryDefinition.ObjectJson,
                    delta.ObjectJson, (long) delta.Timestamp, credential.CredentialRevocationId);

                if (!result.ContainsKey(credential.RevocationRegistryId))
                    result.Add(credential.RevocationRegistryId, new Dictionary<string, JObject>());

                result[credential.RevocationRegistryId].Add($"{timestamp}", JObject.Parse(state));

                // TODO: Revocation state should provide the state between a certain period
                // that can be requested in the proof request in the 'non_revocation' field.
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDetlasAsync(Pool pool,
            IEnumerable<ProofIdentifier> proofIdentifiers)
        {
            var result = new Dictionary<string, Dictionary<string, JObject>>();

            foreach (var identifier in proofIdentifiers)
            {
                var delta = await LedgerService.LookupRevocationRegistryDeltaAsync(pool,
                    identifier.RevocationRegistryId,
                    -1,
                    long.Parse(identifier.Timestamp));

                result.Add(identifier.RevocationRegistryId,
                    new Dictionary<string, JObject>
                    {
                        {identifier.Timestamp, JObject.Parse(delta.ObjectJson)}
                    });
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDefinitionsAsync(Pool pool,
            IEnumerable<string> revocationRegistryIds)
        {
            var result = new Dictionary<string, JObject>();

            foreach (var revocationRegistryId in revocationRegistryIds)
            {
                var ledgerSchema =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, revocationRegistryId);
                result.Add(revocationRegistryId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        #endregion
    }
}