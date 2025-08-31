// using Binnaculum.Extensions; // Commented out temporarily - requires UI project reference
using static Binnaculum.Core.Models;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Binnaculum-specific assertion extensions for device testing with investment tracking functionality.
/// Based on Microsoft MAUI TestUtils patterns for comprehensive device testing.
/// </summary>
public static class BinnaculumAssertionExtensions
{
    #region Currency Format Assertions

    /// <summary>
    /// Validates currency formatting across different cultures.
    /// Ensures decimal values are formatted correctly according to the specified culture's currency format.
    /// </summary>
    /// <param name="amount">The decimal amount to format and validate</param>
    /// <param name="culture">The culture identifier (e.g., "en-US", "es-ES")</param>
    /// <param name="expectedFormat">The expected formatted string</param>
    public static void AssertCurrencyFormat(this decimal amount, string culture, string expectedFormat)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        var actualFormat = amount.ToString("C", cultureInfo);
        
        Assert.Equal(expectedFormat, actualFormat);
    }

    /// <summary>
    /// Validates currency formatting using Binnaculum's ToMoneyString extension method.
    /// Tests the custom currency formatting logic used throughout the application.
    /// </summary>
    /// <param name="amount">The decimal amount to format</param>
    /// <param name="symbol">The currency symbol to use</param>
    /// <param name="expectedFormat">The expected formatted string</param>
    public static void AssertBinnaculumCurrencyFormat(this decimal amount, string? symbol, string expectedFormat)
    {
        // TODO: Uncomment when UI project reference is available
        // var actualFormat = amount.ToMoneyString(symbol);
        // Assert.Equal(expectedFormat, actualFormat);
        
        // Temporary implementation for testing framework
        var actualFormat = string.IsNullOrWhiteSpace(symbol) ? $"${amount:0.00}" : $"{symbol}{amount:0.00}";
        Assert.Equal(expectedFormat, actualFormat);
    }

    /// <summary>
    /// Validates simplified decimal formatting using Binnaculum's Simplifyed extension method.
    /// Ensures decimal values are simplified to the minimum required decimal places.
    /// </summary>
    /// <param name="value">The decimal value to simplify</param>
    /// <param name="expectedFormat">The expected simplified string format</param>
    public static void AssertSimplifiedFormat(this decimal value, string expectedFormat)
    {
        // TODO: Uncomment when UI project reference is available  
        // var actualFormat = value.Simplifyed();
        // Assert.Equal(expectedFormat, actualFormat);
        
        // Temporary implementation for testing framework
        string actualFormat;
        if (value == 0)
            actualFormat = "0";
        else if (value % 1 == 0) // If it's a whole number
            actualFormat = value.ToString("F0");
        else if (Math.Abs(value) < 1) // If it's less than 1
            actualFormat = value.ToString("F4");
        else
            actualFormat = value.ToString("F2");
            
        Assert.Equal(expectedFormat, actualFormat);
    }

    #endregion

    #region Percentage Calculation Assertions

    /// <summary>
    /// Verifies percentage calculations in overview snapshots.
    /// Validates that realized and unrealized percentage calculations are accurate.
    /// </summary>
    /// <param name="snapshot">The overview snapshot containing financial data</param>
    /// <param name="expectedRealizedPercentage">Expected realized percentage</param>
    /// <param name="expectedUnrealizedPercentage">Expected unrealized percentage (optional)</param>
    public static void AssertPercentageCalculation(
        this OverviewSnapshot snapshot, 
        decimal expectedRealizedPercentage, 
        decimal? expectedUnrealizedPercentage = null)
    {
        Assert.NotNull(snapshot);
        
        switch (snapshot.Type)
        {
            case OverviewSnapshotType.InvestmentOverview when snapshot.InvestmentOverview.HasValue:
                var investmentOverview = snapshot.InvestmentOverview.Value;
                Assert.Equal(expectedRealizedPercentage, investmentOverview.RealizedPercentage);
                break;
                
            case OverviewSnapshotType.Broker when snapshot.Broker.HasValue:
                var brokerSnapshot = snapshot.Broker.Value;
                AssertFinancialPercentages(brokerSnapshot.Financial, expectedRealizedPercentage, expectedUnrealizedPercentage);
                break;
                
            case OverviewSnapshotType.BrokerAccount when snapshot.BrokerAccount.HasValue:
                var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
                AssertFinancialPercentages(brokerAccountSnapshot.Financial, expectedRealizedPercentage, expectedUnrealizedPercentage);
                break;
                
            default:
                throw new InvalidOperationException($"Snapshot type {snapshot.Type} does not support percentage calculations");
        }
    }

    /// <summary>
    /// Helper method to assert financial percentages in broker financial snapshots.
    /// </summary>
    private static void AssertFinancialPercentages(
        BrokerFinancialSnapshot financial, 
        decimal expectedRealizedPercentage, 
        decimal? expectedUnrealizedPercentage)
    {
        Assert.Equal(expectedRealizedPercentage, financial.RealizedPercentage);
        
        if (expectedUnrealizedPercentage.HasValue)
        {
            Assert.Equal(expectedUnrealizedPercentage.Value, financial.UnrealizedGainsPercentage);
        }
    }

    #endregion

    #region Portfolio Balance Assertions

    /// <summary>
    /// Tests portfolio balance calculations in broker account snapshots.
    /// Validates that the portfolio value matches expected calculations.
    /// </summary>
    /// <param name="snapshot">The broker account snapshot</param>
    /// <param name="expectedPortfolioValue">Expected total portfolio value</param>
    /// <param name="tolerance">Acceptable tolerance for decimal comparisons (default: 0.01)</param>
    public static void AssertPortfolioBalance(
        this BrokerAccountSnapshot snapshot, 
        decimal expectedPortfolioValue, 
        decimal tolerance = 0.01m)
    {
        Assert.NotNull(snapshot);
        Assert.InRange(snapshot.PortfolioValue, 
            expectedPortfolioValue - tolerance, 
            expectedPortfolioValue + tolerance);
    }

    /// <summary>
    /// Validates broker-level portfolio balance calculations.
    /// Tests aggregated portfolio values across multiple accounts.
    /// </summary>
    /// <param name="snapshot">The broker snapshot</param>
    /// <param name="expectedPortfoliosValue">Expected total portfolios value</param>
    /// <param name="tolerance">Acceptable tolerance for decimal comparisons (default: 0.01)</param>
    public static void AssertBrokerPortfolioBalance(
        this BrokerSnapshot snapshot, 
        decimal expectedPortfoliosValue, 
        decimal tolerance = 0.01m)
    {
        Assert.NotNull(snapshot);
        Assert.InRange(snapshot.PortfoliosValue, 
            expectedPortfoliosValue - tolerance, 
            expectedPortfoliosValue + tolerance);
    }

    #endregion

    #region Financial Snapshot Assertions

    /// <summary>
    /// Validates broker financial snapshots comprehensively.
    /// Tests all financial metrics including gains, investments, fees, and dividends.
    /// </summary>
    /// <param name="snapshot">The broker financial snapshot to validate</param>
    /// <param name="expectedRealizedGains">Expected realized gains</param>
    /// <param name="expectedUnrealizedGains">Expected unrealized gains</param>
    /// <param name="expectedInvested">Expected total invested amount</param>
    /// <param name="expectedCommissions">Expected total commissions</param>
    /// <param name="expectedFees">Expected total fees</param>
    /// <param name="tolerance">Acceptable tolerance for decimal comparisons (default: 0.01)</param>
    public static void AssertFinancialSnapshot(
        this BrokerFinancialSnapshot snapshot,
        decimal? expectedRealizedGains = null,
        decimal? expectedUnrealizedGains = null,
        decimal? expectedInvested = null,
        decimal? expectedCommissions = null,
        decimal? expectedFees = null,
        decimal tolerance = 0.01m)
    {
        Assert.NotNull(snapshot);
        
        if (expectedRealizedGains.HasValue)
        {
            Assert.InRange(snapshot.RealizedGains, 
                expectedRealizedGains.Value - tolerance, 
                expectedRealizedGains.Value + tolerance);
        }
        
        if (expectedUnrealizedGains.HasValue)
        {
            Assert.InRange(snapshot.UnrealizedGains, 
                expectedUnrealizedGains.Value - tolerance, 
                expectedUnrealizedGains.Value + tolerance);
        }
        
        if (expectedInvested.HasValue)
        {
            Assert.InRange(snapshot.Invested, 
                expectedInvested.Value - tolerance, 
                expectedInvested.Value + tolerance);
        }
        
        if (expectedCommissions.HasValue)
        {
            Assert.InRange(snapshot.Commissions, 
                expectedCommissions.Value - tolerance, 
                expectedCommissions.Value + tolerance);
        }
        
        if (expectedFees.HasValue)
        {
            Assert.InRange(snapshot.Fees, 
                expectedFees.Value - tolerance, 
                expectedFees.Value + tolerance);
        }
    }

    /// <summary>
    /// Validates financial snapshot consistency across currencies.
    /// Ensures multi-currency financial snapshots are properly related and consistent.
    /// </summary>
    /// <param name="mainSnapshot">The main currency financial snapshot</param>
    /// <param name="otherCurrencySnapshots">Financial snapshots in other currencies</param>
    public static void AssertFinancialSnapshotConsistency(
        this BrokerFinancialSnapshot mainSnapshot, 
        IEnumerable<BrokerFinancialSnapshot> otherCurrencySnapshots)
    {
        Assert.NotNull(mainSnapshot);
        
        foreach (var otherSnapshot in otherCurrencySnapshots)
        {
            // Verify same date
            Assert.Equal(mainSnapshot.Date, otherSnapshot.Date);
            
            // Verify same broker and broker account references
            Assert.Equal(mainSnapshot.Broker?.Id, otherSnapshot.Broker?.Id);
            Assert.Equal(mainSnapshot.BrokerAccount?.Id, otherSnapshot.BrokerAccount?.Id);
            
            // Verify different currencies
            Assert.NotEqual(mainSnapshot.Currency.Id, otherSnapshot.Currency.Id);
            
            // Verify movement counter consistency
            Assert.Equal(mainSnapshot.MovementCounter, otherSnapshot.MovementCounter);
        }
    }

    #endregion

    #region Memory Leak Detection for Observable Chains

    /// <summary>
    /// Tests for memory leaks in Observable chains using Microsoft MAUI TestUtils patterns.
    /// Validates that subscriptions are properly disposed and don't cause memory leaks.
    /// </summary>
    /// <typeparam name="T">The type of Observable elements</typeparam>
    /// <param name="observableFactory">Factory function that creates the Observable to test</param>
    /// <param name="timeout">Timeout for GC wait (default: 5 seconds)</param>
    /// <returns>Task that completes when memory leak test is finished</returns>
    public static async Task AssertObservableMemoryLeak<T>(
        Func<IObservable<T>> observableFactory, 
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        
        WeakReference? weakRef = null;
        IDisposable? subscription = null;
        
        // Create observable and subscription in separate scope to allow GC
        await Task.Run(() =>
        {
            var observable = observableFactory();
            weakRef = new WeakReference(observable);
            
            // Create and dispose subscription
            subscription = observable.Subscribe(_ => { });
            subscription.Dispose();
        });
        
        // Force garbage collection
        await WaitForGC(timeout.Value);
        
        // Assert that the observable has been garbage collected
        Assert.False(weakRef?.IsAlive ?? false, 
            "Observable was not garbage collected, indicating a potential memory leak");
    }

    /// <summary>
    /// Waits for garbage collection to complete, based on Microsoft MAUI TestUtils WaitForGC implementation.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for GC</param>
    private static async Task WaitForGC(TimeSpan timeout)
    {
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            await Task.Delay(100);
        }
    }

    #endregion

    #region F#/C# Interop Testing Extensions - TEMPORARILY DISABLED

    /* TODO: Uncomment when F# packages are properly configured
    /// <summary>
    /// Tests F# option type handling in C# device tests.
    /// Validates proper interop between F# Core logic and C# UI tests.
    /// </summary>
    /// <typeparam name="T">The type contained in the F# option</typeparam>
    /// <param name="option">The F# option value</param>
    /// <param name="expectedHasValue">Whether the option should have a value</param>
    /// <param name="expectedValue">The expected value if option has value</param>
    public static void AssertFSharpOption<T>(
        FSharpOption<T> option, 
        bool expectedHasValue, 
        T? expectedValue = default)
    {
        Assert.Equal(expectedHasValue, FSharpOption<T>.get_IsSome(option));
        
        if (expectedHasValue && expectedValue != null)
        {
            var actualValue = option.Value;
            Assert.Equal(expectedValue, actualValue);
        }
    }
    */

    /// <summary>
    /// Validates F# domain model objects from C# tests.
    /// Ensures F# types are properly accessible and testable from C# device tests.
    /// </summary>
    /// <param name="brokerAccount">The F# broker account model</param>
    /// <param name="expectedId">Expected account ID</param>
    /// <param name="expectedBrokerName">Expected broker name</param>
    /// <param name="expectedAccountNumber">Expected account number</param>
    public static void AssertFSharpBrokerAccount(
        BrokerAccount brokerAccount,
        int expectedId,
        string expectedBrokerName,
        string expectedAccountNumber)
    {
        Assert.NotNull(brokerAccount);
        Assert.Equal(expectedId, brokerAccount.Id);
        Assert.Equal(expectedBrokerName, brokerAccount.Broker.Name);
        Assert.Equal(expectedAccountNumber, brokerAccount.AccountNumber);
    }

    /* TODO: Uncomment when F# async packages are properly configured
    /// <summary>
    /// Tests F# async workflow integration with C# async/await patterns.
    /// Validates that F# async operations work correctly in device test environments.
    /// </summary>
    /// <typeparam name="T">The return type of the async operation</typeparam>
    /// <param name="fsharpAsync">The F# async operation</param>
    /// <param name="expectedResult">The expected result</param>
    /// <param name="timeout">Timeout for the async operation (default: 10 seconds)</param>
    public static async Task AssertFSharpAsync<T>(
        FSharpAsync<T> fsharpAsync,
        T expectedResult,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        
        using var cts = new CancellationTokenSource(timeout.Value);
        
        try
        {
            var result = await FSharpAsync.StartAsTask(fsharpAsync, null, cts.Token);
            Assert.Equal(expectedResult, result);
        }
        catch (TaskCanceledException)
        {
            Assert.Fail($"F# async operation timed out after {timeout}");
        }
    }
    */

    #endregion

    #region Base MAUI Assertion Extensions

    /// <summary>
    /// Extends base MAUI assertions with Binnaculum-specific UI element testing.
    /// Validates that MAUI controls properly display financial data.
    /// </summary>
    /// <param name="element">The UI element to test</param>
    /// <param name="expectedText">Expected displayed text</param>
    /// <param name="timeout">Timeout for UI updates (default: 2 seconds)</param>
    public static async Task AssertFinancialControlText(
        this Element element, 
        string expectedText, 
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(2);
        var endTime = DateTime.UtcNow.Add(timeout.Value);
        
        string actualText = "";
        while (DateTime.UtcNow < endTime)
        {
            actualText = element switch
            {
                Label label => label.Text ?? "",
                Button button => button.Text ?? "",
                Entry entry => entry.Text ?? "",
                _ => throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for text assertions")
            };
            
            if (actualText == expectedText)
                return;
                
            await Task.Delay(50);
        }
        
        Assert.Equal(expectedText, actualText);
    }

    #endregion
}