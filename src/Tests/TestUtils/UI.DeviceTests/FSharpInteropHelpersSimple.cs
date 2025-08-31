using Microsoft.FSharp.Core;
using static Binnaculum.Core.Models;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Simplified F# interop testing helpers for basic C#/F# integration.
/// Focuses on the most commonly used patterns without complex async workflows.
/// </summary>
public static class FSharpInteropHelpersSimple
{
    #region F# Option Type Helpers

    /// <summary>
    /// Creates an F# Some option from a C# value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="value">Value to wrap in Some</param>
    /// <returns>F# Some option</returns>
    public static FSharpOption<T> CreateSome<T>(T value)
    {
        return FSharpOption<T>.Some(value);
    }

    /// <summary>
    /// Creates an F# None option.
    /// </summary>
    /// <typeparam name="T">Type of the option</typeparam>
    /// <returns>F# None option</returns>
    public static FSharpOption<T> CreateNone<T>()
    {
        return FSharpOption<T>.None;
    }

    /// <summary>
    /// Checks if an F# option has a value.
    /// </summary>
    /// <typeparam name="T">Type of the option</typeparam>
    /// <param name="option">F# option to check</param>
    /// <returns>True if option has value (IsSome)</returns>
    public static bool HasValue<T>(FSharpOption<T> option)
    {
        return FSharpOption<T>.get_IsSome(option);
    }

    /// <summary>
    /// Gets the value from an F# option, or throws if None.
    /// </summary>
    /// <typeparam name="T">Type of the option</typeparam>
    /// <param name="option">F# option to unwrap</param>
    /// <returns>The unwrapped value</returns>
    public static T GetValue<T>(FSharpOption<T> option)
    {
        if (!FSharpOption<T>.get_IsSome(option))
            throw new InvalidOperationException("Option is None, cannot get value");
        return option.Value;
    }

    /// <summary>
    /// Gets the value from an F# option, or returns a default value if None.
    /// </summary>
    /// <typeparam name="T">Type of the option</typeparam>
    /// <param name="option">F# option to unwrap</param>
    /// <param name="defaultValue">Default value to return if None</param>
    /// <returns>The unwrapped value or default</returns>
    public static T GetValueOrDefault<T>(FSharpOption<T> option, T defaultValue)
    {
        return FSharpOption<T>.get_IsSome(option) ? option.Value : defaultValue;
    }

    #endregion

    #region Core Model Validation Helpers

    /// <summary>
    /// Validates that a Broker instance has required properties set.
    /// </summary>
    /// <param name="broker">Broker instance to validate</param>
    public static void ValidateBroker(Broker broker)
    {
        Assert.NotNull(broker);
        Assert.True(broker.Id > 0, "Broker ID must be positive");
        Assert.False(string.IsNullOrWhiteSpace(broker.Name), "Broker name cannot be empty");
        Assert.False(string.IsNullOrWhiteSpace(broker.Image), "Broker image cannot be empty");
        Assert.False(string.IsNullOrWhiteSpace(broker.SupportedBroker), "Supported broker cannot be empty");
    }

    /// <summary>
    /// Validates that a BrokerAccount instance has required properties set.
    /// </summary>
    /// <param name="brokerAccount">BrokerAccount instance to validate</param>
    public static void ValidateBrokerAccount(BrokerAccount brokerAccount)
    {
        Assert.NotNull(brokerAccount);
        Assert.True(brokerAccount.Id > 0, "BrokerAccount ID must be positive");
        Assert.NotNull(brokerAccount.Broker);
        ValidateBroker(brokerAccount.Broker);
        Assert.False(string.IsNullOrWhiteSpace(brokerAccount.AccountNumber), "Account number cannot be empty");
    }

    /// <summary>
    /// Validates that a Currency instance has required properties set.
    /// </summary>
    /// <param name="currency">Currency instance to validate</param>
    public static void ValidateCurrency(Currency currency)
    {
        Assert.NotNull(currency);
        Assert.True(currency.Id > 0, "Currency ID must be positive");
        Assert.False(string.IsNullOrWhiteSpace(currency.Title), "Currency title cannot be empty");
        Assert.False(string.IsNullOrWhiteSpace(currency.Code), "Currency code cannot be empty");
        Assert.False(string.IsNullOrWhiteSpace(currency.Symbol), "Currency symbol cannot be empty");
        
        // Validate code format (typically 3 characters)
        Assert.True(currency.Code.Length == 3, "Currency code should be 3 characters");
        Assert.True(currency.Code.ToUpper() == currency.Code, "Currency code should be uppercase");
    }

    /// <summary>
    /// Validates that a BrokerFinancialSnapshot has realistic financial data.
    /// </summary>
    /// <param name="snapshot">Financial snapshot to validate</param>
    public static void ValidateFinancialSnapshot(BrokerFinancialSnapshot snapshot)
    {
        Assert.NotNull(snapshot);
        Assert.True(snapshot.Id > 0, "Snapshot ID must be positive");
        
        // Validate amounts are reasonable
        Assert.True(snapshot.Invested >= 0, "Invested amount cannot be negative");
        Assert.True(snapshot.Deposited >= 0, "Deposited amount cannot be negative");
        Assert.True(snapshot.DividendsReceived >= 0, "Dividends received cannot be negative");
        Assert.True(snapshot.Commissions >= 0, "Commissions cannot be negative");
        Assert.True(snapshot.Fees >= 0, "Fees cannot be negative");
        Assert.True(snapshot.MovementCounter >= 0, "Movement counter cannot be negative");
        
        // Validate percentage ranges (allowing for extreme scenarios)
        Assert.True(snapshot.RealizedPercentage >= -100, "Realized percentage cannot be less than -100%");
        Assert.True(snapshot.RealizedPercentage <= 1000, "Realized percentage seems unrealistically high (>1000%)");
        
        if (snapshot.UnrealizedGainsPercentage != 0) // In F# models, this is a decimal, not nullable
        {
            Assert.True(snapshot.UnrealizedGainsPercentage >= -100, "Unrealized percentage cannot be less than -100%");
            Assert.True(snapshot.UnrealizedGainsPercentage <= 1000, "Unrealized percentage seems unrealistically high (>1000%)");
        }
    }

    #endregion

    #region Test Data Consistency Helpers

    /// <summary>
    /// Validates that two snapshots from the same broker account are consistent.
    /// </summary>
    /// <param name="snapshot1">First snapshot</param>
    /// <param name="snapshot2">Second snapshot</param>
    public static void ValidateSnapshotConsistency(BrokerFinancialSnapshot snapshot1, BrokerFinancialSnapshot snapshot2)
    {
        Assert.NotNull(snapshot1);
        Assert.NotNull(snapshot2);
        
        // Verify they reference the same broker account (if available)
        if (HasValue(snapshot1.BrokerAccount) && HasValue(snapshot2.BrokerAccount))
        {
            Assert.Equal(snapshot1.BrokerAccount.Value.Id, snapshot2.BrokerAccount.Value.Id);
        }
        
        if (HasValue(snapshot1.Broker) && HasValue(snapshot2.Broker))
        {
            Assert.Equal(snapshot1.Broker.Value.Id, snapshot2.Broker.Value.Id);
        }
    }

    /// <summary>
    /// Creates a test assertion message with F# model context.
    /// </summary>
    /// <param name="model">F# model instance</param>
    /// <param name="assertionDescription">Description of what is being asserted</param>
    /// <returns>Formatted assertion message</returns>
    public static string CreateAssertionMessage<T>(T model, string assertionDescription)
    {
        var modelType = typeof(T).Name;
        var modelId = GetModelId(model);
        return $"{assertionDescription} [Model: {modelType}, ID: {modelId}]";
    }

    /// <summary>
    /// Attempts to get an ID from common F# models that have Id properties.
    /// </summary>
    /// <param name="model">Model instance</param>
    /// <returns>ID if available, or "N/A"</returns>
    private static string GetModelId<T>(T model)
    {
        if (model == null) return "null";
        
        var type = typeof(T);
        var idProperty = type.GetProperty("Id");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(model);
            return id?.ToString() ?? "null";
        }
        
        return "N/A";
    }

    #endregion
}