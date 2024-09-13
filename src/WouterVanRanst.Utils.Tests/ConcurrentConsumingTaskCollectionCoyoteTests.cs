using FluentAssertions;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
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
    public void TestTaskQueueSingleProducerSingleConsumer()
    {
        return;

        var configuration = Configuration.Create()
                .WithReproducibleTrace(File.ReadAllText("C:\\Users\\WouterVanRanst\\Desktop\\mytest.trace"))
                .WithDeadlockTimeout(10000)
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

    [Fact]
    public void TestTaskQueueMultipleProducersMultipleConsumers()
    {
        return;

        var configuration = Configuration.Create()
            //.WithReproducibleTrace(File.ReadAllText("C:\\Users\\WouterVanRanst\\Desktop\\mytest2.trace"))
            //.WithTestingIterations(100)
            ;
        var engine = TestingEngine.Create(configuration, this.TestMultipleProducersMultipleConsumers);
        engine.Run();

        //engine.TryEmitReports("C:\\Users\\WouterVanRanst\\Desktop", "mytest2", out var filenames);

        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    private async Task TestSingleProducerSingleConsumer(IActorRuntime runtime)
    {
        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var actualOrder = new List<string>();
        var expectedOrder = new List<string> { "Task2", "Task3", "Task1" };  // Expected order based on task delays

        // Producer: Add tasks to the queue
        var p1 = Task.Run(async () =>
        {
            taskQueue.Add(SimulateTask("Task1", 300));  // Long-running task
            taskQueue.Add(SimulateTask("Task2", 100));  // Short-running task
            taskQueue.Add(SimulateTask("Task3", 200));  // Medium-running task
            taskQueue.CompleteAdding();
        });

        // Consumer: Consume tasks in completion order and store the result
        var c1 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                //SchedulingPoint.Suppress();
                actualOrder.Add(result.Result);
                Output.WriteLine(result.Result);
                //SchedulingPoint.Resume();
            }
        });

        await Task.WhenAll(p1, c1);
        //Output.WriteLine(c1.Status.ToString());


        // Assert that the tasks were processed in the correct order
        //expectedOrder.SequenceEqual(actualOrder).Should().BeTrue();
        Assert.Equal(expectedOrder, actualOrder);
    }

    private async Task TestMultipleProducersMultipleConsumers(IActorRuntime runtime)
    {
        //var expectedOrder = new List<string> { "Producer1", "Producer2", "Producer3" };
        //var actualOrder   = new string[expectedOrder.Count];
        //var currentIndex  = -1;

        //var t1 = Task.Run(async () =>
        //{
        //    await Task.Delay(1000);
        //    var index = Interlocked.Increment(ref currentIndex);
        //    actualOrder[index] = "Producer1";
        //});

        //var t2 = Task.Run(async () =>
        //{
        //    await Task.Delay(2000);
        //    var index = Interlocked.Increment(ref currentIndex);
        //    actualOrder[index] = "Producer2";
        //});

        //var t3 = Task.Run(async () =>
        //{
        //    await Task.Delay(3000);
        //    var index = Interlocked.Increment(ref currentIndex);
        //    actualOrder[index] = "Producer3";
        //});

        //await Task.WhenAll(t1, t2, t3);
        //Assert.Equal(expectedOrder, actualOrder);

        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
        var expectedOrder = new List<string> { "Producer1_Task2", "Producer2_Task2", "Producer2_Task1", "Producer1_Task1" };
        var actualOrder = new string[expectedOrder.Count];

        var currentIndex = -1;

        // Producer 1: Add tasks to the queue
        var p1 = Task.Run(() =>
        {
            taskQueue.Add(SimulateTask("Producer1_Task1", 3000));  // Long-running task
            taskQueue.Add(SimulateTask("Producer1_Task2", 1000));  // Short-running task
        });

        // Producer 2: Add tasks to the queue
        var p2 = Task.Run(() =>
        {
            taskQueue.Add(SimulateTask("Producer2_Task1", 2000));  // Medium-running task
            taskQueue.Add(SimulateTask("Producer2_Task2", 1500));  // Medium-short task
        });

        Task.WhenAll(p1, p2).ContinueWith(_ => taskQueue.CompleteAdding());

        // Consumer 1: Consume tasks in completion order and store the result
        var c1 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                var index = Interlocked.Increment(ref currentIndex);
                actualOrder[index] = result.Result;
                await Task.Yield();
            }
        });

        // Consumer 2: Consume tasks in completion order and store the result
        var c2 = Task.Run(async () =>
        {
            await foreach (var result in taskQueue.GetConsumingEnumerable())
            {
                var index = Interlocked.Increment(ref currentIndex);
                actualOrder[index] = result.Result;
                await Task.Yield();
            }
        });

        await Task.WhenAll(c1, c2);

        // Assert that the tasks were processed in the correct order
        Assert.Equal(expectedOrder, actualOrder);
    }

    private async Task<string> SimulateTask(string name, int delay)
    {
        //Thread.Sleep(delay);
        await Task.Delay(delay);  // Simulate work
        return name;
    }
}