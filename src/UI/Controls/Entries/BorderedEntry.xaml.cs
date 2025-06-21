using Binnaculum.Core;
using Binnaculum.Popups;
using CommunityToolkit.Maui.Core.Platform;

namespace Binnaculum.Controls;

public partial class BorderedEntry
{
    public event EventHandler<TextChangedEventArgs> TextChanged;
    public event EventHandler<string> CurrencyChanged;
    public event EventHandler Completed;

    // Add a flag to prevent circular updates
    private bool _isUpdatedOutside = false;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text), 
            typeof(string), 
            typeof(BorderedEntry), 
            default(string),
            propertyChanged:(bindable, oldValue, newValue) => 
            {
                if (bindable is BorderedEntry borderedEntry)
                {
                    // Set flag to prevent circular updates
                    borderedEntry._isUpdatedOutside = true;
                    borderedEntry.BorderlessEntry.Text = (string)newValue;
                }
            });

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
    BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(BorderedEntry), default(string),
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is BorderedEntry borderedEntry)
                borderedEntry.BorderlessEntry.Placeholder = (string)newValue;
        });

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty PlaceholderColorProperty =
        BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(BorderedEntry), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BorderedEntry borderedEntry)
                    borderedEntry.BorderlessEntry.PlaceholderColor = (Color)newValue;
            });

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly BindableProperty InformationProperty =
        BindableProperty.Create(nameof(Information), typeof(string), typeof(BorderedEntry), default(string));

    public string Information
    {
        get => (string)GetValue(InformationProperty);
        set => SetValue(InformationProperty, value);
    }

    public static readonly BindableProperty IsCurrencyVisibleProperty =
        BindableProperty.Create(nameof(IsCurrencyVisible), typeof(bool), typeof(BorderedEntry), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BorderedEntry borderedEntry && newValue is bool enabled)
                {
                    borderedEntry.CurrencyLabel.IsVisible = enabled;
                }
            });

    public bool IsCurrencyVisible
    {
        get => (bool)GetValue(IsCurrencyVisibleProperty);
        set => SetValue(IsCurrencyVisibleProperty, value);
    }

    public static readonly BindableProperty IsMoneyEntryProperty =
        BindableProperty.Create(nameof(IsMoneyEntry), typeof(bool), typeof(BorderedEntry), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BorderedEntry borderedEntry && newValue is bool enabled)
                {
                    borderedEntry.BorderlessEntry.Keyboard = enabled ? Keyboard.Numeric : Keyboard.Default;
                }
            });

    public bool IsMoneyEntry
    {
        get => (bool)GetValue(IsMoneyEntryProperty);
        set => SetValue(IsMoneyEntryProperty, value);
    }

    public static readonly BindableProperty TextTransformProperty =
        BindableProperty.Create(nameof(TextTransform), typeof(TextTransform), typeof(BorderedEntry), TextTransform.None,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BorderedEntry borderedEntry)
                    borderedEntry.BorderlessEntry.TextTransform = (TextTransform)newValue;
            });

    public TextTransform TextTransform
    {
        get => (TextTransform)GetValue(TextTransformProperty);
        set => SetValue(TextTransformProperty, value);
    }

    public string SelectedCurrencyText => CurrencyLabel.Text;

    public async Task Unfocus(bool hideKeyboard = false)
    {
        if(BorderlessEntry.IsFocused)
            BorderlessEntry.Unfocus();

        if(BorderlessEntry.IsSoftKeyboardShowing() && hideKeyboard)
            await ((Entry)BorderlessEntry).HideKeyboardAsync();
    }

    public BorderedEntry()
	{
		InitializeComponent();

        CurrencyLabel.Text = Core.UI.SavedPrefereces.UserPreferences.Value.Currency;
        CurrencyChanged?.Invoke(this, CurrencyLabel.Text);
    }

    protected override void StartLoad()
    {
        var isVisible = this.WhenAnyValue(x => x.Information, x => x.IsEnabled)
            .Select(x => !string.IsNullOrEmpty(x.Item1) && x.Item2)
            .ObserveOn(UiThread);

        isVisible
            .BindTo(InformationMarkdownButton, x => x.IsVisible)
            .DisposeWith(Disposables);

        isVisible
            .Select(x => x ? new Thickness(0,0,46,0) : new Thickness(0))
            .BindTo(BorderlessEntry, x => x.Margin)
            .DisposeWith(Disposables);

        BorderlessEntry.Events().TextChanged
            .ObserveOn(UiThread)
            .LogWhileDebug("BorderlessEntry TextChanged")
            .Subscribe(e =>
            {
                TextChanged?.Invoke(this, e);

                // Only update the Text property if we're not already updating from it
                if (_isUpdatedOutside)
                {
                    _isUpdatedOutside = false;
                    return;
                }
                Text = e.NewTextValue;

            }).DisposeWith(Disposables);

        BorderlessEntry.Events().Completed
            .Subscribe(e =>
            {
                Completed?.Invoke(this, e);
            }).DisposeWith(Disposables);

        CurrencyGesture.Events().Tapped
        .SelectMany(_ => Observable.FromAsync(async () =>
        {
            var popupResult = await new CurrencySelectorPopup().ShowAndWait();
            if (popupResult.Result is Models.Currency currency)
            {
                CurrencyLabel.Text = currency.Code;
                CurrencyChanged?.Invoke(this, currency.Code);
            }
            return Unit.Default; // Return Unit.Default as a "void" equivalent
        }))
        .Subscribe()
        .DisposeWith(Disposables);
    }
}