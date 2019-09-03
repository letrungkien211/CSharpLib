using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KL.TaskQueue
{
    /// <summary>
    /// 
    /// </summary>
    internal class ConcurrentTaskQueueOptions
    {
        public int MaxQueueLength { get; set; }
        public int MaxConcurrentTasks { get; set; }
    }
}