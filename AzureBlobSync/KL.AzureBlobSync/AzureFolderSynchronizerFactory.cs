using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Create azure folder synchronizer
    /// </summary>
    public class AzureFolderSynchronizerFactory
    {
        private CloudBlobClient CloudBlobClient { get; }

        /// <summary>
        /// Azure blob synchronizer. Copy the latest version from azure blob storage to local
        /// </summary>
        /// <param name="cloudBlobClient"></param>
        public AzureFolderSynchronizerFactory(CloudBlobClient cloudBlobClient)
        {
            CloudBlobClient = cloudBlobClient;
        }

        /// <summary>
        /// Create an instance of IFolderSynchronizer that syncs files from container to local folder
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="prefix"></param>
        /// <param name="localFolder"></param>
        /// <returns></returns>
        public async Task<IFolderSynchronizer> CreateContainerToLocalSynchronizer(string containerName, string prefix, string localFolder)
        {
            var container = CloudBlobClient.GetContainerReference(containerName);
            if (!await container.ExistsAsync().ConfigureAwait(false))
                throw new DirectoryNotFoundException($"Container={container.Uri} is not found!");

            return new AzureContainerToLocalSynchronizer(container, prefix, localFolder);
        }
    }
}
