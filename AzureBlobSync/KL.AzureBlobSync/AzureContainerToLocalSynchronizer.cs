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
    internal class AzureContainerToLocalSynchronizer : IFolderSynchronizer
    {
        private CloudBlobContainer Container { get; }
        private string Prefix { get; }
        private string TargetLocalFolder { get; }

        /// <summary>
        /// Azure blob synchronizer. Copy the latest version from azure blob storage to local
        /// </summary>
        /// <param name="container"></param>
        /// <param name="prefix"></param>
        /// <param name="targetLocalFolder"></param>
        public AzureContainerToLocalSynchronizer(CloudBlobContainer container, string prefix, string targetLocalFolder)
        {
            Container = container;
            Prefix = prefix;
            TargetLocalFolder = targetLocalFolder;
        }


        /// <summary>
        /// Sync to local
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken)
        {
            BlobContinuationToken blobContinuationToken = null;
            var ret = new List<FolderItemSyncResult>();
            while (!cancellationToken.IsCancellationRequested)
            {
                var blobs = await Container.ListBlobsSegmentedAsync(Prefix,
                    true,
                    BlobListingDetails.None,
                    null,
                    blobContinuationToken, null, null, cancellationToken).ConfigureAwait(false);
                blobContinuationToken = blobs.ContinuationToken;

                foreach (var blob in blobs.Results)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    if (!(blob is CloudBlockBlob cloudBlockBlob)) continue;

                    var nameWithoutPrefix = cloudBlockBlob.Name.Substring(Prefix.Length);
                    var localPath = Path.Combine(TargetLocalFolder, nameWithoutPrefix);

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
                            await cloudBlockBlob.DownloadToFileAsync(localPath, FileMode.Create, null, null, null, null, cancellationToken).ConfigureAwait(false);
                            if (!fileInfo.Exists)
                            {
                                fileInfo = new FileInfo(localPath);
                            }
                            ret.Add(new FolderItemSyncResult()
                            {
                                Path = nameWithoutPrefix,
                                LastModified = fileInfo.LastWriteTimeUtc,
                                Ex = null,
                                Result = FolderItemSyncResultEnum.UpdateSuccess
                            });
                        }
                        catch (Exception ex)
                        {
                            ret.Add(new FolderItemSyncResult()
                            {
                                Path = nameWithoutPrefix,
                                LastModified = fileInfo.LastWriteTimeUtc,
                                Ex = ex,
                                Result = FolderItemSyncResultEnum.UpdateFailure
                            });
                        }
                    }
                    else
                    {
                        ret.Add(new FolderItemSyncResult()
                        {
                            Path = nameWithoutPrefix,
                            LastModified = fileInfo.LastWriteTimeUtc,
                            Ex = null,
                            Result = FolderItemSyncResultEnum.Skip
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
