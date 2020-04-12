using System;
using System.Threading;
using System.Threading.Tasks;

namespace KL.TaskQueue
{
    /// <summary>
    /// Task Queue
    /// </summary>
    /// <typeparam name="Input"></typeparam>
    public interface ITaskQueue<Input> : IDisposable
    {
        /// <summary>
        /// Length
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Outstanding tasks
        /// </summary>
        long OutstandingTasks { get; }

        /// <summary>
        /// Add input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        bool Add(Input input);

        /// <summary>
        /// Run the task queue in background
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken cancellationToken);
    }
}