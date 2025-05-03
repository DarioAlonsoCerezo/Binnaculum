using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class BorderedEntry
{
    public event EventHandler<TextChangedEventArgs> TextChanged;
    public event EventHandler Completed;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(BorderedEntry), default(string));

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
                if (bindable is BorderedEntry borderedEntry)
                    borderedEntry.CurrencyLabel.IsVisible = (bool)newValue;
            });

    public bool IsCurrencyVisible
    {
        get => (bool)GetValue(IsCurrencyVisibleProperty);
        set => SetValue(IsCurrencyVisibleProperty, value);
    }

    public BorderedEntry()
	{
		InitializeComponent();

        CurrencyLabel.Text = Preferences.Get("Currency", "USD");
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
            .Subscribe(e =>
            {
                Text = e.NewTextValue;
                TextChanged?.Invoke(this, e);
            }).DisposeWith(Disposables);

        BorderlessEntry.Events().Completed
            .Subscribe(e =>
            {
                Completed?.Invoke(this, e);
            }).DisposeWith(Disposables);

        CurrencyGesture.Events().Tapped
        .SelectMany(_ => Observable.FromAsync(async () =>
        {
            var popup = new CurrencySelectorPopup();
            var appMainpage = Application.Current!.Windows[0].Page!;
            if (appMainpage is NavigationPage navigator)
            {
                var result = await navigator.ShowPopupAsync(popup);
                if (result is Models.Currency currency)
                {
                    CurrencyLabel.Text = currency.Code;
                }
            }
            return Unit.Default; // Return Unit.Default as a "void" equivalent
        }))
        .Subscribe()
        .DisposeWith(Disposables);
    }
}