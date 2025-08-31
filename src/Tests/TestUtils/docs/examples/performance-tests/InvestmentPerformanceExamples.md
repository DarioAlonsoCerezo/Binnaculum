# Investment Portfolio Performance Testing

Examples of performance testing for Binnaculum's investment tracking functionality, focusing on mobile device constraints and financial calculation efficiency.

## Core Performance Tests

### Large Portfolio Performance Validation

```csharp
using Xunit;
using System.Diagnostics;
using Binnaculum.Tests.TestUtils.UI.DeviceTests;
using Binnaculum.Core.Snapshots;

namespace Binnaculum.Tests.Examples.PerformanceTests
{
    public class InvestmentPortfolioPerformanceTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        [Fact]
        public async Task PortfolioSnapshot_LargePortfolio_CompleteWithinMobileConstraints()
        {
            // Arrange - Large realistic portfolio (mobile stress test)
            var portfolio = InvestmentTestData.CreateLargePortfolio(
                brokerAccounts: 8,
                transactionsPerAccount: 500,  // 4,000 total transactions
                currencies: new[] { "USD", "EUR", "GBP" },
                timeSpan: TimeSpan.FromDays(1825) // 5 years of history
            );
            
            var calculator = new BrokerFinancialSnapshotManager();
            
            // Act - Measure calculation performance
            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
            
            var snapshot = await calculator.CalculateAsync(portfolio);
            
            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
            var memoryUsed = memoryAfter - memoryBefore;
            
            // Assert - Mobile device performance requirements
            Assert.True(stopwatch.ElapsedMilliseconds < 2000,
                $"Large portfolio calculation took {stopwatch.ElapsedMilliseconds}ms, " +
                $"should complete within 2000ms on mobile devices");
            
            Assert.True(memoryUsed < 100 * 1024 * 1024,
                $"Memory usage {memoryUsed / 1024 / 1024}MB exceeds 100MB limit for mobile");
            
            // Verify calculation accuracy despite performance optimizations
            snapshot.AssertCalculationAccuracy();
            snapshot.AssertCurrencyConsistency();
            
            Console.WriteLine($"Portfolio Performance: {stopwatch.ElapsedMilliseconds}ms, " +
                            $"Memory: {memoryUsed / 1024 / 1024}MB");
        }
        
        [Theory]
        [InlineData(100, 50)]    // Small investor
        [InlineData(1000, 200)]  // Active investor  
        [InlineData(5000, 500)]  // Large portfolio
        [InlineData(10000, 1000)] // Institutional scale
        public async Task PortfolioCalculation_VariousDatasetSizes_ScalesLinearly(
            int transactionCount, int maxTimeMs)
        {
            // Arrange - Variable sized portfolios
            var portfolio = InvestmentTestData.CreatePortfolioWithTransactions(transactionCount)
                .WithRealisticDiversification()
                .WithMultipleCurrencies()
                .Build();
            
            var calculator = new BrokerFinancialSnapshotManager();
            
            // Act - Performance measurement
            var stopwatch = Stopwatch.StartNew();
            var snapshot = await calculator.CalculateAsync(portfolio);
            stopwatch.Stop();
            
            // Assert - Linear scaling performance
            Assert.True(stopwatch.ElapsedMilliseconds < maxTimeMs,
                $"Portfolio with {transactionCount} transactions took " +
                $"{stopwatch.ElapsedMilliseconds}ms, expected < {maxTimeMs}ms");
            
            // Verify accuracy is maintained at scale
            snapshot.AssertFinancialAccuracy();
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
```

## UI Performance Testing

### Component Rendering Performance

```csharp
[Fact]
public async Task BrokerAccountTemplate_RapidDataUpdates_MaintainsUIResponsiveness()
{
    // Arrange - Component with real-time data simulation
    var template = new BrokerAccountTemplate();
    var baseAccount = new BrokerAccountBuilder()
        .WithBalance(125000.00m)
        .AsProfitableScenario()
        .Build();
    
    await template.LoadAccountDataAsync(baseAccount);
    
    // Act - Simulate rapid market updates (every 100ms for 10 seconds)
    var updateStopwatch = Stopwatch.StartNew();
    var uiResponsiveTimes = new List<long>();
    
    for (int i = 0; i < 100; i++) // 100 updates
    {
        var uiUpdateStart = Stopwatch.StartNew();
        
        // Simulate market price changes
        var updatedAccount = baseAccount with 
        { 
            Balance = baseAccount.Balance * (1 + (Random.Shared.NextSingle() - 0.5f) * 0.02m) // ±1% variation
        };
        
        await template.UpdateDataAsync(updatedAccount);
        
        // Measure UI update responsiveness
        uiUpdateStart.Stop();
        uiResponsiveTimes.Add(uiUpdateStart.ElapsedMilliseconds);
        
        // Small delay between updates (realistic market data frequency)
        await Task.Delay(100);
    }
    
    updateStopwatch.Stop();
    
    // Assert - UI responsiveness requirements
    var averageUIUpdateTime = uiResponsiveTimes.Average();
    var maxUIUpdateTime = uiResponsiveTimes.Max();
    
    Assert.True(averageUIUpdateTime < 16.67, // 60 FPS = 16.67ms per frame
        $"Average UI update {averageUIUpdateTime}ms exceeds 60 FPS requirement");
    
    Assert.True(maxUIUpdateTime < 33.33, // 30 FPS = 33.33ms (acceptable peak)
        $"Maximum UI update {maxUIUpdateTime}ms causes visible lag");
    
    // Verify UI remains accurate after rapid updates
    template.AssertDataAccuracy();
    template.AssertUIConsistency();
    
    Console.WriteLine($"UI Performance - Average: {averageUIUpdateTime:F2}ms, " +
                      $"Max: {maxUIUpdateTime}ms, Total: {updateStopwatch.ElapsedMilliseconds}ms");
}

[Fact]
public async Task InvestmentChart_LargeDataset_RendersWithinTimeConstraints()
{
    // Arrange - Chart with extensive historical data
    var historicalData = InvestmentTestData.CreateHistoricalPriceData(
        days: 1825, // 5 years
        dataPointsPerDay: 48 // Every 30 minutes during market hours
    );
    
    var chart = new InvestmentPerformanceChart();
    
    // Act - Measure chart rendering performance
    var stopwatch = Stopwatch.StartNew();
    await chart.LoadDataAsync(historicalData);
    stopwatch.Stop();
    
    // Assert - Chart rendering performance
    Assert.True(stopwatch.ElapsedMilliseconds < 1000,
        $"Chart rendering took {stopwatch.ElapsedMilliseconds}ms, " +
        $"should complete within 1000ms for good UX");
    
    // Verify chart accuracy and interactivity
    chart.AssertDataPointAccuracy(sampleSize: 100);
    chart.AssertInteractiveElementsResponsive();
}
```

## Memory Management Performance

### Observable Chain Performance and Memory

```csharp
[Fact]
public async Task InvestmentViewModel_MemoryManagement_NoLeaksUnderLoad()
{
    // Arrange - ViewModel with complex Observable chains
    var viewModel = new InvestmentPortfolioViewModel();
    var testData = InvestmentTestData.CreateDynamicPortfolio();
    
    // Set up realistic reactive chains
    var subscription1 = viewModel.WhenAnyValue(x => x.TotalBalance)
        .Throttle(TimeSpan.FromMilliseconds(100))
        .Subscribe(balance => UpdateBalanceDisplay(balance))
        .DisposeWith(_disposables);
    
    var subscription2 = viewModel.WhenAnyValue(x => x.ProfitLossPercentage)
        .Where(percentage => percentage != 0)
        .Subscribe(percentage => UpdateProfitLossUI(percentage))
        .DisposeWith(_disposables);
    
    // Act - Intensive usage simulation
    var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
    
    for (int cycle = 0; cycle < 50; cycle++) // 50 load/update cycles
    {
        await viewModel.LoadPortfolioAsync(testData);
        
        // Simulate 20 rapid updates per cycle (market volatility)
        for (int update = 0; update < 20; update++)
        {
            await viewModel.UpdateMarketDataAsync(
                GenerateMarketUpdate(volatility: 0.05m)); // 5% volatility
        }
        
        // Simulate navigation away and back (common user pattern)
        viewModel.OnNavigatedAway();
        await Task.Delay(10); // Brief pause
        viewModel.OnNavigatedTo();
    }
    
    // Force garbage collection to measure actual memory usage
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
    var memoryGrowth = memoryAfter - memoryBefore;
    
    // Assert - Memory management requirements
    Assert.True(memoryGrowth < 50 * 1024 * 1024, // 50MB growth limit
        $"Memory growth {memoryGrowth / 1024 / 1024}MB indicates memory leaks");
    
    // Verify Observable chains are properly disposed
    subscription1.AssertObservableMemoryLeak();
    subscription2.AssertObservableMemoryLeak();
    
    Console.WriteLine($"Memory Performance - Growth: {memoryGrowth / 1024 / 1024}MB " +
                      $"over {50 * 20} updates");
}
```

## Concurrent Performance Testing

### Multi-Account Concurrent Updates

```csharp
[Fact]
public async Task MultiAccountPortfolio_ConcurrentUpdates_MaintainsDataConsistency()
{
    // Arrange - Multiple broker accounts updating simultaneously
    var accounts = Enumerable.Range(0, 10)
        .Select(i => new BrokerAccountBuilder()
            .WithName($"Broker Account {i}")
            .WithRandomizedData()
            .Build())
        .ToList();
    
    var portfolioManager = new PortfolioManager();
    await portfolioManager.LoadAccountsAsync(accounts);
    
    // Act - Concurrent market data updates
    var updateTasks = accounts.Select(async account =>
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate 100 concurrent price updates per account
        for (int i = 0; i < 100; i++)
        {
            var marketUpdate = GenerateRandomMarketUpdate(account);
            await portfolioManager.UpdateAccountAsync(account.Id, marketUpdate);
            
            // Small random delay to simulate real market timing
            await Task.Delay(Random.Shared.Next(1, 10));
        }
        
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    });
    
    var completionTimes = await Task.WhenAll(updateTasks);
    
    // Assert - Concurrent performance requirements
    var maxCompletionTime = completionTimes.Max();
    var averageCompletionTime = completionTimes.Average();
    
    Assert.True(maxCompletionTime < 5000, // 5 second max for 100 updates
        $"Slowest account updates took {maxCompletionTime}ms");
    
    Assert.True(averageCompletionTime < 3000, // 3 second average
        $"Average update time {averageCompletionTime}ms too slow");
    
    // Verify data consistency after concurrent updates
    portfolioManager.AssertDataConsistency();
    portfolioManager.AssertNoDataCorruption();
    
    Console.WriteLine($"Concurrent Performance - Max: {maxCompletionTime}ms, " +
                      $"Average: {averageCompletionTime:F0}ms");
}
```

## Platform-Specific Performance Testing

### Mobile-Optimized Financial Calculations

```csharp
[Fact]
public async Task FinancialCalculations_MobileOptimized_PerformsEfficientlyOnLowEndDevices()
{
    // Arrange - Simulate low-end mobile device constraints
    var constrainedEnvironment = new MobileConstraintSimulator
    {
        MaxMemoryMB = 512,        // Low-end device memory limit
        CpuThrottling = 0.5,      // 50% CPU performance (thermal throttling)
        NetworkLatency = 200      // 200ms network delay
    };
    
    using (constrainedEnvironment.Apply())
    {
        var portfolio = InvestmentTestData.CreateMediumPortfolio(
            accounts: 5,
            transactionsPerAccount: 200
        );
        
        var calculator = new BrokerFinancialSnapshotManager();
        
        // Act - Performance under constraints
        var stopwatch = Stopwatch.StartNew();
        var snapshot = await calculator.CalculateAsync(portfolio);
        stopwatch.Stop();
        
        var memoryUsage = GC.GetTotalMemory(false);
        
        // Assert - Low-end device requirements
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Calculation took {stopwatch.ElapsedMilliseconds}ms on constrained device, " +
            $"should complete within 5000ms");
        
        Assert.True(memoryUsage < 200 * 1024 * 1024,
            $"Memory usage {memoryUsage / 1024 / 1024}MB exceeds low-end device limit");
        
        // Verify accuracy is maintained despite optimizations
        snapshot.AssertCalculationAccuracy();
    }
}
```

## Performance Benchmarking Utilities

### Performance Test Helpers

```csharp
public static class PerformanceTestHelpers
{
    public static async Task<PerformanceMetrics> MeasureAsync<T>(
        Func<Task<T>> operation,
        string operationName = "Operation")
    {
        // Warm up JIT and GC
        await operation();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // Measure actual performance
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = Stopwatch.StartNew();
        
        var result = await operation();
        
        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        
        return new PerformanceMetrics
        {
            OperationName = operationName,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = memoryAfter - memoryBefore,
            Result = result
        };
    }
    
    public static void AssertPerformanceRequirements(
        PerformanceMetrics metrics,
        long maxTimeMs,
        long maxMemoryBytes)
    {
        Assert.True(metrics.ElapsedMs <= maxTimeMs,
            $"{metrics.OperationName} took {metrics.ElapsedMs}ms, " +
            $"expected ≤ {maxTimeMs}ms");
        
        Assert.True(metrics.MemoryUsedBytes <= maxMemoryBytes,
            $"{metrics.OperationName} used {metrics.MemoryUsedBytes / 1024 / 1024}MB, " +
            $"expected ≤ {maxMemoryBytes / 1024 / 1024}MB");
    }
}

public class PerformanceMetrics
{
    public string OperationName { get; set; }
    public long ElapsedMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public object Result { get; set; }
}
```

## Performance Regression Testing

### Benchmark Comparison Tests

```csharp
[Fact]
public async Task PortfolioCalculation_PerformanceRegression_NoSlowerThanBaseline()
{
    // Arrange - Standard performance test portfolio
    var portfolio = InvestmentTestData.CreateStandardPerformanceTestPortfolio();
    var calculator = new BrokerFinancialSnapshotManager();
    
    // Act - Measure current performance
    var metrics = await PerformanceTestHelpers.MeasureAsync(
        () => calculator.CalculateAsync(portfolio),
        "Portfolio Calculation"
    );
    
    // Assert - Compare against established baselines
    var baseline = PerformanceBaselines.PortfolioCalculation;
    
    Assert.True(metrics.ElapsedMs <= baseline.MaxTimeMs * 1.1, // 10% tolerance
        $"Performance regression: {metrics.ElapsedMs}ms vs baseline {baseline.MaxTimeMs}ms");
    
    Assert.True(metrics.MemoryUsedBytes <= baseline.MaxMemoryBytes * 1.1,
        $"Memory regression: {metrics.MemoryUsedBytes / 1024 / 1024}MB vs " +
        $"baseline {baseline.MaxMemoryBytes / 1024 / 1024}MB");
    
    // Log performance data for trending analysis
    PerformanceLogger.LogMetrics("PortfolioCalculation", metrics);
}
```

This performance testing suite ensures that Binnaculum's investment tracking functionality meets mobile device performance requirements while maintaining financial calculation accuracy. The tests cover:

1. **Large Dataset Performance**: Validating performance with realistic large portfolios
2. **UI Responsiveness**: Ensuring smooth user experience during rapid updates
3. **Memory Management**: Preventing memory leaks in Observable chains
4. **Concurrent Updates**: Testing multi-account simultaneous updates
5. **Mobile Constraints**: Optimizing for low-end device performance
6. **Regression Prevention**: Baseline comparison for performance monitoring

All tests are designed with mobile device constraints in mind, ensuring the investment app performs well across the target device spectrum.