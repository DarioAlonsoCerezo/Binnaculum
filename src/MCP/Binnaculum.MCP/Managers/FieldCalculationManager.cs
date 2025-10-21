using System.ComponentModel.DataAnnotations;

namespace Binnaculum.MCP.Managers;

/// <summary>
/// Exposes documentation for financial field calculations and formulas used in the investment tracking system.
/// Covers calculation rules, constraints, precision requirements, and implementation guidance
/// for all computed fields in snapshots, positions, and portfolio metrics.
/// </summary>
[McpServerToolType]
public static class FieldCalculationManager
{
    #region Ticker Currency Snapshot Calculations

    [McpServerTool]
    [Description("Get comprehensive calculation rules for TickerCurrencySnapshot entity. Includes all formulas, constraints, edge cases, and implementation guidance.")]
    public static async Task<string> GetTickerCurrencySnapshotCalculations()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("calculations/ticker-currency-snapshot-rules.md");
    }

    #endregion
}
