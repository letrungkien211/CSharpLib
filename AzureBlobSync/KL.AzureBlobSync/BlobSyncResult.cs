using System;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Blob sync result
    /// </summary>
    public class BlobSyncResult
    {
        /// <summary>
        /// Path (without prefix)
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Last modifed on local
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Sync result
        /// </summary>
        public BlobSyncResultEnum Result { get; set; }

        /// <summary>
        /// Exception
        /// </summary>
        public Exception Ex { get; set; }
    }
}