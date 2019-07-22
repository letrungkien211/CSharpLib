using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Blob sync result
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
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