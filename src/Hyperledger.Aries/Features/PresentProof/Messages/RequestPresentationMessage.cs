using System;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Decorators.Attachments;
using Hyperledger.Aries.Decorators.Service;
using Newtonsoft.Json;

namespace Hyperledger.Aries.Features.PresentProof
{
    /// <summary>
    /// Request presentation message
    /// </summary>
    public class RequestPresentationMessage : AgentMessage
    {
        /// <summary>
        /// Initializes a new instace of the <see cref="RequestPresentationMessage" /> class.
        /// </summary>
        public RequestPresentationMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = MessageTypes.PresentProofNames.RequestPresentation;
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the request presentation attachments
        /// </summary>
        /// <value></value>
        [JsonProperty("request_presentations~attach")]
        public Attachment[] Requests { get; set; }

        /// <summary>
        /// Gets or sets the service decorator.
        /// </summary>
        /// <value>
        /// The service decorator.
        /// </value>
        [JsonProperty("~service", NullValueHandling = NullValueHandling.Ignore)]
        public ServiceDecorator ServiceDecorator { get; set; }
    }
}