using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Azure blob synchronizer. Copy the latest version from azure blob storage to local
    /// </summary>
    public class AzureContainerToLocalSynchronizer
    {
        private CloudBlobClient CloudBlobClient { get; }
        private string AccountName { get; set; }

        /// <summary>
        /// Azure blob synchronizer. Copy the latest version from azure blob storage to local
        /// </summary>
        /// <param name="cloudBlobClient"></param>
        public AzureContainerToLocalSynchronizer(CloudBlobClient cloudBlobClient)
        {
            CloudBlobClient = cloudBlobClient;
            AccountName = CloudBlobClient.Credentials.AccountName;
        }

        /// <summary>
        /// Azure blob synchronizer. Copy the latest version from azure blob storage to local
        /// </summary>
        /// <param name="options"></param>
        public AzureContainerToLocalSynchronizer(AzureContainerToLocalOptions options)
        {
            var storageCredentials = new StorageCredentials(options.AccountName, options.AccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            AccountName = options.AccountName;
        }

        /// <summary>
        /// Sync to local
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="prefix"></param>
        /// <param name="localDirectory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BlobSyncResult>> SyncToLocalAsync(string containerName, string prefix, string localDirectory, CancellationToken cancellationToken)
        {
            var container = CloudBlobClient.GetContainerReference(containerName);
            if (!await container.ExistsAsync())
                throw new DirectoryNotFoundException($"AccountName={AccountName}, Container={container} is not found!");

            BlobContinuationToken blobContinuationToken = null;
            var ret = new List<BlobSyncResult>();
            while (!cancellationToken.IsCancellationRequested)
            {
                var blobs = await container.ListBlobsSegmentedAsync(prefix,
                    true,
                    BlobListingDetails.None,
                    null,
                    blobContinuationToken, null, null, cancellationToken);
                blobContinuationToken = blobs.ContinuationToken;

                foreach (var blob in blobs.Results)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    if (!(blob is CloudBlockBlob cloudBlockBlob)) continue;

                    var nameWithoutPrefix = cloudBlockBlob.Name.Substring(prefix.Length);
                    var localPath = Path.Combine(localDirectory, nameWithoutPrefix);

                    var fileInfo = new FileInfo(localPath);

                    var diff = false;
                    if (!fileInfo.Exists)
                    {
                        var directory = fileInfo.Directory;
                        diff = true;

                        if (directory != null && !directory.Exists)
                        {
                            directory.Create();
                        }
                    }
                    else
                    {
                        if (fileInfo.LastWriteTimeUtc < cloudBlockBlob.Properties.LastModified)
                        {
                            diff = true;
                        }
                    }

                    if (diff)
                    {
                        try
                        {
                            await cloudBlockBlob.DownloadToFileAsync(localPath, FileMode.Create, null, null, null, null, cancellationToken);
                            if (!fileInfo.Exists)
                            {
                                fileInfo = new FileInfo(localPath);
                            }
                            ret.Add(new BlobSyncResult()
                            {
                                Path = nameWithoutPrefix,
                                LastModified = fileInfo.LastWriteTimeUtc,
                                Ex = null,
                                Result = BlobSyncResultEnum.UpdateSuccess
                            });
                        }
                        catch (Exception ex)
                        {
                            ret.Add(new BlobSyncResult()
                            {
                                Path = nameWithoutPrefix,
                                LastModified = fileInfo.LastWriteTimeUtc,
                                Ex = ex,
                                Result = BlobSyncResultEnum.UpdateFailure
                            });
                        }
                    }
                    else
                    {
                        ret.Add(new BlobSyncResult()
                        {
                            Path = nameWithoutPrefix,
                            LastModified = fileInfo.LastWriteTimeUtc,
                            Ex = null,
                            Result = BlobSyncResultEnum.Skip
                        });
                    }
                }

                if (blobContinuationToken == null)
                {
                    break;
                }
            }

            return ret;
        }
    }
}
