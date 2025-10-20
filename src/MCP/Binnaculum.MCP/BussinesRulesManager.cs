

[McpServerToolType]
public static class BusinessRulesManager
{
    #region TickerCurrencySnapshot Rules

    [McpServerTool]
    [Description("Get comprehensive calculation rules for TickerCurrencySnapshot entity. Includes all formulas, constraints, edge cases, and implementation guidance.")]
    public static async Task<string> GetTickerCurrencySnapshotRules()
    {
        return await LoadEmbeddedResource("ticker-currency-snapshot-rules.md");
    }

    #endregion

    #region Option Trade Import & Storage Rules

    [McpServerTool]
    [Description("Get comprehensive rules for Option Trade Import & Storage. Defines data model consistency between UI and CSV import, FIFO matching algorithm, and IsOpen flag management.")]
    public static async Task<string> GetOptionTradeImportStorageRules()
    {
        return await LoadEmbeddedResource("option-trade-import-rules.md");
    }

    #endregion

    #region Broker Data Format Rules

    [McpServerTool]
    [Description("Get comprehensive broker data format overview.")]
    public static async Task<string> GetBrokerFormatOverview()
    {
        return await LoadEmbeddedResource("broker-format-overview.md");
    }

    [McpServerTool]
    [Description("Get Tastytrade strike adjustment parsing guide.")]
    public static async Task<string> GetTastytradeStrikeAdjustmentGuide()
    {
        return await LoadEmbeddedResource("tastytrade-strike-adjustment-guide.md");
    }

    [McpServerTool]
    [Description("Get common broker patterns.")]
    public static async Task<string> GetCommonBrokerPatterns()
    {
        return await LoadEmbeddedResource("common-patterns.md");
    }

    [McpServerTool]
    [Description("Get broker data validation rules.")]
    public static async Task<string> GetBrokerDataValidationRules()
    {
        return await LoadEmbeddedResource("validation-rules.md");
    }

    #endregion

    private static async Task<string> LoadEmbeddedResource(string resourceName)
    {
        try
        {
            var assembly = typeof(BusinessRulesManager).Assembly;
            var resourcePath = $"Binnaculum.MCP.Resources.{resourceName.Replace("/", ".").Replace("-", "_")}";
            using var stream = assembly.GetManifestResourceStream(resourcePath) ?? assembly.GetManifestResourceStream(resourcePath.Replace("_", "-"));
            if (stream == null) return $"Resource '{resourceName}' not found";
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
