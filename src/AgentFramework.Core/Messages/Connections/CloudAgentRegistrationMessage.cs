using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentFramework.Core.Messages.Connections
{
    /// <summary>
    /// Represents an invitation message for establishing connection.
    /// </summary>
    public class CloudAgentRegistrationMessage : AgentMessage
    {
        /// <inheritdoc />
        public CloudAgentRegistrationMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.CloudAgentRegistration;
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
        /// Gets or sets the response endpoint.
        /// </summary>
        /// <value>
        /// The response endpoint.
        /// </value>
        [JsonProperty("responseEndpoint")]
        public string ResponseEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the consumer endpoint.
        /// </summary>
        /// <value>
        /// The consumer endpoint.
        /// </value>
        [JsonProperty("consumerEndpoint")]
        public string ConsumerEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the consumer id.
        /// </summary>
        /// <value>
        /// The Consumer Id.
        /// </value>
        [JsonProperty("consumer")]
        public string Consumer { get; set; }

        /// <summary>
        /// Gets or sets the recipient keys.
        /// </summary>
        /// <value>
        /// The recipient keys.
        /// </value>
        [JsonProperty("recipientKeys")]
        public IList<string> RecipientKeys { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Id={Id}, " +
            $"Type={Type}, " +
            $"Name={Label}, " +
            $"ImageUrl={ImageUrl}, " +
            $"Consumer={Consumer}, ";
    }
}
