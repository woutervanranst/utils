namespace WouterVanRanst.Utils.Extensions;

public static class TaskExtensions
{
    public static async Task WhenAllWithCancellationAsync(IEnumerable<Task> tasks, CancellationTokenSource globalCancellationTokenSource)
    {
        if (!tasks.Any())
        {
            return;
        }

        try
        {
            // Attach faulted continuations to each task to trigger cancellation when any task fails
            foreach (var task in tasks)
            {
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Console.WriteLine("A task has faulted, cancelling all other tasks.");
                        globalCancellationTokenSource.Cancel();
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            // Await the completion of all tasks, letting cancellation propagate as necessary
            await Task.WhenAll(tasks);
        }
        catch
        {
            // If any task faults, ensure cancellation is triggered
            globalCancellationTokenSource.Cancel();
            throw;
        }
    }
}