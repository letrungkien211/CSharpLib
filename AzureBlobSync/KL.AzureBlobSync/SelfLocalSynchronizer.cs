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
    internal class SelfLocalSynchronizer : IFolderSynchronizer
    {
        /// <summary>
        /// Self local synchronizer
        /// </summary>
        public SelfLocalSynchronizer(string folder, string syncFilePath)
        {
            Folder = folder ?? throw new ArgumentNullException(nameof(folder));
            SyncFilePath = syncFilePath ?? throw new ArgumentNullException(nameof(syncFilePath));
        }

        private string Folder { get; }
        private string SyncFilePath { get; }

        /// <summary>
        /// Sync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken)
        {
            if (Folder == null) throw new ArgumentNullException(nameof(Folder));

            var fileInfos = JsonConvert.DeserializeObject<Dictionary<string, FolderItemSyncResult>>(File.Exists(SyncFilePath) ? File.ReadAllText(SyncFilePath) : "{}");

            var files = Directory.GetFiles(Folder, "*", SearchOption.AllDirectories);

            var blobSyncResults = new List<FolderItemSyncResult>();
            foreach (var file in files)
            {
                var result = new FolderItemSyncResult()
                {
                    Path = file
                };
                var path = Path.Combine(Folder, file);
                var lastModified = File.GetLastWriteTimeUtc(path);
                if (!fileInfos.TryGetValue(file, out var fileInfo))
                {
                    fileInfo = new FolderItemSyncResult()
                    {
                        LastModified = DateTimeOffset.MinValue,
                        Result = FolderItemSyncResultEnum.Skip,
                        Path = file
                    };
                }

                if (lastModified > fileInfo.LastModified)
                {
                    fileInfo.LastModified = lastModified;
                    fileInfos[path] = fileInfo;
                    result.Result = FolderItemSyncResultEnum.UpdateSuccess;
                }
                else
                {
                    result.Result = FolderItemSyncResultEnum.Skip;
                }
                result.LastModified = lastModified;
                blobSyncResults.Add(result);
            }
            var newFileInfos = blobSyncResults.ToDictionary(x => x.Path, y => y);
            File.WriteAllText(SyncFilePath, JsonConvert.SerializeObject(newFileInfos));
            return Task.FromResult<IEnumerable<FolderItemSyncResult>>(blobSyncResults);
        }
    }
}
