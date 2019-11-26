using System;
using System.Collections.Generic;
using Hyperledger.Aries.Agents;
using Newtonsoft.Json;

namespace Hyperledger.Aries.Features.DidExchange
{
    /// <summary>
    /// Represents an invitation message for establishing connection.
    /// </summary>
    public class ConnectionInvitationMessage : AgentMessage
    {
        /// <inheritdoc />
        public ConnectionInvitationMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.ConnectionInvitation;
        }
        
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>
        /// The image URL.
        /// </value>
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the service endpoint.
        /// </summary>
        /// <value>
        /// The service endpoint.
        /// </value>
        [JsonProperty("serviceEndpoint")]
        public string ServiceEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the routing keys.
        /// </summary>
        /// <value>
        /// The routing keys.
        /// </value>
        [JsonProperty("routingKeys")]
        public IList<string> RoutingKeys { get; set; }

        /// <summary>
        /// Gets or sets the recipient keys.
        /// </summary>
        /// <value>
        /// The recipient keys.
        /// </value>
        [JsonProperty("recipientKeys")]
        public IList<string> RecipientKeys { get; set; }

        /// <summary>
        /// Gets or sets the sso param.
        /// </summary>
        /// <value>
        /// The recipient keys.
        /// </value>
        [JsonProperty("sso")]
        public bool Sso { get; set; }

        /// <summary>
        /// Gets or sets the invitation key param.
        /// </summary>
        /// <value>
        /// The invitation key.
        /// </value>
        [JsonProperty("invitationKey")]
        public string InvitationKey { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"Name={Label}, " +
            $"ImageUrl={ImageUrl}, " +
            $"Sso={Sso}, " +
            $"InvitationKey={InvitationKey}, " +
            $"RoutingKeys={string.Join(",", RoutingKeys ?? new string[0])}, ";
    }
}
