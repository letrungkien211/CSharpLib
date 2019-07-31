using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Self local synchrnoizer
    /// </summary>
    internal class LocalToLocalSynchronizer : IFolderSynchronizer
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
        public Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(SourceFolder, "*", SearchOption.AllDirectories);

            var blobSyncResults = new List<FolderItemSyncResult>();
            foreach (var file in files)
            {
                var result = new FolderItemSyncResult()
                {
                    Path = file
                };
                var sourcePath = Path.Combine(SourceFolder, file);
                var sourceLastModified = File.GetLastWriteTimeUtc(sourcePath);

                var targetPath = Path.Combine(TargetFolder, file);
                if (!File.Exists(targetPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                }
                var targetLastModified = File.GetLastWriteTimeUtc(targetPath);

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
            return Task.FromResult<IEnumerable<FolderItemSyncResult>>(blobSyncResults);
        }
    }
}
