namespace WouterVanRanst.Utils.Extensions;

public static class TaskExtensions
{
    public static async Task WhenAllWithCancellationAsync(IEnumerable<Task> tasks, CancellationTokenSource cancellationTokenSource)
    {
        tasks = tasks.ToArray();

        if (!tasks.Any())
        {
            return;
        }

        Task? t = default;

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
                        cancellationTokenSource.Cancel();
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            // Await the completion of all tasks, letting cancellation propagate as necessary
            t = Task.WhenAll(tasks);
            await t;
        }
        catch
        {
            // If any task faults, ensure cancellation is triggered
            await cancellationTokenSource.CancelAsync();

            if (t?.Exception is not null)
                throw t.Exception.Flatten();
            else
                throw;
        }
    }
}