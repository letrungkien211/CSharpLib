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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken);
    }
}