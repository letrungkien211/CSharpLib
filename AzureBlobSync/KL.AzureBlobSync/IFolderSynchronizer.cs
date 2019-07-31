using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KL.AzureBlobSync
{
    /// <summary>
    /// Sync folder
    /// </summary>
    public interface IFolderSynchronizer
    {
        /// <summary>
        /// Sync folder async
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetFolder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<BlobSyncResult>> SyncFolderAsync(string sourceFolder, string targetFolder, CancellationToken cancellationToken);
    }
}