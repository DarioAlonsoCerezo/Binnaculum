using System.Reflection;

namespace Binnaculum.MCP.Managers;

/// <summary>
/// Shared helper for loading embedded resources from the MCP assembly.
/// Used by all manager classes to access markdown documentation.
/// </summary>
internal static class ResourceLoaderHelper
{
    /// <summary>
    /// Loads an embedded resource from the assembly as text.
    /// Handles path transformation from folder structure to resource naming conventions.
    /// </summary>
    /// <param name="resourcePath">Path to the resource (e.g., "domain-patterns/datetime-pattern.md")</param>
    /// <returns>The resource content as a string, or an error message if not found</returns>
    internal static async Task<string> LoadEmbeddedResourceAsync(string resourcePath)
    {
        try
        {
            var assembly = typeof(ResourceLoaderHelper).Assembly;

            // Build several possible resource names and try them
            var candidates = new List<string>();

            // The actual .NET resource naming pattern appears to be:
            // - Directory separators (/) become dots (.)
            // - Directory hyphens (-) become underscores (_)
            // - File hyphens (-) stay as hyphens
            // - Extension (.md) is kept in the resource name

            // Candidate 1: Replace "/" with "." and directory hyphens with underscores
            // "domain-patterns/datetime-pattern.md" -> "Binnaculum.MCP.Resources.domain_patterns.datetime-pattern.md"
            var parts = resourcePath.Split('/');
            var transformedParts = parts.Select((part, index) =>
            {
                // Last part is the filename, keep its hyphens
                if (index == parts.Length - 1) return part;
                // Directory parts: replace hyphens with underscores
                return part.Replace("-", "_");
            });
            var candidate1 = $"Binnaculum.MCP.Resources.{string.Join(".", transformedParts)}";
            candidates.Add(candidate1);

            // Candidate 2: Without the .md extension
            if (resourcePath.EndsWith(".md"))
            {
                candidates.Add(candidate1.Substring(0, candidate1.Length - 3));
            }

            // Candidate 3: All hyphens to underscores
            candidates.Add(candidate1.Replace("-", "_"));

            // Try each candidate
            foreach (var resourceName in candidates)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync();
                }
            }

            // If none found, list available resources for debugging
            var allResources = string.Join(", ", assembly.GetManifestResourceNames()
                .Where(r => r.Contains("datetime", StringComparison.OrdinalIgnoreCase))
                .Take(10));

            return $"Resource '{resourcePath}' not found. Tried: {string.Join("; ", candidates)}. Available resources with 'datetime': {allResources}";
        }
        catch (Exception ex)
        {
            return $"Error loading resource '{resourcePath}': {ex.Message}";
        }
    }    /// <summary>
         /// Transforms a file path to the corresponding embedded resource name.
         /// Example: "domain-patterns/datetime-pattern.md" -> "Binnaculum.MCP.Resources.domain-patterns.datetime-pattern"
         /// Note: .NET embedded resources preserve hyphens in folder/file names, using dots only for path separators.
         /// </summary>
    private static string TransformPathToResourceName(string resourcePath)
    {
        // Remove .md extension if present
        var pathWithoutExtension = resourcePath.EndsWith(".md")
            ? resourcePath.Substring(0, resourcePath.Length - 3)
            : resourcePath;

        // Replace path separators (/) with dots (.) for .NET resource naming
        // Keep hyphens as-is since .NET preserves them in resource names
        var resourceName = pathWithoutExtension.Replace("/", ".");

        return $"Binnaculum.MCP.Resources.{resourceName}";
    }
}
