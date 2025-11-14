namespace Binnaculum.MCP.Managers;

/// <summary>
/// Exposes documentation for business rules, validation rules, and data transformation rules
/// used throughout the investment tracking system. Covers broker data formats, import/storage rules,
/// and other domain-specific business logic.
/// </summary>
[McpServerToolType]
public static class BusinessRulesManager
{
    #region Option Trade Business Rules

    [McpServerTool]
    [Description("Get comprehensive rules for Option Trade Import & Storage. Defines data model consistency between UI and CSV import, FIFO matching algorithm, and IsOpen flag management.")]
    public static async Task<string> GetOptionTradeImportStorageRules()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("business-rules/option-trade-import-rules.md");
    }

    #endregion

    #region Broker Data Format Rules

    [McpServerTool]
    [Description("Get comprehensive broker data format overview.")]
    public static async Task<string> GetBrokerFormatOverview()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("broker-data-formats/broker-format-overview.md");
    }

    [McpServerTool]
    [Description("Get Tastytrade strike adjustment parsing guide.")]
    public static async Task<string> GetTastytradeStrikeAdjustmentGuide()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("broker-data-formats/tastytrade-strike-adjustment-guide.md");
    }

    [McpServerTool]
    [Description("Get common broker patterns.")]
    public static async Task<string> GetCommonBrokerPatterns()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("broker-data-formats/common-patterns.md");
    }

    [McpServerTool]
    [Description("Get broker data validation rules.")]
    public static async Task<string> GetBrokerDataValidationRules()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("broker-data-formats/validation-rules.md");
    }

    #endregion
}
