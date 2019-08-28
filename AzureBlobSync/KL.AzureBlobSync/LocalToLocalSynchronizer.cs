using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Self local synchrnoizer
    /// </summary>
    internal class LocalToLocalSynchronizer : FolderSynchronizerBase
    {
        /// <summary>
        /// Self local synchronizer
        /// </summary>
        public LocalToLocalSynchronizer(string sourceFolder, string targetFolder)
        {
            SourceFolder = sourceFolder;
            TargetFolder = targetFolder;
        }

        public string SourceFolder { get; }
        public string TargetFolder { get; }

        /// <summary>
        /// Sync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken)
        {
            var files = LocalFileUtils.ListFiles(SourceFolder);

            var blobSyncResults = new List<FolderItemSyncResult>();
            foreach (var file in files)
            {
                var result = new FolderItemSyncResult()
                {
                    Path = file
                };
                try
                {
                    var sourcePath = Path.Combine(SourceFolder, file);
                    var sourceLastModified = File.GetLastWriteTimeUtc(sourcePath);

                    var targetPath = Path.Combine(TargetFolder, file);
                    if (!File.Exists(targetPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    }
                    var targetLastModified = File.GetLastWriteTimeUtc(targetPath);

                    result.LastModified = sourceLastModified > targetLastModified ? sourceLastModified : targetLastModified;
                    if (sourceLastModified > targetLastModified)
                    {
                        File.Copy(sourcePath, targetPath, true);
                        result.Result = FolderItemSyncResultEnum.UpdateSuccess;
                    }
                    else
                    {
                        result.Result = FolderItemSyncResultEnum.Skip;
                    }
                    blobSyncResults.Add(result);
                }
                catch(Exception ex)
                {
                    result.Result = FolderItemSyncResultEnum.UpdateFailure;
                    result.Ex = ex;
                }
            }
            return Task.FromResult<IEnumerable<FolderItemSyncResult>>(blobSyncResults);
        }
    }
}
