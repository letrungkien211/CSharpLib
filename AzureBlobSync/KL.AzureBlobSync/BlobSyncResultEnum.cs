namespace KL.AzureBlobSync
{
    /// <summary>
    /// Blob sync result
    /// </summary>
    public enum BlobSyncResultEnum
    {
        /// <summary>
        /// Skip
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Update success
        /// </summary>
        UpdateSuccess = 1,

        /// <summary>
        /// Update failure
        /// </summary>
        UpdateFailure = 2
    }
}