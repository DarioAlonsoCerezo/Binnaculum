using Microsoft.Maui.Animations;

namespace Binnaculum.Controls;

public class CustomRadioButton : GraphicsView
{
    private readonly RadioButtonDrawable _radioButtonDrawable;
    private IAnimationManager _animationManager;

    public event EventHandler<bool>? CheckedChanged;
    public event EventHandler? Clicked;

    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(
        nameof(IsChecked),
        typeof(bool),
        typeof(CustomRadioButton),
        false,
        BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomRadioButton radioButton && newValue is bool isChecked)
                radioButton.UpdateIsChecked();
        });

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor),
        typeof(Color),
        typeof(CustomRadioButton),
        Colors.Gray,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomRadioButton radioButton && newValue is Color)
                radioButton.UpdateBorderColor();
        });

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty CheckedBorderColorProperty = BindableProperty.Create(
        nameof(CheckedBorderColor),
        typeof(Color),
        typeof(CustomRadioButton),
        null,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomRadioButton radioButton && newValue is Color)
                radioButton.UpdateBorderColor();
        });

    public Color CheckedBorderColor
    {
        get => (Color)GetValue(CheckedBorderColorProperty) ??
               Application.Current?.Resources["Primary"] as Color ?? Colors.Purple;
        set => SetValue(CheckedBorderColorProperty, value);
    }

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor),
        typeof(Color),
        typeof(CustomRadioButton),
        null,
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is CustomRadioButton radioButton && newValue is Color)
                radioButton.UpdateFillColor();
        });

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty) ??
               Application.Current?.Resources["Primary"] as Color ?? Colors.Purple;
        set => SetValue(FillColorProperty, value);
    }

    public CustomRadioButton()
    {
        // Create and assign the drawable
        _radioButtonDrawable = new RadioButtonDrawable();
        this.Drawable = _radioButtonDrawable;

        // Set default size
        this.WidthRequest = 24;
        this.HeightRequest = 24;

        // Add interaction handler
        StartInteraction += OnRadioButtonStartInteraction;
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
            UpdateBorderColor();
            UpdateFillColor();
            UpdateIsChecked(false); // Don't animate on initial setup
        }
    }

    private void OnRadioButtonStartInteraction(object? sender, TouchEventArgs e)
    {
        if(IsEnabled)
        {
            if (IsChecked)
                return;

            Clicked?.Invoke(this, EventArgs.Empty);
            IsChecked = !IsChecked;
        }        
    }

    private void UpdateBorderColor()
    {
        if (_radioButtonDrawable == null)
            return;

        _radioButtonDrawable.BorderColor = IsChecked ? CheckedBorderColor : BorderColor;
        Invalidate();
    }

    private void UpdateFillColor()
    {
        if (_radioButtonDrawable == null)
            return;

        _radioButtonDrawable.FillColor = FillColor;
        Invalidate();
    }

    private void UpdateIsChecked(bool animate = true)
    {
        if (_radioButtonDrawable == null)
            return;

        _radioButtonDrawable.IsChecked = IsChecked;
        UpdateBorderColor();

        // Trigger events for state change
        CheckedChanged?.Invoke(this, IsChecked);

        if (animate && _animationManager != null)
            AnimateCheck();
        else
        {
            _radioButtonDrawable._animationPercent = IsChecked ? 1.0f : 0.0f;
            Invalidate();
        }
    }

    private void AnimateCheck()
    {
        if (_radioButtonDrawable == null || _animationManager == null)
            return;

        float start = IsChecked ? 0 : 1;
        float end = IsChecked ? 1 : 0;

        // Create a smooth animation for the check/uncheck
        _animationManager.Add(new Microsoft.Maui.Animations.Animation(callback: (progress) =>
        {
            _radioButtonDrawable._animationPercent = start.Lerp(end, progress);
            Invalidate();
        },
        // Animation duration
        duration: 0.2,
        // Using a cubic ease-in-out for a natural feel
        easing: Easing.CubicInOut));
    }
}
