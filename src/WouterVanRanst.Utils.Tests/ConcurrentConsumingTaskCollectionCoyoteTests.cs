using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using WouterVanRanst.Utils.Collections;

public class TaskCompletionBufferCoyoteTests
{
    /// <summary>
    /// Tests concurrent addition of tasks and completion signaling to ensure
    /// all tasks are processed and no invalid operations occur.
    /// </summary>
    [Fact]
    public static async Task TestConcurrentAddAndCompleteAdding()
    {
        var configuration = Configuration.Create().WithTestingIterations(100);
        var engine = TestingEngine.Create(configuration, async (runtime) =>
        {
            var buffer = new TaskCompletionBuffer<int>();
            var pendingTasks = new List<TaskCompletionSource<int>>();
            int numTasks = 10;

            // Add tasks concurrently
            var producer = Task.Run(() =>
            {
                for (int i = 0; i < numTasks; i++)
                {
                    var tcs = new TaskCompletionSource<int>();
                    lock (buffer)
                    {
                        buffer.Add(tcs.Task);
                        pendingTasks.Add(tcs);
                    }
                }
            });

            await producer;

            // Signal completion concurrently
            var completer = Task.Run(() => buffer.CompleteAdding());

            await Task.WhenAll(producer, completer);

            // Complete all tasks
            foreach (var tcs in pendingTasks)
            {
                tcs.SetResult(42);
            }

            // Collect results
            var results = new ConcurrentBag<int>();
            await foreach (var task in buffer.GetConsumingEnumerable())
            {
                results.Add(await task);
            }

            Assert.Equal(results.Count, numTasks);
        });

        engine.Run();
        var testResult = engine.TestReport;

        Assert.Equal(0, testResult.NumOfFoundBugs);
    }

    /// <summary>
    /// Verifies that tasks added after CompleteAdding is called throw exceptions
    /// and that existing tasks are processed correctly.
    /// </summary>
    [Fact]
    public static async Task TestAddAfterCompleteAddingThrows()
    {
        await RunCoyoteTest(async (buffer) =>
        {
            buffer.CompleteAdding();

            bool exceptionThrown = false;
            try
            {
                buffer.Add(Task.FromResult(1));
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Add after CompleteAdding should throw");
        });
    }

    /// <summary>
    /// Ensures all tasks are processed exactly once even with multiple consumers.
    /// </summary>
    [Fact]
    public static async Task TestMultipleConsumersProcessAllTasks()
    {
        await RunCoyoteTest(async (buffer) =>
        {
            int numTasks = 20;
            var pendingTasks = new List<TaskCompletionSource<int>>();

            for (int i = 0; i < numTasks; i++)
            {
                var tcs = new TaskCompletionSource<int>();
                buffer.Add(tcs.Task);
                pendingTasks.Add(tcs);
            }

            buffer.CompleteAdding();

            var results = new ConcurrentBag<int>();
            var consumer1 = ConsumeAsync(buffer, results);
            var consumer2 = ConsumeAsync(buffer, results);

            // Complete tasks in random order
            var random = new Random();
            while (pendingTasks.Count > 0)
            {
                int index = random.Next(pendingTasks.Count);
                pendingTasks[index].SetResult(pendingTasks.Count);
                pendingTasks.RemoveAt(index);
            }

            await Task.WhenAll(consumer1, consumer2);

            numTasks.Should().Be(results.Count, $"Expected {numTasks} results, got {results.Count}");
        });
    }

    ///// <summary>
    ///// Validates that the buffer handles the completion order correctly when
    ///// tasks complete before being awaited.
    ///// </summary>
    //[Test]
    //public static async Task TestCompletionOrderWithControlledTasks()
    //{
    //    await RunCoyoteTest(async (buffer) =>
    //    {
    //        var tcs1 = new TaskCompletionSource<int>();
    //        var tcs2 = new TaskCompletionSource<int>();

    //        buffer.Add(tcs2.Task); // This task completes first
    //        buffer.Add(tcs1.Task);

    //        tcs2.SetResult(2);
    //        tcs1.SetResult(1);
    //        buffer.CompleteAdding();

    //        var results = new List<int>();
    //        await foreach (var task in buffer.GetConsumingEnumerable())
    //        {
    //            results.Add(await task);
    //        }

    //        Assert(results.Count == 2);
    //        Assert(results[0] == 2, "First completed task should be result 2");
    //        Assert(results[1] == 1, "Second completed task should be result 1");
    //    });
    //}

    // Helper methods
    private static async Task RunCoyoteTest(Func<TaskCompletionBuffer<int>, Task> testFunc)
    {
        var config = Configuration.Create().WithTestingIterations(100);
        var engine = TestingEngine.Create(config, async (runtime) =>
        {
            var buffer = new TaskCompletionBuffer<int>();
            await testFunc(buffer);
        });

        engine.Run();
        var testResult = engine.TestReport;

        Assert.Equal(0, testResult.NumOfFoundBugs);
    }

    private static async Task ConsumeAsync(TaskCompletionBuffer<int> buffer, ConcurrentBag<int> results)
    {
        await foreach (var task in buffer.GetConsumingEnumerable())
        {
            results.Add(await task);
        }
    }
}


//using FluentAssertions;
//using Microsoft.Coyote;
//using Microsoft.Coyote.Actors;
//using Microsoft.Coyote.Runtime;
//using Microsoft.Coyote.SystematicTesting;
//using WouterVanRanst.Utils.Collections;
//using Xunit.Abstractions;

//namespace WouterVanRanst.Utils.Tests;

//public class ConcurrentConsumingTaskCollectionCoyoteTests
//{
//    ITestOutputHelper Output;

//    public ConcurrentConsumingTaskCollectionCoyoteTests(ITestOutputHelper output)
//    {
//        this.Output = output;
//    }


//    [Fact]
//    public void TestTaskQueueSingleProducerSingleConsumer()
//    {
//        return;

//        var configuration = Configuration.Create()
//                .WithReproducibleTrace(File.ReadAllText("C:\\Users\\WouterVanRanst\\Desktop\\mytest.trace"))
//                //.WithDeadlockTimeout(10000)
//                //.WithVerbosityEnabled()
//            ;
//        var engine = TestingEngine.Create(configuration, this.TestSingleProducerSingleConsumer);
//        engine.Run();
//        var report = engine.TestReport;
//        Output.WriteLine("Coyote found {0} bug.", report.NumOfFoundBugs);

//        engine.TryEmitReports("C:\\Users\\WouterVanRanst\\Desktop", "mytest", out var filenames);
//        //foreach (var item in filenames)
//        //{
//        //    Output.WriteLine("See log file: {0}", item);
//        //}

//        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
//    }

//    [Fact]
//    public void TestTaskQueueMultipleProducersMultipleConsumers()
//    {
//        return;

//        var configuration = Configuration.Create()
//            //.WithReproducibleTrace(File.ReadAllText("C:\\Users\\WouterVanRanst\\Desktop\\mytest2.trace"))
//            //.WithTestingIterations(100)
//            ;
//        var engine = TestingEngine.Create(configuration, this.TestMultipleProducersMultipleConsumers);
//        engine.Run();

//        //engine.TryEmitReports("C:\\Users\\WouterVanRanst\\Desktop", "mytest2", out var filenames);

//        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
//    }

//    private async Task TestSingleProducerSingleConsumer(IActorRuntime runtime)
//    {
//        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
//        var actualOrder = new List<string>();
//        var expectedOrder = new List<string> { "Task2", "Task3", "Task1" };  // Expected order based on task delays

//        // Producer: Add tasks to the queue
//        var p1 = Task.Run(async () =>
//        {
//            taskQueue.Add(SimulateTask("Task1", 300));  // Long-running task
//            taskQueue.Add(SimulateTask("Task2", 100));  // Short-running task
//            taskQueue.Add(SimulateTask("Task3", 200));  // Medium-running task
//            taskQueue.CompleteAdding();
//        });

//        // Consumer: Consume tasks in completion order and store the result
//        var c1 = Task.Run(async () =>
//        {
//            await foreach (var result in taskQueue.GetConsumingEnumerable())
//            {
//                //SchedulingPoint.Suppress();
//                actualOrder.Add(result.Result);
//                Output.WriteLine(result.Result);
//                //SchedulingPoint.Resume();
//            }
//        });

//        await Task.WhenAll(p1, c1);
//        //Output.WriteLine(c1.Status.ToString());


//        // Assert that the tasks were processed in the correct order
//        //expectedOrder.SequenceEqual(actualOrder).Should().BeTrue();
//        Assert.Equal(expectedOrder, actualOrder);
//    }

//    private async Task TestMultipleProducersMultipleConsumers(IActorRuntime runtime)
//    {
//        //var expectedOrder = new List<string> { "Producer1", "Producer2", "Producer3" };
//        //var actualOrder   = new string[expectedOrder.Count];
//        //var currentIndex  = -1;

//        //var t1 = Task.Run(async () =>
//        //{
//        //    await Task.Delay(1000);
//        //    var index = Interlocked.Increment(ref currentIndex);
//        //    actualOrder[index] = "Producer1";
//        //});

//        //var t2 = Task.Run(async () =>
//        //{
//        //    await Task.Delay(2000);
//        //    var index = Interlocked.Increment(ref currentIndex);
//        //    actualOrder[index] = "Producer2";
//        //});

//        //var t3 = Task.Run(async () =>
//        //{
//        //    await Task.Delay(3000);
//        //    var index = Interlocked.Increment(ref currentIndex);
//        //    actualOrder[index] = "Producer3";
//        //});

//        //await Task.WhenAll(t1, t2, t3);
//        //Assert.Equal(expectedOrder, actualOrder);

//        var taskQueue = new ConcurrentConsumingTaskCollection<string>();
//        var expectedOrder = new List<string> { "Producer1_Task2", "Producer2_Task2", "Producer2_Task1", "Producer1_Task1" };
//        var actualOrder = new string[expectedOrder.Count];

//        var currentIndex = -1;

//        // Producer 1: Add tasks to the queue
//        var p1 = Task.Run(() =>
//        {
//            taskQueue.Add(SimulateTask("Producer1_Task1", 3000));  // Long-running task
//            taskQueue.Add(SimulateTask("Producer1_Task2", 1000));  // Short-running task
//        });

//        // Producer 2: Add tasks to the queue
//        var p2 = Task.Run(() =>
//        {
//            taskQueue.Add(SimulateTask("Producer2_Task1", 2000));  // Medium-running task
//            taskQueue.Add(SimulateTask("Producer2_Task2", 1500));  // Medium-short task
//        });

//        Task.WhenAll(p1, p2).ContinueWith(_ => taskQueue.CompleteAdding());

//        // Consumer 1: Consume tasks in completion order and store the result
//        var c1 = Task.Run(async () =>
//        {
//            await foreach (var result in taskQueue.GetConsumingEnumerable())
//            {
//                var index = Interlocked.Increment(ref currentIndex);
//                actualOrder[index] = result.Result;
//                await Task.Yield();
//            }
//        });

//        // Consumer 2: Consume tasks in completion order and store the result
//        var c2 = Task.Run(async () =>
//        {
//            await foreach (var result in taskQueue.GetConsumingEnumerable())
//            {
//                var index = Interlocked.Increment(ref currentIndex);
//                actualOrder[index] = result.Result;
//                await Task.Yield();
//            }
//        });

//        await Task.WhenAll(c1, c2);

//        // Assert that the tasks were processed in the correct order
//        Assert.Equal(expectedOrder, actualOrder);
//    }

//    private async Task<string> SimulateTask(string name, int delay)
//    {
//        //Thread.Sleep(delay);
//        await Task.Delay(delay);  // Simulate work
//        return name;
//    }
//}