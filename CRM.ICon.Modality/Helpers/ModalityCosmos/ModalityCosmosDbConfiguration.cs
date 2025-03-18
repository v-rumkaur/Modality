namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public class ModalityCosmosDbConfiguration
    {       
        /// <summary>
        /// Gets or sets CosmosDbEndpoint
        /// </summary>
        public string CosmosDbEndpoint { get; set; }

        /// <summary>
        /// Gets or sets CosmosDbKey
        /// </summary>
        public string CosmosDbKey { get; set; }

        /// <summary>
        /// Gets or sets DatabaseId
        /// </summary>
        public string DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets ContainerId
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        /// Gets or sets PartitionKeyPath
        /// </summary>
        public string PartitionKeyPath { get; set; }

        /// <summary>
        /// Gets or sets RequestTimeout
        /// </summary>
        public int RequestTimeout { get; set; }
        public string UserManagedIdentityId { get; set; }
    }
}
