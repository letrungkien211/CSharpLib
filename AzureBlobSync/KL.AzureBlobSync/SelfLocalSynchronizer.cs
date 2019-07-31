using Newtonsoft.Json;
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
    public class SelfLocalSynchronizer : IFolderSynchronizer
    {
        private class TempFileInfo
        {
            public DateTimeOffset LastModified { get; set; }
        }
        /// <summary>
        /// Self local synchronizer
        /// </summary>
        public SelfLocalSynchronizer(string syncFilePath)
        {
            SyncFilePath = syncFilePath;
        }

        private string SyncFilePath { get; }

        /// <summary>
        /// Sync
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetFolder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEnumerable<BlobSyncResult>> SyncFolderAsync(string sourceFolder, string targetFolder, CancellationToken cancellationToken)
        {
            if (sourceFolder == null) throw new ArgumentNullException(nameof(sourceFolder));
            if (targetFolder == null) throw new ArgumentNullException(nameof(targetFolder));

            if (sourceFolder != targetFolder)
                throw new ArgumentException($"{nameof(SelfLocalSynchronizer)} only supports sync the same folder to itself.");

            var fileInfos = JsonConvert.DeserializeObject<Dictionary<string, TempFileInfo>>(File.Exists(SyncFilePath) ? File.ReadAllText(SyncFilePath) : "{}");

            var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

            var blobSyncResults = new List<BlobSyncResult>();
            foreach (var file in files)
            {
                var result = new BlobSyncResult()
                {
                    Path = file
                };
                var path = Path.Combine(sourceFolder, file);
                var lastModified = File.GetLastWriteTimeUtc(path);
                if (!fileInfos.TryGetValue(file, out var fileInfo))
                {
                    fileInfo = new TempFileInfo()
                    {
                        LastModified = DateTimeOffset.MinValue
                    };
                }

                if (lastModified > fileInfo.LastModified)
                {
                    fileInfo.LastModified = lastModified;
                    fileInfos[path] = fileInfo;
                    result.Result = BlobSyncResultEnum.UpdateSuccess;
                }
                else
                {
                    result.Result = BlobSyncResultEnum.Skip;
                }
                result.LastModified = lastModified;
                blobSyncResults.Add(result);
            }
            return Task.FromResult<IEnumerable<BlobSyncResult>>(blobSyncResults);
        }
    }
}
