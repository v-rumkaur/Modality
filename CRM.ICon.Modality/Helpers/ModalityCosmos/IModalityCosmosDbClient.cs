namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public interface IModalityCosmosDbClient
    {
        /// <summary>
        /// Add/Update list of documents for doc db
        /// </summary>
        /// <param name="query"></param>
        /// <param name="logger"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        Task<T> UpsertItemAsync<T>(T item);

        Task<T> GetItemAsync<T>(string id);
    }
}
