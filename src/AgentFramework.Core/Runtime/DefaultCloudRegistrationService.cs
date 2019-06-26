using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Cache;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Attachments;
using AgentFramework.Core.Decorators.Signature;
using AgentFramework.Core.Decorators.Threading;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Dids;
using AgentFramework.Core.Models.Events;
using AgentFramework.Core.Utils;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Microsoft.Extensions.Logging;
using ConnectionState = AgentFramework.Core.Models.Records.ConnectionState;

namespace AgentFramework.Core.Handlers.Agents
{
    /// <inheritdoc />
    public class DefaultCloudRegistrationService : ICloudAgentRegistrationService
    {
        /// <summary>
        /// The record service
        /// </summary>
        protected readonly IWalletRecordService RecordService;
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger<DefaultConnectionService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCloudRegistrationService"/> class.
        /// </summary>
        /// <param name="recordService">The record service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultCloudRegistrationService(
            IWalletRecordService recordService,
            ILogger<DefaultConnectionService> logger)
        {
            Logger = logger;
            RecordService = recordService;
        }

        /// <inheritdoc />
        public virtual async Task<CloudAgentRegistrationRecord> RegisterCloudAgentAsync(IAgentContext agentContext, CloudAgentRegistrationMessage registration)
        {
            Logger.LogInformation(LoggingEvents.CloudAgentRegistration, "Key {0}, Endpoint {1}",
                registration.RecipientKeys[0], registration.ServiceEndpoint);
            var record = new CloudAgentRegistrationRecord
            {
                Endpoint = new CloudAgentEndpoint(registration.ServiceEndpoint, registration.ConsumerEndpoint, registration.ResponseEndpoint),
                TheirVk = registration.RecipientKeys[0],
                Label = registration.Label,
                MyConsumerId = registration.Consumer,
                Id = Guid.NewGuid().ToString().ToLowerInvariant()
            };
            record.SetTag(TagConstants.CloudAgent, registration.Label);
            await RecordService.AddAsync(agentContext.Wallet, record);
            return record;
        }
    }
}