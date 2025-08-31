using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Collections.Generic;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Memory leak detection tests for Observable chains in Binnaculum's ReactiveUI architecture.
/// Tests memory management, disposal patterns, and subscription lifecycle.
/// </summary>
public class ObservableMemoryLeakTests
{
    #region WeakReference Memory Leak Tests

    [Fact]
    public void Observable_Subscription_WithoutDisposal_CausesMemoryLeak()
    {
        // Arrange
        var source = new Subject<int>();
        var receivedValues = new List<int>();
        WeakReference<List<int>> weakRef;
        
        // Create a scope that should be garbage collected
        {
            var scopedList = new List<int>();
            weakRef = new WeakReference<List<int>>(scopedList);
            
            // Subscribe without disposing - this creates a memory leak
            source.Subscribe(value => scopedList.Add(value));
        }
        
        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert - The weak reference should still be alive due to the subscription
        Assert.True(weakRef.TryGetTarget(out _), 
            "Subscribed object should still be alive due to Observable subscription memory leak");
    }

    [Fact]
    public void Observable_Subscription_WithProperDisposal_AllowsGarbageCollection()
    {
        // Arrange
        var source = new Subject<int>();
        WeakReference<List<int>> weakRef;
        IDisposable? subscription = null;
        
        // Create a scope that should be garbage collected
        {
            var scopedList = new List<int>();
            weakRef = new WeakReference<List<int>>(scopedList);
            
            // Subscribe and capture the subscription
            subscription = source.Subscribe(value => scopedList.Add(value));
        }
        
        // Act - Dispose the subscription before GC
        subscription!.Dispose();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert - The weak reference should be collected
        Assert.False(weakRef.TryGetTarget(out _), 
            "Disposed subscription should allow object to be garbage collected");
    }

    [Fact]
    public void CompositeDisposable_Pattern_DisposesAllSubscriptions()
    {
        // Arrange
        var source1 = new Subject<int>();
        var source2 = new Subject<string>();
        var source3 = new Subject<decimal>();
        var disposables = new CompositeDisposable();
        
        var receivedInts = new List<int>();
        var receivedStrings = new List<string>();
        var receivedDecimals = new List<decimal>();
        
        // Act - Create multiple subscriptions using CompositeDisposable pattern
        source1
            .Subscribe(value => receivedInts.Add(value))
            .DisposeWith(disposables);
            
        source2
            .Subscribe(value => receivedStrings.Add(value))
            .DisposeWith(disposables);
            
        source3
            .Subscribe(value => receivedDecimals.Add(value))
            .DisposeWith(disposables);
        
        // Emit some values
        source1.OnNext(1);
        source2.OnNext("test");
        source3.OnNext(100.50m);
        
        // Dispose all subscriptions
        disposables.Dispose();
        
        // Try to emit more values after disposal
        source1.OnNext(2);
        source2.OnNext("after disposal");
        source3.OnNext(200.75m);
        
        // Assert - Only values before disposal should be received
        Assert.Single(receivedInts);
        Assert.Equal(1, receivedInts[0]);
        
        Assert.Single(receivedStrings);
        Assert.Equal("test", receivedStrings[0]);
        
        Assert.Single(receivedDecimals);
        Assert.Equal(100.50m, receivedDecimals[0]);
    }

    #endregion

    #region Subject Memory Leak Tests

    [Fact]
    public void Subject_WithoutCompletion_HoldsSubscriberReferences()
    {
        // Arrange
        var subject = new Subject<string>();
        WeakReference<MockSubscriber> weakRef;
        
        {
            var subscriber = new MockSubscriber();
            weakRef = new WeakReference<MockSubscriber>(subscriber);
            
            // Subscribe to the subject
            subject.Subscribe(subscriber.OnNext);
        }
        
        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert - Subject holds reference to subscriber
        Assert.True(weakRef.TryGetTarget(out _), 
            "Subject should hold reference to subscriber until completed or disposed");
    }

    [Fact]
    public void Subject_WithCompletion_ReleasesSubscriberReferences()
    {
        // Arrange
        var subject = new Subject<string>();
        WeakReference<MockSubscriber> weakRef;
        
        {
            var subscriber = new MockSubscriber();
            weakRef = new WeakReference<MockSubscriber>(subscriber);
            
            // Subscribe to the subject
            subject.Subscribe(subscriber.OnNext);
            
            // Complete the subject
            subject.OnCompleted();
        }
        
        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert - Completed subject should release references
        Assert.False(weakRef.TryGetTarget(out _), 
            "Completed subject should release subscriber references");
    }

    #endregion

    #region Binnaculum-Specific Observable Patterns

    [Fact]
    public void BrokerAccountTemplate_ObservableMerge_MemoryManagement()
    {
        // Arrange - Simulate BrokerAccountTemplate's Observable.Merge pattern
        var addClicked = new Subject<Unit>();
        var containerTapped = new Subject<Unit>();
        var textTapped = new Subject<Unit>();
        var disposables = new CompositeDisposable();
        
        var navigationCallCount = 0;
        WeakReference<object> weakNavigationTarget;
        
        {
            var navigationTarget = new object(); // Simulates navigation service
            weakNavigationTarget = new WeakReference<object>(navigationTarget);
            
            // Act - Create the merged observable chain like in BrokerAccountTemplate
            Observable
                .Merge(
                    addClicked.Select(_ => Unit.Default),
                    containerTapped.Select(_ => Unit.Default),
                    textTapped.Select(_ => Unit.Default))
                .Where(_ => navigationTarget != null)
                .Subscribe(_ => {
                    navigationCallCount++;
                    // Simulate navigation using the target
                    GC.KeepAlive(navigationTarget);
                })
                .DisposeWith(disposables);
        }
        
        // Emit some events
        addClicked.OnNext(Unit.Default);
        containerTapped.OnNext(Unit.Default);
        
        // Dispose the chain
        disposables.Dispose();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert - Navigation target should be collected after disposal
        Assert.Equal(2, navigationCallCount);
        Assert.False(weakNavigationTarget.TryGetTarget(out _), 
            "Navigation target should be garbage collected after Observable chain disposal");
    }

    [Fact]
    public async Task PercentageControl_ObservableChain_ProperCleanup()
    {
        // Arrange - Simulate a percentage control with observable updates
        var percentageUpdates = new Subject<decimal>();
        var disposables = new CompositeDisposable();
        WeakReference<MockPercentageDisplay> weakDisplay;
        
        {
            var display = new MockPercentageDisplay();
            weakDisplay = new WeakReference<MockPercentageDisplay>(display);
            
            // Create observable chain for percentage updates
            percentageUpdates
                .Where(p => Math.Abs(p) < 1000) // Reasonable percentage bounds
                .Select(p => new { 
                    Value = (int)Math.Truncate(p),
                    Decimals = $".{Math.Abs(p % 1):F2}".Substring(2, 2),
                    IsPositive = p >= 0
                })
                .Subscribe(update => {
                    display.UpdateValue(update.Value, update.Decimals, update.IsPositive);
                })
                .DisposeWith(disposables);
        }
        
        // Act - Send updates and then dispose
        percentageUpdates.OnNext(15.75m);
        percentageUpdates.OnNext(-8.42m);
        
        // Wait a brief moment for updates to process
        await Task.Delay(10);
        
        disposables.Dispose();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert
        Assert.False(weakDisplay.TryGetTarget(out _), 
            "Percentage display should be garbage collected after Observable disposal");
    }

    [Fact]
    public void InvestmentSnapshot_ObservableUpdates_NoMemoryLeaks()
    {
        // Arrange - Simulate investment snapshot updates with proper disposal
        var snapshotUpdates = new Subject<decimal>();
        var disposables = new CompositeDisposable();
        var updateCount = 0;
        WeakReference<List<decimal>> weakHistory;
        
        {
            var priceHistory = new List<decimal>();
            weakHistory = new WeakReference<List<decimal>>(priceHistory);
            
            // Create observable chain for investment updates
            snapshotUpdates
                .Buffer(TimeSpan.FromMilliseconds(100)) // Batch updates
                .Where(batch => batch.Count > 0)
                .SelectMany(batch => batch)
                .Subscribe(price => {
                    priceHistory.Add(price);
                    updateCount++;
                })
                .DisposeWith(disposables);
        }
        
        // Act - Send rapid updates
        for (int i = 0; i < 10; i++)
        {
            snapshotUpdates.OnNext(100m + i);
        }
        
        // Wait for buffering
        Thread.Sleep(150);
        
        disposables.Dispose();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert
        Assert.Equal(10, updateCount);
        Assert.False(weakHistory.TryGetTarget(out _), 
            "Price history should be garbage collected after disposal");
    }

    #endregion

    #region Test Helper Classes

    private class MockSubscriber
    {
        private readonly List<string> _receivedValues = new();
        
        public void OnNext(string value)
        {
            _receivedValues.Add(value);
        }
        
        public IReadOnlyList<string> ReceivedValues => _receivedValues.AsReadOnly();
    }

    private class MockPercentageDisplay
    {
        public int Value { get; private set; }
        public string Decimals { get; private set; } = string.Empty;
        public bool IsPositive { get; private set; }
        
        public void UpdateValue(int value, string decimals, bool isPositive)
        {
            Value = value;
            Decimals = decimals;
            IsPositive = isPositive;
        }
    }

    #endregion

    #region Performance Memory Tests

    [Fact]
    public void Observable_HighFrequencyUpdates_BoundedMemoryUsage()
    {
        // Arrange
        var source = new Subject<decimal>();
        var disposables = new CompositeDisposable();
        var processedCount = 0;
        
        // Get initial memory usage
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act - Create observable chain that processes high-frequency updates
        source
            .Sample(TimeSpan.FromMilliseconds(16)) // ~60 FPS sampling rate
            .Subscribe(_ => processedCount++)
            .DisposeWith(disposables);
        
        // Send many rapid updates
        for (int i = 0; i < 1000; i++)
        {
            source.OnNext(100m + (decimal)Math.Sin(i * 0.1));
        }
        
        // Wait for processing
        Thread.Sleep(100);
        
        disposables.Dispose();
        
        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Assert - Memory increase should be bounded for mobile performance
        Assert.True(processedCount > 0, "Should have processed some updates");
        Assert.True(processedCount < 1000, "Should have sampled updates, not processed all");
        Assert.True(memoryIncrease < 1024 * 1024, // Less than 1MB increase
            $"Memory increase of {memoryIncrease} bytes should be bounded for mobile performance");
    }

    [Fact]
    public void Observable_LongRunningChain_DoesNotAccumulateMemory()
    {
        // Arrange
        var source = new Subject<int>();
        var disposables = new CompositeDisposable();
        var results = new List<int>();
        
        // Create a chain that could potentially accumulate memory
        source
            .Scan(0, (acc, value) => acc + value)
            .Where(sum => sum % 10 == 0) // Filter to reduce frequency
            .Take(10) // Limit to prevent infinite accumulation
            .Subscribe(results.Add)
            .DisposeWith(disposables);
        
        // Act - Send many values
        for (int i = 1; i <= 100; i++)
        {
            source.OnNext(i);
        }
        
        disposables.Dispose();
        
        // Assert
        Assert.True(results.Count <= 10, "Should not accumulate more results than specified");
        Assert.All(results, r => Assert.True(r % 10 == 0, "All results should be multiples of 10"));
    }

    #endregion
}