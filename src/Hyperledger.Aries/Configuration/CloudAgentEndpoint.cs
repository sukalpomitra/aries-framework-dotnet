using Newtonsoft.Json;

namespace Hyperledger.Aries.Configuration
{
    /// <summary>
    /// An object for containing cloud agent endpoint information.
    /// </summary>
    public class CloudAgentEndpoint
    {
        [JsonProperty("serviceEndpoint")]
        private string _serviceEndpoint;
        [JsonProperty("consumerEndpoint")]
        private string _consumerEndpoint;
        [JsonProperty("responseEndpoint")]
        private string _responseEndpoint;

        internal CloudAgentEndpoint() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAgentEndpoint"/> class.
        /// </summary>
        /// <param name="serviceEndpoint">The Service Endpoint URI.</param>
        /// <param name="consumerEndpoint">The Consumer Endpoint URI.</param>
        public CloudAgentEndpoint(string serviceEndpoint, string consumerEndpoint, string responseEndpoint)
        {
            ServiceEndpoint = serviceEndpoint;
            ConsumerEndpoint = consumerEndpoint;
            ResponseEndpoint = responseEndpoint;
        }

        /// <summary>
        /// Gets or sets the service endpoint uri of the agent.
        /// </summary>
        /// <value>
        /// The service endpoint uri of the agent.
        /// </value>
        [JsonIgnore]
        public string ServiceEndpoint
        {
            get => _serviceEndpoint;
            internal set => _serviceEndpoint = value;
        }

        /// <summary>
        /// Gets or sets the consumer endpoint uri of the agent.
        /// </summary>
        /// <value>
        /// The consumer endpoint uri of the agent.
        /// </value>
        [JsonIgnore]
        public string ConsumerEndpoint
        {
            get => _consumerEndpoint;
            internal set => _consumerEndpoint = value;
        }

        /// <summary>
        /// Gets or sets the response endpoint uri of the agent.
        /// </summary>
        /// <value>
        /// The response endpoint uri of the agent.
        /// </value>
        [JsonIgnore]
        public string ResponseEndpoint
        {
            get => _responseEndpoint;
            internal set => _responseEndpoint = value;
        }

        /// <inheritdoc />
        public override string ToString() =>
            $"ServiceEndpoint={ServiceEndpoint}, " +
            $"ConsumerEndpoint={ConsumerEndpoint}, " +
            $"ResponseEndpoint={ResponseEndpoint}";
    }
}