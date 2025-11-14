namespace Binnaculum.MCP.Managers;

/// <summary>
/// Exposes documentation for domain pattern types used throughout Binnaculum.
/// Domain patterns are wrapped types (discriminated unions) that enforce type safety, format consistency,
/// and centralize business logic for core domain concepts.
/// </summary>
[McpServerToolType]
public static class DomainPatternsManager
{
    #region Domain Types Documentation

    [McpServerTool]
    [Description("Get comprehensive explanation of DateTimePattern domain type. Covers type safety, format consistency, usage patterns, and best practices for temporal handling in the investment tracking system.")]
    public static async Task<string> GetDateTimePatternDocumentation()
    {
        return await ResourceLoaderHelper.LoadEmbeddedResourceAsync("domain-patterns/datetime-pattern.md");
    }

    #endregion
}
