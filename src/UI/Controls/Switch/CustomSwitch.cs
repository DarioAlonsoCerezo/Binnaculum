using Microsoft.Maui.Animations;

namespace Binnaculum.Controls;

public class CustomSwitch : GraphicsView
{
    private readonly SwitchDrawable _switchDrawable;
    private IAnimationManager _animationManager;

    public event EventHandler<bool>? Toggled;

    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(
        nameof(IsOn),
        typeof(bool),
        typeof(CustomSwitch),
        false,
        BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomSwitch switchControl && newValue is bool isOn)
                switchControl.UpdateIsOn();
        });

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor),
        typeof(Color),
        typeof(CustomSwitch),
        Color.FromArgb("#F2F7FA"), // OffWhite
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomSwitch switchControl && newValue is Color)
                switchControl.UpdateTrackColor();
        });

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public static readonly BindableProperty OnTrackColorProperty = BindableProperty.Create(
        nameof(OnTrackColor),
        typeof(Color),
        typeof(CustomSwitch),
        Color.FromArgb("#512BD4"), // Primary color
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomSwitch switchControl && newValue is Color)
                switchControl.UpdateTrackColor();
        });

    public Color OnTrackColor
    {
        get => (Color)GetValue(OnTrackColorProperty);
        set => SetValue(OnTrackColorProperty, value);
    }

    public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(
        nameof(ThumbColor),
        typeof(Color),
        typeof(CustomSwitch),
        Colors.White, // White
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomSwitch switchControl && newValue is Color)
                switchControl.UpdateThumbColor();
        });

    public Color ThumbColor
    {
        get => (Color)GetValue(ThumbColorProperty);
        set => SetValue(ThumbColorProperty, value);
    }

    public CustomSwitch()
    {
        // Create and assign the drawable
        _switchDrawable = new SwitchDrawable();
        this.Drawable = _switchDrawable;

        // Set default size - iOS switches are a bit wider and less tall
        this.WidthRequest = 51;
        this.HeightRequest = 31;

        // Add interaction handler
        StartInteraction += OnSwitchStartInteraction;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent != null)
        {
            // Initialize animation manager
#if __ANDROID__
            _animationManager = new AnimationManager(new PlatformTicker(new Microsoft.Maui.Platform.EnergySaverListenerManager()));
#else
            _animationManager = new AnimationManager(new PlatformTicker());
#endif

            // Initialize visual state
            UpdateTrackColor();
            UpdateThumbColor();
            UpdateIsOn(false); // Don't animate on initial setup
        }
    }

    private void OnSwitchStartInteraction(object? sender, TouchEventArgs e)
    {
        if (IsEnabled)
        {
            IsOn = !IsOn;
        }
    }

    private void UpdateTrackColor()
    {
        if (_switchDrawable == null)
            return;

        _switchDrawable.TrackColor = IsOn ? OnTrackColor : TrackColor;
        Invalidate();
    }

    private void UpdateThumbColor()
    {
        if (_switchDrawable == null)
            return;

        _switchDrawable.ThumbColor = ThumbColor;
        Invalidate();
    }

    private void UpdateIsOn(bool animate = true)
    {
        if (_switchDrawable == null)
            return;

        _switchDrawable.IsOn = IsOn;
        UpdateTrackColor();

        // Trigger events for state change
        Toggled?.Invoke(this, IsOn);

        if (animate && _animationManager != null)
            AnimateToggle();
        else
            Invalidate();
    }

    private void AnimateToggle()
    {
        if (_switchDrawable == null || _animationManager == null)
            return;

        float start = IsOn ? 0 : 1;
        float end = IsOn ? 1 : 0;

        // Create a softer, longer animation for the toggle
        _animationManager.Add(new Microsoft.Maui.Animations.Animation(callback: (progress) =>
        {
            // Using the built-in easing function
            _switchDrawable._animationPercent = start.Lerp(end, progress);
            Invalidate();
        },
        // Animation duration
        duration: 0.35,
        // Using a built-in easing function for a soft feel
        easing: Easing.CubicInOut));
    }
}
