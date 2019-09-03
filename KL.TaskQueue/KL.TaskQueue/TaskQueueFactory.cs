using System;
using System.Threading;
using System.Threading.Tasks;

namespace KL.TaskQueue
{
    /// <summary>
    /// Task queue factory
    /// </summary>
    public class TaskQueueFactory
    {
        /// <summary>
        /// Create a task queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="processor"></param>
        /// <param name="maxQueueLength"></param>
        /// <param name="maxConcurrentTasks"></param>
        /// <returns></returns>
        public ITaskQueue<T> Create<T>(Func<T, CancellationToken, Task> processor, int maxQueueLength, int maxConcurrentTasks)
        {
            return new ConcurrentTaskQueue<T>(new ConcurrentTaskQueueOptions()
            {
                MaxConcurrentTasks = maxConcurrentTasks,
                MaxQueueLength = maxQueueLength
            }, processor);
        }
    }
}
