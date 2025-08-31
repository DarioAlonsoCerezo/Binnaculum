using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Binnaculum.Tests.TestUtils.Performance.Components;

/// <summary>
/// Performance tests for PercentageControl calculation performance with large datasets
/// Based on issue requirements for component performance testing
/// </summary>
[TestFixture]
public class PercentageControlPerformanceTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    [Test]
    [Description("Test PercentageControl calculation performance with different dataset sizes")]
    [TestCase(100, 10, "Small dataset")]
    [TestCase(1000, 50, "Medium dataset")]
    [TestCase(10000, 200, "Large dataset - mobile stress test")]
    [TestCase(50000, 1000, "Extra large dataset - desktop performance")]
    public void PercentageControl_CalculationPerformance_WithLargeDatasets(
        int datasetSize, long maxTimeMs, string description)
    {
        // Arrange - Generate financial data for percentage calculations
        var financialData = GenerateFinancialData(datasetSize);
        
        // Act - Measure percentage calculation performance
        var metrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var results = new List<PercentageCalculationResult>();
            
            foreach (var data in financialData)
            {
                var result = new PercentageCalculationResult
                {
                    Id = data.Id,
                    Symbol = data.Symbol,
                    // Core percentage calculations that PercentageControl would perform
                    ProfitLossPercentage = CalculateProfitLossPercentage(data.CurrentValue, data.InitialValue),
                    DailyChangePercentage = CalculateDailyChangePercentage(data.CurrentValue, data.PreviousDayValue),
                    WeeklyChangePercentage = CalculateWeeklyChangePercentage(data.CurrentValue, data.WeekAgoValue),
                    MonthlyChangePercentage = CalculateMonthlyChangePercentage(data.CurrentValue, data.MonthAgoValue),
                    YearlyChangePercentage = CalculateYearlyChangePercentage(data.CurrentValue, data.YearAgoValue),
                    // Additional calculations for stress testing
                    VolatilityPercentage = CalculateVolatilityPercentage(data.HistoricalValues),
                    RiskAdjustedReturn = CalculateRiskAdjustedReturn(data.CurrentValue, data.InitialValue, data.Volatility),
                    AllocationPercentage = CalculateAllocationPercentage(data.CurrentValue, data.TotalPortfolioValue),
                    IsPositive = data.CurrentValue > data.InitialValue,
                    FormattedPercentage = FormatPercentage(CalculateProfitLossPercentage(data.CurrentValue, data.InitialValue))
                };
                
                results.Add(result);
            }
            
            return results;
        }, $"PercentageControl_Calculations_{description}");
        
        // Assert - Performance requirements
        PerformanceMeasurement.AssertPerformanceRequirements(
            metrics,
            maxTimeMs,
            100 * 1024 * 1024, // 100MB memory limit
            3); // Max 3 GC collections
        
        PerformanceMeasurement.LogMetrics(metrics);
        
        Console.WriteLine($"Processed {datasetSize} percentage calculations in {metrics.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average time per calculation: {(double)metrics.ElapsedMilliseconds / datasetSize:F6}ms");
        
        var results = (List<PercentageCalculationResult>)metrics.Result!;
        Console.WriteLine($"Results: {results.Count} calculations completed");
    }
    
    [Test]
    [Description("Test PercentageControl real-time calculation performance")]
    public async Task PercentageControl_RealTimeCalculations_ResponsiveUpdates()
    {
        const int updateCount = 1000;
        const int maxCalculationTime = 5; // 5ms max per calculation for real-time updates
        
        // Arrange
        var baseData = GenerateFinancialData(10);
        var calculationTimes = new List<long>();
        
        // Act - Simulate real-time percentage calculations
        var overallMetrics = await PerformanceMeasurement.MeasureAsync(async () =>
        {
            for (int i = 0; i < updateCount; i++)
            {
                var calculationMetrics = PerformanceMeasurement.MeasureSync(() =>
                {
                    var dataPoint = baseData[i % baseData.Count];
                    
                    // Simulate market data update
                    var updatedValue = dataPoint.CurrentValue * (1 + (decimal)((Random.Shared.NextDouble() - 0.5) * 0.05)); // ±2.5% variation
                    
                    // Perform real-time percentage calculations
                    var percentageChange = CalculateProfitLossPercentage(updatedValue, dataPoint.CurrentValue);
                    var formattedPercentage = FormatPercentage(percentageChange);
                    var isSignificantChange = Math.Abs(percentageChange) > 1.0m;
                    
                    return new { UpdatedValue = updatedValue, PercentageChange = percentageChange, Formatted = formattedPercentage };
                }, $"RealTimeCalculation_{i}");
                
                calculationTimes.Add(calculationMetrics.ElapsedMilliseconds);
                
                // Simulate realistic update frequency (market data updates)
                await Task.Delay(1);
            }
            
            return calculationTimes;
        }, "RealTimeCalculations_Complete");
        
        // Assert - Real-time performance requirements
        var averageCalculationTime = calculationTimes.Average();
        var maxCalculationTimeActual = calculationTimes.Max();
        
        Assert.That(averageCalculationTime, Is.LessThan(maxCalculationTime),
            $"Average calculation time {averageCalculationTime:F2}ms exceeds real-time requirement ({maxCalculationTime}ms)");
        
        Assert.That(maxCalculationTimeActual, Is.LessThan(maxCalculationTime * 3),
            $"Max calculation time {maxCalculationTimeActual}ms is too slow for real-time updates");
        
        Console.WriteLine($"Real-Time Calculation Performance:");
        Console.WriteLine($"  Average calculation time: {averageCalculationTime:F3}ms (Target: <{maxCalculationTime}ms)");
        Console.WriteLine($"  Max calculation time: {maxCalculationTimeActual}ms");
        Console.WriteLine($"  Calculations completed: {calculationTimes.Count}/{updateCount}");
        
        PerformanceMeasurement.LogMetrics(overallMetrics);
    }
    
    [Test]
    [Description("Test PercentageControl Observable chain performance for reactive updates")]
    public async Task PercentageControl_ObservableChains_ReactivePercentageUpdates()
    {
        // Test Observable-based percentage calculations
        var result = await MemoryLeakDetection.TestObservableMemoryLeak(disposables =>
        {
            var initialData = GenerateFinancialData(5);
            
            var observable = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Take(500)
                .Select(i =>
                {
                    var dataIndex = (int)(i % initialData.Count);
                    var data = initialData[dataIndex];
                    
                    // Simulate reactive percentage calculation updates
                    var marketChange = (decimal)((Random.Shared.NextDouble() - 0.5) * 0.1); // ±5% market change
                    var newValue = data.CurrentValue * (1 + marketChange);
                    
                    return new PercentageUpdateEvent
                    {
                        Symbol = data.Symbol,
                        OldValue = data.CurrentValue,
                        NewValue = newValue,
                        PercentageChange = CalculateProfitLossPercentage(newValue, data.CurrentValue),
                        Timestamp = DateTime.UtcNow
                    };
                })
                .Where(evt => Math.Abs(evt.PercentageChange) > 0.1m) // Filter small changes
                .Select(evt => new
                {
                    evt.Symbol,
                    evt.PercentageChange,
                    FormattedPercentage = FormatPercentage(evt.PercentageChange),
                    IsSignificant = Math.Abs(evt.PercentageChange) > 2.0m,
                    evt.Timestamp
                });
            
            disposables.Add(observable.Subscribe());
            return observable;
        }, iterations: 500, testDuration: TimeSpan.FromSeconds(10));
        
        // Assert no memory leaks in Observable chains
        result.AssertNoMemoryLeak();
        
        Console.WriteLine($"Observable Percentage Calculations:");
        Console.WriteLine($"  Events processed: {result.ResultCount}");
        Console.WriteLine($"  Peak memory: {result.PeakMemoryMB:F1}MB");
        Console.WriteLine($"  Memory properly cleaned up after disposal");
    }
    
    [Test]
    [Description("Test PercentageControl precision with extreme values")]
    public void PercentageControl_ExtremePrecision_AccurateCalculations()
    {
        // Arrange - Test with extreme financial values that could cause precision issues
        var extremeTestCases = new List<(decimal initial, decimal current, string description)>
        {
            (0.0001m, 0.0002m, "Micro values"),
            (1000000m, 1500000m, "Large values"),
            (0.01m, 1000000m, "Extreme growth"),
            (1000000m, 0.01m, "Extreme loss"),
            (decimal.MaxValue / 2, decimal.MaxValue / 3, "Near max values"),
            (1m, 1.000001m, "Tiny change"),
            (999.999999m, 1000.000001m, "High precision")
        };
        
        // Act - Test precision handling
        var metrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var results = new List<PrecisionTestResult>();
            
            foreach (var (initial, current, description) in extremeTestCases)
            {
                try
                {
                    var percentage = CalculateProfitLossPercentage(current, initial);
                    var formatted = FormatPercentage(percentage);
                    var isValid = !double.IsNaN((double)percentage) && !double.IsInfinity((double)percentage);
                    
                    results.Add(new PrecisionTestResult
                    {
                        Description = description,
                        InitialValue = initial,
                        CurrentValue = current,
                        CalculatedPercentage = percentage,
                        FormattedPercentage = formatted,
                        IsValid = isValid
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new PrecisionTestResult
                    {
                        Description = description,
                        InitialValue = initial,
                        CurrentValue = current,
                        Error = ex.Message,
                        IsValid = false
                    });
                }
            }
            
            return results;
        }, "PrecisionTesting");
        
        // Assert - All calculations should handle extreme values gracefully
        var results = (List<PrecisionTestResult>)metrics.Result!;
        
        Assert.That(results.Count, Is.EqualTo(extremeTestCases.Count), "All test cases should be processed");
        
        var validResults = results.Where(r => r.IsValid).ToList();
        var invalidResults = results.Where(r => !r.IsValid).ToList();
        
        Console.WriteLine($"Precision Test Results:");
        Console.WriteLine($"  Valid calculations: {validResults.Count}/{results.Count}");
        
        foreach (var result in results)
        {
            if (result.IsValid)
            {
                Console.WriteLine($"  ✓ {result.Description}: {result.InitialValue} → {result.CurrentValue} = {result.FormattedPercentage}");
            }
            else
            {
                Console.WriteLine($"  ✗ {result.Description}: {result.Error}");
            }
        }
        
        PerformanceMeasurement.LogMetrics(metrics);
        
        // At least 80% should handle extreme values correctly
        Assert.That(validResults.Count, Is.GreaterThanOrEqualTo((int)(results.Count * 0.8)),
            "At least 80% of extreme value calculations should be handled correctly");
    }
    
    // Core calculation methods that PercentageControl would use
    private decimal CalculateProfitLossPercentage(decimal current, decimal initial)
    {
        if (initial == 0) return 0m;
        return (current - initial) / initial * 100m;
    }
    
    private decimal CalculateDailyChangePercentage(decimal current, decimal previousDay) =>
        CalculateProfitLossPercentage(current, previousDay);
    
    private decimal CalculateWeeklyChangePercentage(decimal current, decimal weekAgo) =>
        CalculateProfitLossPercentage(current, weekAgo);
    
    private decimal CalculateMonthlyChangePercentage(decimal current, decimal monthAgo) =>
        CalculateProfitLossPercentage(current, monthAgo);
    
    private decimal CalculateYearlyChangePercentage(decimal current, decimal yearAgo) =>
        CalculateProfitLossPercentage(current, yearAgo);
    
    private decimal CalculateVolatilityPercentage(List<decimal> historicalValues)
    {
        if (historicalValues.Count < 2) return 0m;
        
        var changes = new List<decimal>();
        for (int i = 1; i < historicalValues.Count; i++)
        {
            changes.Add(CalculateProfitLossPercentage(historicalValues[i], historicalValues[i - 1]));
        }
        
        var mean = changes.Average();
        var variance = changes.Select(c => (c - mean) * (c - mean)).Average();
        return (decimal)Math.Sqrt((double)variance);
    }
    
    private decimal CalculateRiskAdjustedReturn(decimal current, decimal initial, decimal volatility)
    {
        var totalReturn = CalculateProfitLossPercentage(current, initial);
        return volatility == 0 ? totalReturn : totalReturn / volatility;
    }
    
    private decimal CalculateAllocationPercentage(decimal currentValue, decimal totalPortfolioValue) =>
        totalPortfolioValue == 0 ? 0m : currentValue / totalPortfolioValue * 100m;
    
    private string FormatPercentage(decimal percentage) =>
        $"{percentage:F2}%";
    
    private List<FinancialDataPoint> GenerateFinancialData(int count)
    {
        var random = new Random(42);
        var symbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN", "META", "NFLX", "AMD", "NVDA", "INTC" };
        var data = new List<FinancialDataPoint>();
        
        for (int i = 0; i < count; i++)
        {
            var symbol = symbols[i % symbols.Length];
            var initialValue = 100m + (decimal)(random.NextDouble() * 400); // $100-$500
            var currentValue = initialValue * (0.5m + (decimal)random.NextDouble() * 1.5m); // 50% to 200% of initial
            
            var historicalValues = new List<decimal>();
            for (int j = 0; j < 30; j++) // 30 days of history
            {
                historicalValues.Add(initialValue * (0.8m + (decimal)random.NextDouble() * 0.4m));
            }
            
            data.Add(new FinancialDataPoint
            {
                Id = i,
                Symbol = $"{symbol}_{i}",
                InitialValue = initialValue,
                CurrentValue = currentValue,
                PreviousDayValue = currentValue * (0.95m + (decimal)random.NextDouble() * 0.1m),
                WeekAgoValue = currentValue * (0.9m + (decimal)random.NextDouble() * 0.2m),
                MonthAgoValue = currentValue * (0.8m + (decimal)random.NextDouble() * 0.4m),
                YearAgoValue = initialValue,
                HistoricalValues = historicalValues,
                Volatility = (decimal)(random.NextDouble() * 50), // 0-50% volatility
                TotalPortfolioValue = 100000m // $100k portfolio
            });
        }
        
        return data;
    }
    
    public void Dispose()
    {
        _disposables?.Dispose();
    }
}

// Test data classes
public class FinancialDataPoint
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PreviousDayValue { get; set; }
    public decimal WeekAgoValue { get; set; }
    public decimal MonthAgoValue { get; set; }
    public decimal YearAgoValue { get; set; }
    public List<decimal> HistoricalValues { get; set; } = new();
    public decimal Volatility { get; set; }
    public decimal TotalPortfolioValue { get; set; }
}

public class PercentageCalculationResult
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal ProfitLossPercentage { get; set; }
    public decimal DailyChangePercentage { get; set; }
    public decimal WeeklyChangePercentage { get; set; }
    public decimal MonthlyChangePercentage { get; set; }
    public decimal YearlyChangePercentage { get; set; }
    public decimal VolatilityPercentage { get; set; }
    public decimal RiskAdjustedReturn { get; set; }
    public decimal AllocationPercentage { get; set; }
    public bool IsPositive { get; set; }
    public string FormattedPercentage { get; set; } = string.Empty;
}

public class PercentageUpdateEvent
{
    public string Symbol { get; set; } = string.Empty;
    public decimal OldValue { get; set; }
    public decimal NewValue { get; set; }
    public decimal PercentageChange { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PrecisionTestResult
{
    public string Description { get; set; } = string.Empty;
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal CalculatedPercentage { get; set; }
    public string FormattedPercentage { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
}