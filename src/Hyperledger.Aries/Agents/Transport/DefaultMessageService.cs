using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;

namespace Hyperledger.Aries.Agents
{
    /// <inheritdoc />
    public class DefaultMessageService : IMessageService
    {
        /// <summary>The agent wire message MIME type</summary>
        public const string AgentWireMessageMimeType = "application/ssi-agent-wire";

        private readonly ICloudAgentRegistrationService _registrationService;

        /// <summary>The logger</summary>
        // ReSharper disable InconsistentNaming
        protected readonly ILogger<DefaultMessageService> Logger;

        /// <summary>The HTTP client</summary>
        protected readonly IEnumerable<IMessageDispatcher> MessageDispatchers;
        // ReSharper restore InconsistentNaming

        /// <summary>Initializes a new instance of the <see cref="DefaultMessageService"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageDispatchers">The message handler.</param>
        public DefaultMessageService(
            ILogger<DefaultMessageService> logger, 
            IEnumerable<IMessageDispatcher> messageDispatchers,
            ICloudAgentRegistrationService agentRegistrationService)
        {
            Logger = logger;
            MessageDispatchers = messageDispatchers;
            _registrationService = agentRegistrationService;
        }

        /// <inheritdoc />
        public virtual async Task<(byte[], string)> PrepareRouteAsync(Wallet wallet, byte[] message, string endpointUri)
        {
            var records = await _registrationService.GetAllCloudAgentAsync(wallet);
            int counter = 0;
            while (records.Count > 0)
            {
                counter++;
                var record = _registrationService.getRandomCloudAgent(records);
                message = await CryptoUtils.PackAsync(wallet, record.TheirVk, new ForwardMessage { Message = message.GetUTF8String(), To = endpointUri });
                endpointUri = record.Endpoint.ServiceEndpoint;
            }
            return (message, endpointUri);
        }
        private async Task<UnpackedMessageContext> UnpackAsync(Wallet wallet, PackedMessageContext message, string senderKey)
        {
            UnpackResult unpacked;

            try
            {
                unpacked = await CryptoUtils.UnpackAsync(wallet, message.Payload);
            }
            catch (Exception e)
            {
                //Logger.LogError("Failed to un-pack message", e);
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Failed to un-pack message", e);
            }
            return new UnpackedMessageContext(unpacked.Message, senderKey);
        }

        /// <inheritdoc />
        public virtual async Task SendAsync(Wallet wallet, AgentMessage message, string recipientKey,
            string endpointUri, string[] routingKeys = null, string senderKey = null)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", recipientKey,
                endpointUri);

            if (string.IsNullOrEmpty(message.Id))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@id field on message must be populated");

            if (string.IsNullOrEmpty(message.Type))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@type field on message must be populated");

            if (string.IsNullOrEmpty(endpointUri))
                throw new ArgumentNullException(nameof(endpointUri));

            var uri = new Uri(endpointUri);

            var dispatcher = GetDispatcher(uri.Scheme);

            if (dispatcher == null)
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, $"No registered dispatcher for transport scheme : {uri.Scheme}");

            var wireMsg = await CryptoUtils.PrepareAsync(wallet, message, recipientKey, routingKeys, senderKey);

            await dispatcher.DispatchAsync(uri, new PackedMessageContext(wireMsg));
        }

        /// <inheritdoc />
        public async Task<MessageContext> SendReceiveAsync(Wallet wallet, AgentMessage message, string recipientKey,
            string endpointUri, string[] routingKeys = null, string senderKey = null, bool requestResponse = false)
        {
            Logger.LogInformation(LoggingEvents.SendMessage, "Recipient {0} Endpoint {1}", recipientKey,
                endpointUri);

            if (string.IsNullOrEmpty(message.Id))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@id field on message must be populated");

            if (string.IsNullOrEmpty(message.Type))
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "@type field on message must be populated");

            if (string.IsNullOrEmpty(endpointUri))
                throw new ArgumentNullException(nameof(endpointUri));

            if (requestResponse)
                message.AddReturnRouting();

            var wireMsg = await CryptoUtils.PrepareAsync(wallet, message, recipientKey, routingKeys, senderKey);
            var (msg, serviceEndpoint) = await PrepareRouteAsync(wallet, wireMsg, endpointUri);
            var uri = new Uri(serviceEndpoint);

            var dispatcher = GetDispatcher(uri.Scheme);

            if (dispatcher == null)
                throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, $"No registered dispatcher for transport scheme : {uri.Scheme}");

            var response = await dispatcher.DispatchAsync(uri, new PackedMessageContext(msg));
            if (requestResponse) {
                if (response is PackedMessageContext responseContext)
                {
                    return await UnpackAsync(wallet, responseContext, senderKey);
                }
                throw new InvalidOperationException("Invalid or empty response");
            }
            return response;
        }

        /// <inheritdoc />
        public virtual async Task<List<MessageContext>> ConsumeAsync(Wallet wallet)
        {
            var records = await _registrationService.GetAllCloudAgentAsync(wallet);
            List<MessageContext> messages = new List<MessageContext>();
            foreach (var record in records)
            {
                var uri = new Uri(record.Endpoint.ConsumerEndpoint + "/" + record.MyConsumerId);

                var dispatcher = GetDispatcher(uri.Scheme);

                if (dispatcher == null)
                    throw new AgentFrameworkException(ErrorCode.A2AMessageTransmissionError, $"No registered dispatcher for transport scheme : {uri.Scheme}");

                var messageContexts = await dispatcher.ConsumeAsync(uri);
                messages.AddRange(messageContexts);
            }
            return messages;
        }

        private IMessageDispatcher GetDispatcher(string scheme) => MessageDispatchers.FirstOrDefault(_ => _.TransportSchemes.Contains(scheme));
    }
}