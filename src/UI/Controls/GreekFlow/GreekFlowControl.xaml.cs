namespace Binnaculum.Controls;

/// <summary>
/// Reusable control that displays option-greek exposure data (Delta, Gamma, Vanna or Charm).
/// Set <see cref="GreekKind"/> to choose which greek to represent and
/// <see cref="DisplayData"/> (or call <see cref="Update"/>) to populate values.
/// </summary>
public partial class GreekFlowControl
{
    private double _containerWidth;

    // ──────────────────────────────────────────────
    // BindableProperties
    // ──────────────────────────────────────────────

    public static readonly BindableProperty GreekKindProperty =
        BindableProperty.Create(
            nameof(GreekKind),
            typeof(GreekKind),
            typeof(GreekFlowControl),
            GreekKind.Delta,
            propertyChanged: OnGreekKindChanged);

    public GreekKind GreekKind
    {
        get => (GreekKind)GetValue(GreekKindProperty);
        set => SetValue(GreekKindProperty, value);
    }

    public static readonly BindableProperty DisplayDataProperty =
        BindableProperty.Create(
            nameof(DisplayData),
            typeof(GreekFlowDisplayData),
            typeof(GreekFlowControl),
            null,
            propertyChanged: OnDisplayDataChanged);

    public GreekFlowDisplayData? DisplayData
    {
        get => (GreekFlowDisplayData?)GetValue(DisplayDataProperty);
        set => SetValue(DisplayDataProperty, value);
    }

    public static readonly BindableProperty TitleOverrideProperty =
        BindableProperty.Create(
            nameof(TitleOverride),
            typeof(string),
            typeof(GreekFlowControl),
            null,
            propertyChanged: OnTitleOverrideChanged);

    public string? TitleOverride
    {
        get => (string?)GetValue(TitleOverrideProperty);
        set => SetValue(TitleOverrideProperty, value);
    }

    // ──────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────

    public GreekFlowControl()
    {
        InitializeComponent();
    }

    // ──────────────────────────────────────────────
    // BaseContentView overrides
    // ──────────────────────────────────────────────

    protected override void StartLoad()
    {
        // Track the container width so that exposure bars can be scaled.
        this.Events().SizeChanged
            .ObserveOn(UiThread)
            .Subscribe(e =>
            {
                _containerWidth = Width;
                RefreshBars();
            })
            .DisposeWith(Disposables);

        ApplyGreekKind(GreekKind);
        ApplyDisplayData(DisplayData);
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Updates the control with new display data without changing GreekKind.
    /// </summary>
    public void Update(GreekFlowDisplayData data) => DisplayData = data;

    /// <summary>
    /// Clears all exposure values and hides the flip-zone warning.
    /// </summary>
    public void ClearDisplay()
    {
        LongValue.Text = FormatExposure(0);
        ShortValue.Text = FormatExposure(0);
        NetValue.Text = FormatExposure(0);
        LongBar.WidthRequest = 0;
        ShortBar.WidthRequest = 0;
        FlipZoneWarning.IsVisible = false;
        NetValue.TextColor = NeutralTextColor();
    }

    // ──────────────────────────────────────────────
    // Property-changed callbacks
    // ──────────────────────────────────────────────

    private static void OnGreekKindChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is GreekFlowControl ctrl && newValue is GreekKind kind)
            ctrl.Dispatcher.Dispatch(() => ctrl.ApplyGreekKind(kind));
    }

    private static void OnDisplayDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is GreekFlowControl ctrl)
            ctrl.Dispatcher.Dispatch(() => ctrl.ApplyDisplayData(newValue as GreekFlowDisplayData));
    }

    private static void OnTitleOverrideChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is GreekFlowControl ctrl)
            ctrl.Dispatcher.Dispatch(() => ctrl.ApplyTitleOverride(newValue as string));
    }

    // ──────────────────────────────────────────────
    // Internal helpers
    // ──────────────────────────────────────────────

    private void ApplyGreekKind(GreekKind kind)
    {
        var meta = GreekMetadata.For(kind);
        GreekSymbol.Text = meta.Symbol;
        TitleLabel.Text = TitleOverride ?? meta.Title;
        LongLabel.Text = meta.LongLabel;
        ShortLabel.Text = meta.ShortLabel;
        NetLabel.Text = meta.NetLabel;
    }

    private void ApplyTitleOverride(string? title)
    {
        TitleLabel.Text = string.IsNullOrWhiteSpace(title)
            ? GreekMetadata.For(GreekKind).Title
            : title;
    }

    private void ApplyDisplayData(GreekFlowDisplayData? data)
    {
        if (data is null)
        {
            ClearDisplay();
            return;
        }

        // Reset labels to per-greek defaults first, then apply optional overrides.
        var meta = GreekMetadata.For(GreekKind);
        LongLabel.Text = string.IsNullOrWhiteSpace(data.LongLabelOverride) ? meta.LongLabel : data.LongLabelOverride;
        ShortLabel.Text = string.IsNullOrWhiteSpace(data.ShortLabelOverride) ? meta.ShortLabel : data.ShortLabelOverride;

        LongValue.Text = FormatExposure(data.LongExposure);
        ShortValue.Text = FormatExposure(data.ShortExposure);
        NetValue.Text = FormatExposure(data.NetExposure);

        NetValue.TextColor = data.NetExposure > 0
            ? GreenColor()
            : data.NetExposure < 0
                ? RedColor()
                : NeutralTextColor();

        FlipZoneWarning.IsVisible = data.IsFlipZone;

        RefreshBars();
    }

    private void RefreshBars()
    {
        var data = DisplayData;
        if (data is null || _containerWidth <= 0 || data.MaxAbsExposure <= 0)
        {
            LongBar.WidthRequest = 0;
            ShortBar.WidthRequest = 0;
            return;
        }

        // Subtract padding (12 each side = 24) from the available bar width
        var availableWidth = Math.Max(0, _containerWidth - 24);
        LongBar.WidthRequest = (double)(data.LongExposure / data.MaxAbsExposure) * availableWidth;
        ShortBar.WidthRequest = (double)(data.ShortExposure / data.MaxAbsExposure) * availableWidth;
    }

    private static string FormatExposure(decimal value) =>
        value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

    private static Color GreenColor() =>
        (Color)Application.Current!.Resources["GreenState"];

    private static Color RedColor() =>
        (Color)Application.Current!.Resources["RedState"];

    private Color NeutralTextColor() =>
        CurrentTheme == AppTheme.Dark
            ? (Color)Application.Current!.Resources["White"]
            : (Color)Application.Current!.Resources["Black"];
}
