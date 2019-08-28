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

        /// <summary>
        /// Set number of files to be downloaded in parallel
        /// </summary>
        int Parallel { get; set; }
    }

    /// <inheritDoc/>
    public abstract class FolderSynchronizerBase : IFolderSynchronizer
    {
        /// <inheritDoc/>
        public virtual int Parallel { get; set; } = 1;

        /// <inheritDoc/>
        public abstract Task<IEnumerable<FolderItemSyncResult>> SyncFolderAsync(CancellationToken cancellationToken);
    }
}