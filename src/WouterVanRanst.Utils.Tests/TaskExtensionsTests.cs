using FluentAssertions;
using TaskExtensions = WouterVanRanst.Utils.Extensions.TaskExtensions;

namespace WouterVanRanst.Utils.Tests;

public sealed class TaskExtensionsTests
{
    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldCancelAllTasks_WhenCancellationTokenIsTriggered()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var task1 = CreateLongRunningTask(cancellationToken);
        var task2 = CreateLongRunningTask(cancellationToken);

        // Act
        var t = TaskExtensions.WhenAllWithCancellationAsync(new[] { task1, task2 }, cancellationTokenSource);

        // Cancel after a short delay
        cancellationTokenSource.CancelAfter(10);

        // Assert
        await FluentActions
            .Invoking(async () => await t)
            .Should().ThrowAsync<TaskCanceledException>();

        task1.Status.Should().Be(TaskStatus.Canceled);
        task2.Status.Should().Be(TaskStatus.Canceled);
        cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldCancelAllOtherTasks_WhenOneTaskFaults()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var task1 = CreateFaultedTask("Task 1 failed", 50);
        var task2 = CreateLongRunningTask(cancellationToken);

        // Act
        var t = TaskExtensions.WhenAllWithCancellationAsync(new[] { task1, task2 }, cancellationTokenSource);

        // Assert
        await FluentActions
            .Invoking(async () => await t)
            .Should().ThrowAsync<Exception>().WithMessage("Task 1 failed");

        task1.Status.Should().Be(TaskStatus.Faulted);
        task2.Status.Should().Be(TaskStatus.Canceled);
        cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldReturn_WhenNoTasksAreProvided()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        Func<Task> act = async () => await TaskExtensions.WhenAllWithCancellationAsync(Enumerable.Empty<Task>(), cancellationTokenSource);

        // Assert
        await act.Should().NotThrowAsync();
        cancellationTokenSource.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldComplete_WhenAllTasksSucceed()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        var task1 = Task.CompletedTask;
        var task2 = Task.CompletedTask;

        // Act
        var taskList = new List<Task> { task1, task2 };
        Func<Task> act = async () => await TaskExtensions.WhenAllWithCancellationAsync(taskList, cancellationTokenSource);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldCancelAllOtherTasks_WhenMultipleTasksFault()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var task1 = CreateFaultedTask("Task 1 failed", 50);
        var task2 = CreateFaultedTask("Task 2 failed", 70);
        var task3 = CreateLongRunningTask(cancellationToken);

        // Act
        var t = TaskExtensions.WhenAllWithCancellationAsync(new[] { task1, task2, task3 }, cancellationTokenSource);

        // Assert
        await FluentActions
            .Invoking(async () => await t)
            .Should().ThrowAsync<Exception>().WithMessage("Task 1 failed");

        task1.Status.Should().Be(TaskStatus.Faulted);
        task2.Status.Should().Be(TaskStatus.Faulted);
        task3.Status.Should().Be(TaskStatus.Canceled);
        cancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldCancelTasks_WhenCancellationTokenIsAlreadyCanceled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Pre-cancel the token
        var cancellationToken = cancellationTokenSource.Token;

        var task1 = CreateLongRunningTask(cancellationToken);
        var task2 = CreateLongRunningTask(cancellationToken);

        // Act
        Func<Task> act = async () => await TaskExtensions.WhenAllWithCancellationAsync(new[] { task1, task2 }, cancellationTokenSource);

        // Assert that a TaskCanceledException is thrown since the tasks are canceled
        await FluentActions
            .Invoking(async () => await act())
            .Should().ThrowAsync<TaskCanceledException>();

        task1.Status.Should().Be(TaskStatus.Canceled);
        task2.Status.Should().Be(TaskStatus.Canceled);
        cancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldCancelTasks_WhenExternalCancellationAndTaskFaultOccurs()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var task1 = CreateFaultedTask("Task 1 failed", 50);
        var task2 = CreateLongRunningTask(cancellationToken);

        // Act: Trigger external cancellation after 500ms
        cancellationTokenSource.CancelAfter(500);
        Func<Task> act = async () => await TaskExtensions.WhenAllWithCancellationAsync(new[] { task1, task2 }, cancellationTokenSource);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Task 1 failed");
        task1.Status.Should().Be(TaskStatus.Faulted); // task1 should fault
        task2.Status.Should().Be(TaskStatus.Canceled); // task2 should be canceled
        cancellationToken.IsCancellationRequested.Should().BeTrue(); // Ensure cancellation was requested
    }

    [Fact]
    public async Task ExecuteTasksWithCancellationAsync_ShouldThrowAggregateException_WhenMultipleTasksFault()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Create tasks: Two faulted tasks and one long-running task
        var task1 = CreateFaultedTask("Task 1 failed", 0);  
        var task2 = CreateFaultedTask("Task 2 failed", 0); 
        var task3 = CreateLongRunningTask(cancellationToken);  // Long-running task

        // Act
        Func<Task> act = async () => await TaskExtensions.WhenAllWithCancellationAsync([task1, task2, task3], cancellationTokenSource);

        // Assert: Expect an AggregateException with both task failures
        var exception = await act.Should().ThrowAsync<AggregateException>();
        exception.Which.InnerExceptions.Should().Contain(e => e.Message == "Task 1 failed");
        exception.Which.InnerExceptions.Should().Contain(e => e.Message == "Task 2 failed");

        // Verify that the long-running task was canceled
        task3.Status.Should().Be(TaskStatus.Canceled);
        cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
    }

    
    private static Task CreateLongRunningTask(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Task cancelled");
                throw;
            }
        }, cancellationToken);
    }

    private static Task CreateFaultedTask(string exceptionMessage, int delayMs)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(delayMs); // Simulate some delay before fault
            throw new Exception(exceptionMessage);
        });
    }

}