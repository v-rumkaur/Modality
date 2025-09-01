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
        public ContainerIds ContainerIds { get; set; }

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

    public class ContainerIds
    {
        /// <summary>
        /// Gets or sets WidgetMapping container id
        /// </summary>
        public required string WidgetMapping { get; set; }

        /// <summary>
        /// Gets or sets LiveChatSettings container id
        /// </summary>
        public required string LiveChatSettings { get; set; }
    }
}
