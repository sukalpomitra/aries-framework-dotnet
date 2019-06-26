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
        /// <returns>Creates a non-secret record of the cloud agent in the wallet.</returns>
        Task<CloudAgentRegistrationRecord> RegisterCloudAgentAsync(IAgentContext agentContext, CloudAgentRegistrationMessage registration);
    }
}
