using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Models.Connections;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stateless;
using Stateless.Graph;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Represents a connection record in the agency wallet.
    /// </summary>
    /// <seealso cref="RecordBase" />
    public class CloudAgentRegistrationRecord : RecordBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAgentRegistrationRecord"/> class.
        /// </summary>
        public CloudAgentRegistrationRecord()
        {
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        public override string TypeName => "AF.CloudAgentRegistrationRecord";

        /// <summary>
        /// Gets or sets their verkey.
        /// </summary>
        /// <value>Their vk.</value>
        [JsonIgnore]
        public string TheirVk
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets their label.
        /// </summary>
        /// <value>Their label.</value>
        [JsonIgnore]
        public string Label
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets My Consumer id.
        /// </summary>
        /// <value>My Consumer Id.</value>
        public string MyConsumerId
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets CloudAgentEndpoint.
        /// </summary>
        /// <value>CloudAgentEndpoint.</value>
        public CloudAgentEndpoint Endpoint
        {
            get;
            set;
        }

        /// <inheritdoc />
        public override string ToString() =>
            $"{GetType().Name}: " +
            $"Label={Label}, " +
            $"TheirVk={(TheirVk?.Length > 0 ? "[hidden]" : null)}, " +
            $"CloudAgentEndpoint={Endpoint}, " +
            base.ToString();  
    }
}
