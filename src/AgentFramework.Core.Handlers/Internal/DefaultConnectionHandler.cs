using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Utils;

namespace AgentFramework.Core.Handlers.Internal
{
    public class DefaultConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;
        private readonly IMessageService _messageService;

        /// <summary>Initializes a new instance of the <see cref="DefaultConnectionHandler"/> class.</summary>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="messageService">The message service.</param>
        public DefaultConnectionHandler(
            IConnectionService connectionService,
            IMessageService messageService)
        {
            _connectionService = connectionService;
            _messageService = messageService;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(MessageTypes.ConnectionInvitation),
            new MessageType(MessageTypes.ConnectionRequest),
            new MessageType(MessageTypes.ConnectionResponse)
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AgentFrameworkException">Unsupported message type {message.Type}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, MessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypes.ConnectionInvitation:
                    var invitation = messageContext.GetMessage<ConnectionInvitationMessage>();
                    await _connectionService.CreateRequestAsync(agentContext, invitation);
                    return null;

                case MessageTypes.ConnectionRequest:
                {
                    var request = messageContext.GetMessage<ConnectionRequestMessage>();
                    var connectionId = await _connectionService.ProcessRequestAsync(agentContext, request, messageContext.Connection);
                    // Auto accept connection if set during invitation
                    if (messageContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                    {
                        (var message, var _) = await _connectionService.CreateResponseAsync(agentContext, connectionId);
                        return message;
                    }
                    return null;
                }

                case MessageTypes.ConnectionResponse:
                {
                    var response = messageContext.GetMessage<ConnectionResponseMessage>();
                    await _connectionService.ProcessResponseAsync(agentContext, response, messageContext.Connection);
                    if (messageContext.Connection.Sso)
                    {
                        var endpoint = messageContext.Connection.Endpoint.Uri.Replace("response", "trigger/")
                                + messageContext.Connection.MyDid + "/" + messageContext.Connection.InvitationKey;
                        HttpClient httpClient = new HttpClient();
                        await httpClient.GetAsync(new System.Uri(endpoint));
                    }
                    return null;
                }
                default:
                    throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}