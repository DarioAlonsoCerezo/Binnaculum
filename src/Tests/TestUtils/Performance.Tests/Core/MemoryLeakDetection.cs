using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NUnit.Framework;

namespace Binnaculum.Tests.TestUtils.Performance;

/// <summary>
/// Memory leak detection utilities for Observable chains and ReactiveUI components
/// Based on patterns from troubleshooting.md and existing performance tests
/// </summary>
public static class MemoryLeakDetection
{
    /// <summary>
    /// Track memory usage during Observable operations to detect leaks
    /// </summary>
    public class MemoryTracker : IDisposable
    {
        private readonly long _initialMemory;
        private readonly int _initialGen0;
        private readonly int _initialGen1;
        private readonly int _initialGen2;
        private readonly DateTime _startTime;
        private bool _disposed = false;
        
        public MemoryTracker()
        {
            // Force GC for accurate baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            _initialMemory = GC.GetTotalMemory(false);
            _initialGen0 = GC.CollectionCount(0);
            _initialGen1 = GC.CollectionCount(1);
            _initialGen2 = GC.CollectionCount(2);
            _startTime = DateTime.UtcNow;
        }
        
        public long PeakMemoryBytes { get; private set; }
        public double PeakMemoryMB => PeakMemoryBytes / (1024.0 * 1024.0);
        
        public MemorySnapshot TakeSnapshot(string description = "")
        {
            var currentMemory = GC.GetTotalMemory(false);
            var memoryDelta = currentMemory - _initialMemory;
            
            if (currentMemory > PeakMemoryBytes)
                PeakMemoryBytes = currentMemory;
            
            return new MemorySnapshot
            {
                Description = description,
                MemoryDelta = memoryDelta,
                TotalMemory = currentMemory,
                Gen0Collections = GC.CollectionCount(0) - _initialGen0,
                Gen1Collections = GC.CollectionCount(1) - _initialGen1,
                Gen2Collections = GC.CollectionCount(2) - _initialGen2,
                ElapsedTime = DateTime.UtcNow - _startTime
            };
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                // Force GC for final measurement
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                _disposed = true;
            }
        }
    }
    
    public class MemorySnapshot
    {
        public string Description { get; set; } = string.Empty;
        public long MemoryDelta { get; set; }
        public long TotalMemory { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        
        public double MemoryDeltaMB => MemoryDelta / (1024.0 * 1024.0);
        public int TotalGCCollections => Gen0Collections + Gen1Collections + Gen2Collections;
    }
    
    /// <summary>
    /// Test Observable chain for memory leaks
    /// Based on the pattern from troubleshooting.md
    /// </summary>
    public static async Task<MemoryLeakTestResult> TestObservableMemoryLeak<T>(
        Func<CompositeDisposable, IObservable<T>> observableFactory,
        int iterations = 1000,
        TimeSpan? testDuration = null)
    {
        using var memoryTracker = new MemoryTracker();
        var disposables = new CompositeDisposable();
        var results = new List<T>();
        
        try
        {
            var observable = observableFactory(disposables);
            var subscription = observable
                .Take(iterations)
                .Subscribe(value => results.Add(value))
                .DisposeWith(disposables);
            
            // Wait for completion or timeout
            var timeout = testDuration ?? TimeSpan.FromSeconds(10);
            var start = DateTime.UtcNow;
            
            while (results.Count < iterations && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(10);
            }
            
            var beforeDisposeSnapshot = memoryTracker.TakeSnapshot("BeforeDispose");
            
            // Dispose all subscriptions
            disposables.Dispose();
            
            // Allow cleanup time
            await Task.Delay(100);
            
            // Force GC to clean up disposed objects
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterDisposeSnapshot = memoryTracker.TakeSnapshot("AfterDispose");
            
            return new MemoryLeakTestResult
            {
                BeforeDisposeSnapshot = beforeDisposeSnapshot,
                AfterDisposeSnapshot = afterDisposeSnapshot,
                ResultCount = results.Count,
                PeakMemoryMB = memoryTracker.PeakMemoryMB,
                HasMemoryLeak = DetectMemoryLeak(beforeDisposeSnapshot, afterDisposeSnapshot)
            };
        }
        catch (Exception ex)
        {
            disposables.Dispose();
            throw new InvalidOperationException($"Observable memory leak test failed: {ex.Message}", ex);
        }
    }
    
    private static bool DetectMemoryLeak(MemorySnapshot before, MemorySnapshot after)
    {
        // Memory should decrease or stay approximately the same after disposal
        // Allow for some tolerance due to GC timing
        const double toleranceMB = 5.0; // 5MB tolerance
        
        var memoryIncrease = after.MemoryDeltaMB - before.MemoryDeltaMB;
        return memoryIncrease > toleranceMB;
    }
    
    public class MemoryLeakTestResult
    {
        public MemorySnapshot BeforeDisposeSnapshot { get; set; } = new();
        public MemorySnapshot AfterDisposeSnapshot { get; set; } = new();
        public int ResultCount { get; set; }
        public double PeakMemoryMB { get; set; }
        public bool HasMemoryLeak { get; set; }
        
        public void AssertNoMemoryLeak()
        {
            if (HasMemoryLeak)
            {
                Assert.Fail(
                    $"Memory leak detected! Memory increased from {BeforeDisposeSnapshot.MemoryDeltaMB:F1}MB " +
                    $"to {AfterDisposeSnapshot.MemoryDeltaMB:F1}MB after disposal. " +
                    $"Peak memory usage: {PeakMemoryMB:F1}MB. " +
                    $"GC collections - Before: {BeforeDisposeSnapshot.TotalGCCollections}, " +
                    $"After: {AfterDisposeSnapshot.TotalGCCollections}");
            }
        }
    }
    
    /// <summary>
    /// Simulate high memory pressure to test Observable cleanup under stress
    /// Based on memory pressure patterns from F# performance tests
    /// </summary>
    public static async Task<MemoryPressureTestResult> TestObservableUnderMemoryPressure<T>(
        Func<CompositeDisposable, IObservable<T>> observableFactory,
        int memoryPressureMB = 50)
    {
        using var memoryTracker = new MemoryTracker();
        var disposables = new CompositeDisposable();
        var results = new List<T>();
        
        try
        {
            // Create memory pressure
            var pressureData = new List<byte[]>();
            var pressureBytes = memoryPressureMB * 1024 * 1024;
            var chunkSize = 1024 * 1024; // 1MB chunks
            
            for (int i = 0; i < pressureBytes / chunkSize; i++)
            {
                pressureData.Add(new byte[chunkSize]);
            }
            
            var pressureSnapshot = memoryTracker.TakeSnapshot("MemoryPressureCreated");
            
            // Create and run Observable under memory pressure
            var observable = observableFactory(disposables);
            var subscription = observable
                .Take(100)
                .Subscribe(value => results.Add(value))
                .DisposeWith(disposables);
            
            // Wait for completion
            var timeout = TimeSpan.FromSeconds(10);
            var start = DateTime.UtcNow;
            
            while (results.Count < 100 && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(10);
            }
            
            var completionSnapshot = memoryTracker.TakeSnapshot("ObservableCompleted");
            
            // Dispose Observable
            disposables.Dispose();
            
            // Clear memory pressure
            pressureData.Clear();
            
            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            await Task.Delay(100);
            
            var finalSnapshot = memoryTracker.TakeSnapshot("CleanupCompleted");
            
            return new MemoryPressureTestResult
            {
                PressureSnapshot = pressureSnapshot,
                CompletionSnapshot = completionSnapshot,
                FinalSnapshot = finalSnapshot,
                ResultCount = results.Count,
                PeakMemoryMB = memoryTracker.PeakMemoryMB
            };
        }
        catch (Exception ex)
        {
            disposables.Dispose();
            throw new InvalidOperationException($"Memory pressure test failed: {ex.Message}", ex);
        }
    }
    
    public class MemoryPressureTestResult
    {
        public MemorySnapshot PressureSnapshot { get; set; } = new();
        public MemorySnapshot CompletionSnapshot { get; set; } = new();
        public MemorySnapshot FinalSnapshot { get; set; } = new();
        public int ResultCount { get; set; }
        public double PeakMemoryMB { get; set; }
        
        public void AssertHandledMemoryPressure()
        {
            // Observable should complete despite memory pressure
            Assert.That(ResultCount, Is.GreaterThan(0), "Observable should produce results under memory pressure");
            
            // Memory should be cleaned up after disposal
            var memoryIncrease = FinalSnapshot.MemoryDeltaMB - PressureSnapshot.MemoryDeltaMB;
            Assert.That(memoryIncrease, Is.LessThan(10.0), // 10MB tolerance
                $"Memory not properly cleaned up after disposal. Increase: {memoryIncrease:F1}MB");
        }
    }
}