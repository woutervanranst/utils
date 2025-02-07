using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;


namespace WouterVanRanst.Utils.Collections;

public class TaskCompletionBuffer<T>
{
    private readonly Channel<Task<T>> _taskChannel = Channel.CreateUnbounded<Task<T>>();
    private readonly CancellationTokenSource _completionSignal = new();
    private bool _isAddingCompleted;

    public void Add(Task<T> task)
    {
        if (_isAddingCompleted)
            throw new InvalidOperationException("Adding tasks has been marked as complete.");

        _taskChannel.Writer.TryWrite(task);
    }

    public void CompleteAdding()
    {
        _isAddingCompleted = true;
        _taskChannel.Writer.Complete();
        _completionSignal.Cancel(); // Signal to exit enumeration when done
    }

    public async IAsyncEnumerable<Task<T>> GetConsumingEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pendingTasks = new List<Task<T>>();
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _completionSignal.Token).Token;

        var reader = _taskChannel.Reader;

        while (!combinedToken.IsCancellationRequested || pendingTasks.Count > 0)
        {
            // Check for newly added tasks
            while (reader.TryRead(out var task))
                pendingTasks.Add(task);

            if (pendingTasks.Count == 0)
            {
                if (_isAddingCompleted) break; // No more tasks coming

                // Wait for new tasks or completion signal
                await reader.WaitToReadAsync(combinedToken).ConfigureAwait(false);
                continue;
            }

            // Wait for any task to complete or new tasks to arrive
            var completedTask = await Task.WhenAny([..pendingTasks, reader.WaitToReadAsync(combinedToken).AsTask()]).ConfigureAwait(false);

            if (completedTask is Task<T> resultTask)
            {
                pendingTasks.Remove(resultTask);
                yield return resultTask;
            }
        }
    }
}