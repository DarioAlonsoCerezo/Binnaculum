namespace Binnaculum.Controls;

/// <summary>
/// Per-greek display metadata: symbol, title and axis labels.
/// </summary>
internal sealed record GreekMeta(
    string Symbol,
    string Title,
    string LongLabel,
    string ShortLabel,
    string NetLabel);

internal static class GreekMetadata
{
    private static readonly IReadOnlyDictionary<GreekKind, GreekMeta> _map =
        new Dictionary<GreekKind, GreekMeta>
        {
            [GreekKind.Delta] = new("Δ", "Delta", "Long Δ", "Short Δ", "Net Δ"),
            [GreekKind.Gamma] = new("Γ", "Gamma", "Long Γ", "Short Γ", "Net Γ"),
            [GreekKind.Vanna] = new("V", "Vanna", "Long Vanna", "Short Vanna", "Net Vanna"),
            [GreekKind.Charm] = new("C", "Charm", "Long Charm", "Short Charm", "Net Charm"),
        };

    public static GreekMeta For(GreekKind kind) =>
        _map.TryGetValue(kind, out var meta)
            ? meta
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown GreekKind.");
}
