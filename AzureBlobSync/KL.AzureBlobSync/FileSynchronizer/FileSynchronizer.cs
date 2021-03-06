﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KL.AzureBlobSync
{
    public class FileSyncResult
    {
        public FileSyncResultStatus Status { get; set; }
        public bool IsErrorStatus()
        {
            return Status == FileSyncResultStatus.Error;
        }
        public Exception Ex { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FileSyncResultStatus
    {
        Skip,
        SourceNotFound,
        Updated,
        Error
    }

    /// <summary>
    /// Synchronize files across storages.
    /// </summary>
    public class FileSynchronizer
    {
        private readonly Dictionary<string, CloudBlobClient> _storages = new Dictionary<string, CloudBlobClient>();

        /// <summary>
        /// File synchronizer
        /// </summary>
        /// <param name="storageInfos"></param>
        public FileSynchronizer(IEnumerable<StorageInfo> storageInfos)
        {
            foreach (var storageInfo in storageInfos)
            {
                AddStorage(storageInfo);
            }
        }

        /// <summary>
        /// Add storage
        /// </summary>
        /// <param name="storageInfo"></param>
        public void AddStorage(StorageInfo storageInfo)
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageInfo.Id};AccountKey={storageInfo.Key}";
            _storages[storageInfo.Id] = AzureUtil.GetBlobClient(connectionString);
        }

        /// <summary>
        /// Synchronize files
        /// </summary>
        /// <param name="syncFilePairs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FileSyncResult>> RunAsync(IEnumerable<SyncFilePair> syncFilePairs, CancellationToken cancellationToken)
        {
            var results = new List<FileSyncResult>();
            foreach (var pair in syncFilePairs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    var sourceClient = GetStorage(pair.SourceStorageId, pair.SourceContainer);
                    var targetClient = GetStorage(pair.TargetStorageId, pair.TargetContainer);

                    if (!await sourceClient.ExistsAsync(pair.SourcePath).ConfigureAwait(false))
                    {
                        results.Add(new FileSyncResult()
                        {
                            Status = FileSyncResultStatus.SourceNotFound
                        });
                        continue;
                    }

                    var sourceLastModified = await sourceClient.GetLastModifiedAsync(pair.SourcePath).ConfigureAwait(false);

                    var targetLastModified = await targetClient.ExistsAsync(pair.TargetPath).ConfigureAwait(false) ? await targetClient.GetLastModifiedAsync(pair.TargetPath).ConfigureAwait(false) : DateTime.MinValue;

                    if (sourceLastModified > targetLastModified)
                    {
                        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".temp");
                        try
                        {
                            await sourceClient.DownloadToFileAsync(pair.SourcePath, tempFile).ConfigureAwait(false);
                            await targetClient.UploadFileAsync(tempFile, pair.TargetPath).ConfigureAwait(false);
                        }
                        finally
                        {
                            if (File.Exists(tempFile))
                                File.Delete(tempFile);
                        }
                        results.Add(new FileSyncResult()
                        {
                            Status = FileSyncResultStatus.Updated
                        });
                    }
                    else
                    {
                        results.Add(new FileSyncResult()
                        {
                            Status = FileSyncResultStatus.Skip
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new FileSyncResult()
                    {
                        Status = FileSyncResultStatus.Error,
                        Ex = ex
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Get storage
        /// </summary>
        /// <param name="id"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        internal IStorage GetStorage(string id, string container)
        {
            if (id == "LOCAL")
            {
                return new LocalStorage("");
            }

            if (!_storages.TryGetValue(id, out var storage))
            {
                return null;
            }

            return new AzureBlobStorage(storage, container, "");
        }
    }

    /// <summary>
    /// Dummy implementation for istorage
    /// </summary>
    internal class LocalStorage : IStorage
    {
        private readonly string _prefix;
        public LocalStorage(string prefix)
        {
            _prefix = prefix ?? "";
            if (_prefix != "" && !Directory.Exists(_prefix))
            {
                Directory.CreateDirectory(_prefix);
            }
        }

        public Task<DateTime> GetLastModifiedAsync(string path)
        {
            return Task.FromResult(File.GetLastWriteTimeUtc(Path.Combine(_prefix, path)));
        }

        public Task UploadFileAsync(string localFilePath, string storagePath)
        {
            var directoryName = Path.GetDirectoryName(Path.Combine(_prefix, storagePath));
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);
            File.Copy(localFilePath, Path.Combine(_prefix, storagePath));
            return Task.FromResult(0);
        }

        public Task DownloadToFileAsync(string storagePath, string targetFilePath)
        {
            var directoryName = Path.GetDirectoryName(Path.Combine(_prefix, storagePath));
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);
            File.Copy(Path.Combine(_prefix, storagePath), targetFilePath);
            return Task.FromResult(0);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(File.Exists((Path.Combine(_prefix, path))));
        }
    }

    /// <summary>
    /// AzureStorage class. Upload to blob storage
    /// </summary>
    internal class AzureBlobStorage : IStorage
    {
        private readonly CloudBlobContainer _blobContainer;
        private readonly string _folder;

        /// <summary>
        /// Azure blob constructor
        /// </summary>
        /// <param name="blobClient"></param>
        /// <param name="container"></param>
        /// <param name="prefix"></param>
        public AzureBlobStorage(CloudBlobClient blobClient, string container, string prefix)
        {
            if (blobClient == null || string.IsNullOrEmpty(container))
                throw new ArgumentNullException();

            _blobContainer = blobClient.GetContainerReference(container);
            _folder = prefix;
        }

        /// <summary>
        /// Azure blob constructor
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="container"></param>
        /// <param name="prefix"></param>
        public AzureBlobStorage(string connectionString, string container, string prefix) : this(AzureUtil.GetBlobClient(connectionString), container, prefix)
        {
        }

        public Task UploadFileAsync(string localFilePath, string storagePath)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(_folder + storagePath);
            return blockBlob.UploadFromFileAsync(localFilePath);
        }

        public async Task<DateTime> GetLastModifiedAsync(string path)
        {
            var blockBlob = await _blobContainer.GetBlobReferenceFromServerAsync(_folder + path).ConfigureAwait(false);
            if (blockBlob?.Properties.LastModified != null)
                return ((DateTimeOffset)blockBlob.Properties.LastModified).DateTime;
            return DateTime.MinValue;
        }

        public Task DownloadToFileAsync(string storagePath, string localFilePath)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(_folder + storagePath);
            return blockBlob.DownloadToFileAsync(localFilePath, FileMode.Create);
        }

        public Task<bool> ExistsAsync(string path)
        {
            var blockBlob = _blobContainer.GetBlockBlobReference(_folder + path);
            return blockBlob.ExistsAsync();
        }
    }

    /// <summary>
    /// Interface for a storage
    /// </summary>
    internal interface IStorage
    {
        Task UploadFileAsync(string localFilePath, string storagePath);
        Task DownloadToFileAsync(string storagePath, string localFilePath);
        Task<DateTime> GetLastModifiedAsync(string path);
        Task<bool> ExistsAsync(string path);
    }

    /// <summary>
    /// This is utility class for azure
    /// </summary>
    internal static class AzureUtil
    {
        /// <summary>
        /// Get azure blob client from connection string
        /// </summary>
        /// <param name="connectionString">azure connection string</param>
        /// <returns></returns>
        public static CloudBlobClient GetBlobClient(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            return storageAccount.CreateCloudBlobClient();
        }

        public static CloudTableClient GetTableClient(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            return storageAccount.CreateCloudTableClient();
        }

        public static CloudTable GetTable(string connectionString, string tableName)
        {
            return GetTableClient(connectionString).GetTableReference(tableName.ToLower());
        }
    }

    /// <summary>
    /// Storage info.
    /// </summary>
    public class StorageInfo
    {
        /// <summary>
        /// Id of storage. Id=LOCAL for local storage. Id=storageAccountName for azure blob storage
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Key of storage. Key=NULL if local. Key=storageAccountKey for azure blob storage
        /// </summary>
        public string Key { get; set; }
    }

    /// <summary>
    /// Sync file pair
    /// </summary>
    public class SyncFilePair
    {
        public string SourceStorageId { get; set; }
        public string SourceContainer { get; set; }
        public string SourcePath { get; set; }
        public string TargetStorageId { get; set; }
        public string TargetContainer { get; set; }
        public string TargetPath { get; set; }
    }
}
