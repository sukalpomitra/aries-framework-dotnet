using Newtonsoft.Json;

namespace AgentFramework.Core.Runtime.Responses
{
    /// <summary>
    /// Represents a credential object as stored in the wallet.
    /// </summary>
    public class CloudAgentResponse
    {
        /// <summary>
        /// Gets or sets the credential object info.
        /// </summary>
        /// <value>The credential object.</value>
        [JsonProperty("message")]
        public byte[] message { get; set; }

        /// <summary>
        /// Gets or sets the non revocation interval for this credential.
        /// </summary>
        /// <value>The non revocation interval.</value>
        [JsonProperty("packed")]
        public bool packed { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"msg={message}, " +
            $"packed={packed}";
    }
}