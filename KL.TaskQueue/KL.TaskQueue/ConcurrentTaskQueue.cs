using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KL.TaskQueue
{
    internal class ConcurrentTaskQueue<Input> : IDisposable, ITaskQueue<Input>
    {
        private BlockingCollection<Input> Inputs { get; } = new BlockingCollection<Input>();
        private ConcurrentTaskQueueOptions Options { get; }
        private Func<Input, CancellationToken, Task> Processor { get; }

        private long _outstandingTasks = 0;

        public long Length => Inputs.Count;
        public long OutstandingTasks => _outstandingTasks;

        public ConcurrentTaskQueue(
            ConcurrentTaskQueueOptions options,
            Func<Input, CancellationToken, Task> processor
            )
        {
            Options = options;
            Processor = processor;
        }

        public bool Add(Input input)
        {
            if (Inputs.Count >= Options.MaxQueueLength)
            {
                return false;
            }
            return Inputs.TryAdd(input);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var input = Inputs.Take(cancellationToken);
                Interlocked.Increment(ref _outstandingTasks);

                var task = Processor(input, cancellationToken)
                          .ContinueWith(_ => Interlocked.Decrement(ref _outstandingTasks));

                if (Interlocked.Read(ref _outstandingTasks) > Options.MaxConcurrentTasks)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            Inputs.Dispose();
        }
    }
}
