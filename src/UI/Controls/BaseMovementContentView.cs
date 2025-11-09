namespace Binnaculum.Controls;

/// <summary>
/// Base class for all movement templates with fixed height constraints.
/// Ensures consistent 120px height across all movement types for CollectionView virtualization.
/// </summary>
public abstract class BaseMovementContentView : BaseContentView
{
    protected BaseMovementContentView()
    {
        HeightRequest = 120;
        MinimumHeightRequest = 120;
        MaximumHeightRequest = 120;
    }
}
