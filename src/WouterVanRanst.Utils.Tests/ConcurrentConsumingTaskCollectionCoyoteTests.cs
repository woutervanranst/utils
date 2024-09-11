using FluentAssertions;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using WouterVanRanst.Utils.Collections;
using Xunit.Abstractions;

namespace WouterVanRanst.Utils.Tests;

public class ConcurrentConsumingTaskCollectionCoyoteTests
{
    ITestOutputHelper Output;

    public ConcurrentConsumingTaskCollectionCoyoteTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    [Fact]
    //    [Microsoft.Coyote.SystematicTesting.Test]
    public void TestTaskQueueSingleProducerSingleConsumer()
    {
        var configuration = Configuration.Create()
                .WithReproducibleTrace(File.ReadAllText("C:\\Users\\WouterVanRanst\\Desktop\\mytest.trace"))
                //.WithDeadlockTimeout(10000)
                //.WithVerbosityEnabled()
            ;
        var engine = TestingEngine.Create(configuration, this.TestSingleProducerSingleConsumer);
        engine.Run();
        var report = engine.TestReport;
        Output.WriteLine("Coyote found {0} bug.", report.NumOfFoundBugs);

        engine.TryEmitReports("C:\\Users\\WouterVanRanst\\Desktop", "mytest", out var filenames);
        //foreach (var item in filenames)
        //{
        //    Output.WriteLine("See log file: {0}", item);
        //}

        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    //    [Fact]
    //    //[Microsoft.Coyote.SystematicTesting.Test]
    //    public void TestTaskQueueMultipleProducersMultipleConsumers()
    //    {
    //        // Using Coyote's systematic testing engine
    //        var configuration = Configuration.Create();
    //        var engine = TestingEngine.Create(configuration, this.TestMultipleProducersMultipleConsumers);
    //        engine.Run();
    //        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    //    }

    private async Task TestSingleProducerSingleConsumer(IActorRuntime runtime)
    {
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var actualOrder = new List<string>();
        var expectedOrder = new List<string> { "Task2", "Task3", "Task1" };  // Expected order based on task delays

        // Producer: Add tasks to the queue
        var t1 = Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Task1", 3000));  // Long-running task
            taskQueue.Add(SimulateTask("Task2", 1000));  // Short-running task
            taskQueue.Add(SimulateTask("Task3", 2000));  // Medium-running task
            //await Task.Delay(5000);
            taskQueue.CompleteAdding();
        });

        // Consumer: Consume tasks in completion order and store the result
        var t2 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                actualOrder.Add(await result);
                Output.WriteLine(await result);
            }
        });

        await Task.WhenAll(t1, t2);
        Output.WriteLine(t2.Status.ToString());
        
        
        // Assert that the tasks were processed in the correct order
        expectedOrder.SequenceEqual(actualOrder).Should().BeTrue();
        //Assert.Equal(expectedOrder, actualOrder);
    }

    //    private void TestMultipleProducersMultipleConsumers(IActorRuntime runtime)
    //    {
    //        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
    //        var actualOrder = new List<string>();
    //        var expectedOrder = new List<string> { "Producer1_Task2", "Producer2_Task2", "Producer2_Task1", "Producer1_Task1" };

    //        // Producer 1: Add tasks to the queue
    //        Task.Run(async () =>
    //        {
    //            taskQueue.Add(SimulateTask("Producer1_Task1", 3000));  // Long-running task
    //            taskQueue.Add(SimulateTask("Producer1_Task2", 1000));  // Short-running task
    //        });

    //        // Producer 2: Add tasks to the queue
    //        Task.Run(async () =>
    //        {
    //            taskQueue.Add(SimulateTask("Producer2_Task1", 2000));  // Medium-running task
    //            taskQueue.Add(SimulateTask("Producer2_Task2", 1500));  // Medium-short task
    //            taskQueue.CompleteAdding();
    //        });

    //        // Consumer 1: Consume tasks in completion order and store the result
    //        Task.Run(async () =>
    //        {
    //            await foreach (var result in taskQueue.GetConsumingEnumerable())
    //            {
    //                actualOrder.Add(result);
    //            }
    //        });

    //        // Consumer 2: Consume tasks in completion order and store the result
    //        Task.Run(async () =>
    //        {
    //            await foreach (var result in taskQueue.GetConsumingEnumerable())
    //            {
    //                actualOrder.Add(result);
    //            }
    //        }).Wait();

    //        // Assert that the tasks were processed in the correct order
    //        Assert.Equal(expectedOrder, actualOrder);
    //    }

    private async Task<string> SimulateTask(string name, int delay)
    {
        await Task.Delay(delay);  // Simulate work
        return name;
    }
}