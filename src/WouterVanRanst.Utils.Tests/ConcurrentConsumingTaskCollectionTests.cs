using WouterVanRanst.Utils.Collections;

namespace WouterVanRanst.Utils.Tests;

public class ConcurrentConsumingTaskCollectionTests
{
    [Fact]
    public async Task TasksAreProcessedInCompletionOrder()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var t1 = SimulateTask("Task1", 300);
        var t2 = SimulateTask("Task2", 100);
        var t3 = SimulateTask("Task3", 200);

        taskQueue.Add(t1);
        taskQueue.Add(t2);
        taskQueue.Add(t3);
        taskQueue.CompleteAdding();

        // Act
        var processedTasks = new List<string>();
        await foreach (var result in taskQueue.GetConsumingEnumerable())
        {
            processedTasks.Add(await result);
        }

        // Assert
        Assert.Equal(new[] { "Task2", "Task3", "Task1" }, processedTasks);
    }

    [Fact]
    public async Task QueueStopsWhenAllTasksAreCompleted()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var t1 = SimulateTask("Task1", 100);

        taskQueue.Add(t1);
        taskQueue.CompleteAdding();

        // Act
        var processedTasks = new List<string>();
        await foreach (var result in taskQueue.GetConsumingEnumerable())
        {
            processedTasks.Add(await result);
        }

        // Assert
        Assert.Single(processedTasks);
        Assert.Equal("Task1", processedTasks.First());
    }

    [Fact]
    public void CannotAddTasksAfterCompleteAdding()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        taskQueue.CompleteAdding();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => taskQueue.Add(SimulateTask("Task1", 100)));
    }

    [Fact]
    public async Task QueueHandlesEmptyTaskSet()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        taskQueue.CompleteAdding();

        // Act
        var processedTasks = new List<string>();
        await foreach (var result in taskQueue.GetConsumingEnumerable())
        {
            processedTasks.Add(await result);
        }

        // Assert
        Assert.Empty(processedTasks);
    }

    [Fact]
    public async Task ProcessingStopsWhenCancelled()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var t1 = SimulateTask("Task1", 200);
        var t2 = SimulateTask("Task2", 400);

        taskQueue.Add(t1);
        taskQueue.Add(t2);
        taskQueue.CompleteAdding();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(250); // Cancel after 0.25 seconds

        // Act
        var processedTasks = new List<string>();
        try
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable(cts.Token))
            {
                processedTasks.Add(await result);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected due to cancellation; ignore.
        }

        // Assert
        Assert.Single(processedTasks); // Only Task1 should complete before cancellation
        Assert.Equal("Task1", processedTasks.First());
    }

    [Fact]
    public async Task ConcurrentProducersAndConsumersProcessCorrectly()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();

        var producer = Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Task1", 300));
            await Task.Delay(500);
            taskQueue.Add(SimulateTask("Task2", 100));
            await Task.Delay(500);
            taskQueue.Add(SimulateTask("Task3", 200));
            taskQueue.CompleteAdding();
        });

        var processedTasks = new List<string>();
        var consumer1 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                processedTasks.Add(await result);
            }
        });

        var consumer2 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                processedTasks.Add(await result);
            }
        });

        // Act
        await producer;
        await Task.WhenAll(consumer1, consumer2);

        // Assert
        Assert.Equal(3, processedTasks.Count);
        Assert.Contains("Task1", processedTasks);
        Assert.Contains("Task2", processedTasks);
        Assert.Contains("Task3", processedTasks);
    }

    [Fact]
    public async Task TaskSetTemporarilyEmptyButMoreTasksAreAdded()
    {
        // Arrange
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();

        var producer = Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Task1", 100)); // Task1 added first
            await Task.Delay(1500); // Simulate delay where the task set becomes temporarily empty
            taskQueue.Add(SimulateTask("Task2", 100)); // Task2 added after some time
            taskQueue.CompleteAdding(); // Complete adding tasks
        });

        var processedTasks = new List<string>();

        // Act
        await foreach (var result in taskQueue.GetConsumingEnumerable())
        {
            processedTasks.Add(await result);
        }

        // Assert
        Assert.Equal(2, processedTasks.Count);
        Assert.Contains("Task1", processedTasks);
        Assert.Contains("Task2", processedTasks);
    }


    private async Task<string> SimulateTask(string name, int delay)
    {
        await Task.Delay(delay);
        return name;
    }
}