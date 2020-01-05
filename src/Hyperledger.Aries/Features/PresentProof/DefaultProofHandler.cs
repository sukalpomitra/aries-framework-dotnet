using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;

namespace Hyperledger.Aries.Features.PresentProof
{
    public class DefaultProofHandler : IMessageHandler
    {
        private readonly IProofService _proofService;

        public DefaultProofHandler(IProofService proofService)
        {
            _proofService = proofService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            MessageTypes.PresentProofNames.Presentation,
            MessageTypes.PresentProofNames.RequestPresentation
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AriesFrameworkException">Unsupported message type {messageType}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                // v0.1
                //case MessageTypes.ProofRequest:
                //{
                //    var request = messageContext.GetMessage<ProofRequestMessage>();
                //    var proofId = await _proofService.ProcessProofRequestAsync(agentContext, request, messageContext.Connection, false);
                //    messageContext.ContextRecord = await _proofService.GetAsync(agentContext, proofId);
                //    break;
                //}
                //case MessageTypes.DisclosedProof:
                //{
                //    var proof = messageContext.GetMessage<ProofMessage>();
                //    var proofId = await _proofService.ProcessProofAsync(agentContext, proof);

                //    messageContext.ContextRecord = await _proofService.GetAsync(agentContext, proofId);
                //    break;
                //}

                // v1.0
                case MessageTypes.PresentProofNames.RequestPresentation:
                {
                    var message = messageContext.GetMessage<RequestPresentationMessage>();
                    var record = await _proofService.ProcessRequestAsync(agentContext, message, messageContext.Connection);

                    messageContext.ContextRecord = record;
                    break;
                }
                case MessageTypes.PresentProofNames.Presentation:
                {
                    var message = messageContext.GetMessage<PresentationMessage>();
                    var record = await _proofService.ProcessPresentationAsync(agentContext, message);

                    messageContext.ContextRecord = record;
                    break;
                }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
            return null;
        }
    }
}
