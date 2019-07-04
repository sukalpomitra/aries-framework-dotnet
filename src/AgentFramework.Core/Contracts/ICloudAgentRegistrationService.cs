using System.Collections.Generic;
using System.Threading.Tasks;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

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

        /// <summary>
        /// Get All Registered Cloud Agents async.
        /// </summary>
        /// <param name="wallet">Wallet.</param>
        /// <returns>Fetches all registered cloud agents from non-secret records in the wallet having a tag cloudagent.</returns>
        Task<List<CloudAgentRegistrationRecord>> GetAllCloudAgentAsync(Wallet wallet);

        /// <summary>
        /// Get All Registered Cloud Agents async.
        /// </summary>
        /// <param name="records">List of Cloud Agents.</param>
        /// <returns>Returns a random cloud agent from a list of cloud agents.</returns>
        CloudAgentRegistrationRecord getRandomCloudAgent(List<CloudAgentRegistrationRecord> records);

        /// <summary>
        /// Deletes a Registered Cloud Agents async.
        /// </summary>
        /// <param name="wallet">wallet.</param>
        /// <param name="id">Record id.</param>
        /// <returns>Boolean status indicating if the removal succeed</returns>
        Task removeCloudAgentAsync(Wallet wallet, string id);
    }
}
