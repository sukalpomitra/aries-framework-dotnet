using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentFramework.Core.Decorators.Service
{
    /// <summary>
    /// Service decorator model.
    /// </summary>
    public class ServiceDecorator
    {
        /// <summary>
        /// Service Endpoint.
        /// </summary>
        [JsonProperty("serviceEndpoint")]
        public string ServiceEndpoint { get; set; }

        /// <summary>
        /// List of recipient keys.
        /// </summary>
        [JsonProperty("recipientKeys")]
        public List<string> RecipientKeys { get; set; }

        /// <summary>
        /// List of routing keys.
        /// </summary>
        [JsonProperty("routingKeys")]
        public List<string> RoutingKeys { get; set; }

    }
}
