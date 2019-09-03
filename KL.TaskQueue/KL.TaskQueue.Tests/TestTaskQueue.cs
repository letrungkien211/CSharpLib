using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KL.TaskQueue.Tests
{
    public class TestTaskQueue
    {
        public ITestOutputHelper TestOutputHelper { get; }

        public TestTaskQueue(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Success()
        {
            long sum = 0;
            var queue = new TaskQueueFactory().Create<int>((x, cancellationToken) =>
            {
                Interlocked.Add(ref sum, x);
                return Task.FromResult(0);
            }, int.MaxValue, Environment.ProcessorCount);

            var n = 1000;
            using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var task = Task.Run(() => queue.RunAsync(cancelSource.Token));

                for (var i = 0; i < n; i++)
                {
                    queue.Add(i);
                }
                await task.ContinueWith(_ => { });

                Assert.Equal((n * (n - 1)) / 2, sum);
            }

        }

        [Fact]
        public async Task FailedToAdd()
        {
            long sum = 0;
            var queue = new TaskQueueFactory().Create<int>((x, cancellationToken) =>
            {
                Interlocked.Add(ref sum, x);
                return Task.FromResult(0);
            }, Environment.ProcessorCount, Environment.ProcessorCount);

            var n = 1000;
            using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var task = Task.Run(() => queue.RunAsync(cancelSource.Token));

                for (var i = 0; i < n; i++)
                {
                    queue.Add(i);
                }
                await task.ContinueWith(_ => { });

                Assert.NotEqual((n * (n - 1)) / 2, sum);
            }
        }

        [Fact]
        public async Task NotThrowException()
        {
            await Temp().ContinueWith(_ => { TestOutputHelper.WriteLine(_.Exception?.ToString()); });
        }

        private Task Temp()
        {
            throw new Exception();
        }
    }
}
