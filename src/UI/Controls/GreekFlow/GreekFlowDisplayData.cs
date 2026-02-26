namespace Binnaculum.Controls;

/// <summary>
/// Normalized display data contract for GreekFlowControl.
/// LongExposure and ShortExposure are absolute values (always ≥ 0).
/// NetExposure may be positive or negative.
/// MaxAbsExposure is used to scale the exposure bars (must be > 0).
/// IsFlipZone signals that net exposure is near the flip threshold.
/// </summary>
public record GreekFlowDisplayData(
    decimal LongExposure,
    decimal ShortExposure,
    decimal NetExposure,
    decimal MaxAbsExposure,
    bool IsFlipZone,
    string? LongLabelOverride = null,
    string? ShortLabelOverride = null);
