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
    private readonly Channel<Task<T>> channel = Channel.CreateUnbounded<Task<T>>(new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = false, SingleWriter = false });

    private bool addingCompleted = false;

    public void Add(Task<T> task)
    {
        if (addingCompleted)
            throw new InvalidOperationException("Cannot add tasks after completion.");

        task.ContinueWith(t =>
        {
            channel.Writer.TryWrite(t);
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public void CompleteAdding()
    {
        addingCompleted = true;
        channel.Writer.Complete();
    }

    public bool IsCompleted => addingCompleted;

    public async IAsyncEnumerable<Task<T>> GetConsumingEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var t in channel.Reader.ReadAllAsync(cancellationToken))
            yield return t;
    }
}