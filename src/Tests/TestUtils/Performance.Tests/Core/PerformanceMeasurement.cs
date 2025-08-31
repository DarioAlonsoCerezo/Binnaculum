using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Binnaculum.Tests.TestUtils.Performance;

/// <summary>
/// Performance metrics collected during test execution
/// </summary>
public class PerformanceMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public object? Result { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Memory usage in MB (convenience property)
    /// </summary>
    public double MemoryUsedMB => MemoryUsedBytes / (1024.0 * 1024.0);
    
    /// <summary>
    /// Total GC collections across all generations
    /// </summary>
    public int TotalGCCollections => Gen0Collections + Gen1Collections + Gen2Collections;
}

/// <summary>
/// Core performance measurement utilities based on F# BrokerFinancialSnapshotManagerPerformanceTests patterns
/// </summary>
public static class PerformanceMeasurement
{
    /// <summary>
    /// Measure performance of a synchronous operation
    /// Follows the measureTime pattern from BrokerFinancialSnapshotManagerPerformanceTests.fs
    /// </summary>
    public static PerformanceMetrics MeasureSync<T>(Func<T> operation, string operationName = "Operation")
    {
        // Force garbage collection before measurement for accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryBefore = GC.GetTotalMemory(false);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        
        var stopwatch = Stopwatch.StartNew();
        var result = operation();
        stopwatch.Stop();
        
        var memoryAfter = GC.GetTotalMemory(false);
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);
        
        return new PerformanceMetrics
        {
            OperationName = operationName,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = memoryAfter - memoryBefore,
            Gen0Collections = gen0After - gen0Before,
            Gen1Collections = gen1After - gen1Before,
            Gen2Collections = gen2After - gen2Before,
            Result = result
        };
    }
    
    /// <summary>
    /// Measure performance of an asynchronous operation
    /// </summary>
    public static async Task<PerformanceMetrics> MeasureAsync<T>(Func<Task<T>> operation, string operationName = "AsyncOperation")
    {
        // Force garbage collection before measurement for accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryBefore = GC.GetTotalMemory(false);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        
        var stopwatch = Stopwatch.StartNew();
        var result = await operation();
        stopwatch.Stop();
        
        var memoryAfter = GC.GetTotalMemory(false);
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);
        
        return new PerformanceMetrics
        {
            OperationName = operationName,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = memoryAfter - memoryBefore,
            Gen0Collections = gen0After - gen0Before,
            Gen1Collections = gen1After - gen1Before,
            Gen2Collections = gen2After - gen2Before,
            Result = result
        };
    }
    
    /// <summary>
    /// Run multiple iterations of a performance test and return average metrics
    /// Similar to the performance regression baseline pattern from F# tests
    /// </summary>
    public static PerformanceMetrics MeasureMultipleRuns<T>(Func<T> operation, int iterations = 5, string operationName = "MultipleRuns")
    {
        var metrics = new List<PerformanceMetrics>();
        
        for (int i = 0; i < iterations; i++)
        {
            metrics.Add(MeasureSync(operation, $"{operationName}_Run{i + 1}"));
            
            // Allow GC between runs
            if (i < iterations - 1)
            {
                Task.Delay(10).Wait();
            }
        }
        
        return new PerformanceMetrics
        {
            OperationName = $"{operationName}_Average",
            ElapsedMilliseconds = (long)metrics.Average(m => m.ElapsedMilliseconds),
            MemoryUsedBytes = (long)metrics.Average(m => m.MemoryUsedBytes),
            Gen0Collections = (int)metrics.Average(m => m.Gen0Collections),
            Gen1Collections = (int)metrics.Average(m => m.Gen1Collections),
            Gen2Collections = (int)metrics.Average(m => m.Gen2Collections),
            Result = metrics.Select(m => m.Result).ToArray()
        };
    }
    
    /// <summary>
    /// Assert performance meets requirements
    /// Based on patterns from BuildPerformanceTests.cs
    /// </summary>
    public static void AssertPerformanceRequirements(
        PerformanceMetrics metrics,
        long maxTimeMs,
        long maxMemoryBytes,
        int maxGCCollections = int.MaxValue)
    {
        if (metrics.ElapsedMilliseconds > maxTimeMs)
        {
            throw new AssertionException(
                $"{metrics.OperationName} took {metrics.ElapsedMilliseconds}ms, should be < {maxTimeMs}ms");
        }
        
        if (Math.Abs(metrics.MemoryUsedBytes) > maxMemoryBytes)
        {
            throw new AssertionException(
                $"{metrics.OperationName} used {metrics.MemoryUsedMB:F1}MB memory, should be < {maxMemoryBytes / 1024.0 / 1024.0:F1}MB");
        }
        
        if (metrics.TotalGCCollections > maxGCCollections)
        {
            throw new AssertionException(
                $"{metrics.OperationName} triggered {metrics.TotalGCCollections} GC collections, should be < {maxGCCollections}");
        }
    }
    
    /// <summary>
    /// Log performance metrics to console
    /// Following the console output pattern from F# performance tests
    /// </summary>
    public static void LogMetrics(PerformanceMetrics metrics)
    {
        Console.WriteLine($"{metrics.OperationName}: {metrics.ElapsedMilliseconds}ms");
        if (Math.Abs(metrics.MemoryUsedBytes) > 1024)
        {
            Console.WriteLine($"Memory delta: {metrics.MemoryUsedMB:F1}MB");
        }
        if (metrics.TotalGCCollections > 0)
        {
            Console.WriteLine($"GC Collections - Gen0: {metrics.Gen0Collections}, Gen1: {metrics.Gen1Collections}, Gen2: {metrics.Gen2Collections}");
        }
    }
}