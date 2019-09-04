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
    internal class AzureContainerToLocalSynchronizer : FolderSynchronizerBase
    {
        private CloudBlobContainer Container { get; }
        private string Prefix { get; }
        private string TargetLocalFolder { get; }

        public override int Parallel { get; set; } = 1;

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
        public override async Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken)
        {
            BlobContinuationToken blobContinuationToken = null;
            var ret = new List<FolderItemSyncResult>();
            using (var semaphoreSlim = new SemaphoreSlim(Parallel))
            {
                var tasks = new List<Task>();
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
                            await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                            var downloadTask = cloudBlockBlob.DownloadToFileAsync(localPath, FileMode.Create, null, null, null, null, cancellationToken)
                                        .ContinueWith(result =>
                                        {
                                            if (result.Status == TaskStatus.RanToCompletion)
                                            {
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
                                            else
                                            {
                                                try
                                                {
                                                    if (File.Exists(localPath))
                                                        File.Delete(localPath);
                                                }
                                                catch
                                                {
                                                    //
                                                }
                                                ret.Add(new FolderItemSyncResult()
                                                {
                                                    Path = nameWithoutPrefix,
                                                    LastModified = fileInfo.LastWriteTimeUtc,
                                                    Ex = result.Exception,
                                                    Result = FolderItemSyncResultEnum.UpdateFailure
                                                });
                                            }
                                            semaphoreSlim.Release();
                                        });
                            tasks.Add(downloadTask);
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

                await Task.WhenAll(tasks).ConfigureAwait(false);
                return ret;
            }
        }
    }
}
