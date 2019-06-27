using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Connections;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Models.Records.Search;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Cloud Agent Registration Service.
    /// </summary>
    public interface ICloudAgentRegistrationService
    {
        /// <summary>
        /// Accepts the connection invitation async.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <param name="registration">Cloud Agent Registration Details</param>
        /// <exception cref="AgentFrameworkException">Throws with ErrorCode.CloudAgentAlreadyRegistered.</exception>
        /// <returns>Creates a non-secret record of the cloud agent in the wallet.</returns>
        Task<CloudAgentRegistrationRecord> RegisterCloudAgentAsync(IAgentContext agentContext, CloudAgentRegistrationMessage registration);

        /// <summary>
        /// Get All Registered Cloud Agents async.
        /// </summary>
        /// <param name="agentContext">Agent Context.</param>
        /// <returns>Fetches all registered cloud agents from non-secret records in the wallet having a tag cloudagent.</returns>
        Task<List<CloudAgentRegistrationRecord>> GetAllCloudAgentAsync(IAgentContext agentContext);
    }
}
