using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using WouterVanRanst.Utils.Collections;
using Xunit.Abstractions;

namespace WouterVanRanst.Utils.Tests;

public class Test
{
    ITestOutputHelper Output;

    public Test(ITestOutputHelper output)
    {
        this.Output = output;
    }


    [Fact(Timeout = 5000)]
    public void RunCoyoteTest()
    {
        var config = Configuration.Create();
        TestingEngine engine = TestingEngine.Create(config, CoyoteTestMethod);
        engine.Run();
        var report = engine.TestReport;
        Output.WriteLine("Coyote found {0} bug.", report.NumOfFoundBugs);
        Assert.True(report.NumOfFoundBugs == 0, $"Coyote found {report.NumOfFoundBugs} bug(s).");
    }

    private async Task CoyoteTestMethod()
    {
        // This is running as a Coyote test.
        await Task.Delay(10);
        Specification.Assert(false, "This test failed!");
    }
}

public class ConcurrentConsumingTaskCollectionCoyoteTests
{
    [Fact]
    public void TestTaskQueueSingleProducerSingleConsumer()
    {
        // Using Coyote's systematic testing engine
        var configuration = Configuration.Create();
        var engine = TestingEngine.Create(configuration, this.TestSingleProducerSingleConsumer);
        engine.Run();
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    [Fact]
    public void TestTaskQueueMultipleProducersMultipleConsumers()
    {
        // Using Coyote's systematic testing engine
        var configuration = Configuration.Create();
        var engine = TestingEngine.Create(configuration, this.TestMultipleProducersMultipleConsumers);
        engine.Run();
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    private void TestSingleProducerSingleConsumer(IActorRuntime runtime)
    {
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var actualOrder = new List<string>();
        var expectedOrder = new List<string> { "Task2", "Task3", "Task1" };  // Expected order based on task delays

        // Producer: Add tasks to the queue
        Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Task1", 3000));  // Long-running task
            taskQueue.Add(SimulateTask("Task2", 1000));  // Short-running taskdz
            taskQueue.Add(SimulateTask("Task3", 2000));  // Medium-running task
            taskQueue.CompleteAdding();
        });

        // Consumer: Consume tasks in completion order and store the result
        Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                actualOrder.Add(result);
            }
        }).Wait();

        // Assert that the tasks were processed in the correct order
        Assert.Equal(expectedOrder, actualOrder);
    }

    private void TestMultipleProducersMultipleConsumers(IActorRuntime runtime)
    {
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var actualOrder = new List<string>();
        var expectedOrder = new List<string> { "Producer1_Task2", "Producer2_Task2", "Producer2_Task1", "Producer1_Task1" };

        // Producer 1: Add tasks to the queue
        Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Producer1_Task1", 3000));  // Long-running task
            taskQueue.Add(SimulateTask("Producer1_Task2", 1000));  // Short-running task
        });

        // Producer 2: Add tasks to the queue
        Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Producer2_Task1", 2000));  // Medium-running task
            taskQueue.Add(SimulateTask("Producer2_Task2", 1500));  // Medium-short task
            taskQueue.CompleteAdding();
        });

        // Consumer 1: Consume tasks in completion order and store the result
        Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                actualOrder.Add(result);
            }
        });

        // Consumer 2: Consume tasks in completion order and store the result
        Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                actualOrder.Add(result);
            }
        }).Wait();

        // Assert that the tasks were processed in the correct order
        Assert.Equal(expectedOrder, actualOrder);
    }

    private async Task<string> SimulateTask(string name, int delay)
    {
        await Task.Delay(delay);  // Simulate work
        return name;
    }
}