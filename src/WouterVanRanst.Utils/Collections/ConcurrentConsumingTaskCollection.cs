using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WouterVanRanst.Utils.Collections;

/// <summary>
/// A thread-safe collection of tasks that are consumed as they complete.
/// Tasks can be added concurrently by multiple producers and consumed by multiple consumers.
/// The collection processes tasks in the order they complete, regardless of the order they were added.
/// </summary>
/// <typeparam name="T">The type of the result returned by the tasks.</typeparam>
public sealed class ConcurrentConsumingTaskCollection<T>
{
    /* TODO: See for a better approach https://devblogs.microsoft.com/pfxteam/processing-tasks-as-they-complete/, https://github.com/StephenCleary/AsyncEx/blob/0361015459938f2eb8f3c1ad1021d19ee01c93a4/src/Nito.AsyncEx.Tasks/TaskExtensions.cs#L184
     */
    private readonly Channel<Task<T>> channel = Channel.CreateUnbounded<Task<T>>(new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = false, SingleWriter = false });

    private bool addingCompleted = false;
    private int activeTaskCount = 0;

    public void Add(Task<T> task)
    {
        if (addingCompleted)
            throw new InvalidOperationException("Cannot add tasks after completion.");

        Interlocked.Increment(ref activeTaskCount);

        task.ContinueWith(async t =>
        {
            await channel.Writer.WriteAsync(t);

            // Decrement active task count and complete the writer if done
            if (Interlocked.Decrement(ref activeTaskCount) == 0 && addingCompleted)
            {
                channel.Writer.Complete();
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public void CompleteAdding()
    {
        addingCompleted = true;

        if (Interlocked.CompareExchange(ref activeTaskCount, 0, 0) == 0)
        {
            channel.Writer.Complete();
        }
    }

    public bool IsCompleted => addingCompleted && activeTaskCount == 0 && channel.Reader.Completion.IsCompleted;

    public async IAsyncEnumerable<Task<T>> GetConsumingEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var t in channel.Reader.ReadAllAsync(cancellationToken))
            yield return t;
    }
}
