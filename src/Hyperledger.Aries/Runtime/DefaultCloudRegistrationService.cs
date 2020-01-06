using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hyperledger.Indy.WalletApi;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.CloudRegistrationMessage;
using Hyperledger.Aries.Utils;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Storage;

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

        /// <inheritdoc />
        public virtual async Task<List<CloudAgentRegistrationRecord>> GetAllCloudAgentAsync(Wallet wallet)
        {
            return await RecordService.SearchAsync<CloudAgentRegistrationRecord>(wallet, null, null, 100);
        }

        /// <inheritdoc />
        public CloudAgentRegistrationRecord getRandomCloudAgent(List<CloudAgentRegistrationRecord> records)
        {
            Random rand = new Random();
            var randomNumber = rand.Next(0, records.Count);
            var record = records[randomNumber];
            records.RemoveAt(randomNumber);
            return record;
        }

        /// <inheritdoc />
        public virtual async Task removeCloudAgentAsync(Wallet wallet, string id)
        {
            Logger.LogInformation(LoggingEvents.CloudAgentRegistrationRemoval, "ID {0}",
                id);

            await RecordService.DeleteAsync<CloudAgentRegistrationRecord>(wallet, id);

        }
    }
}