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

    public BorderedEntry()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.Information, x => x.IsEnabled)
            .Select(x => !string.IsNullOrEmpty(x.Item1) && x.Item2)
            .ObserveOn(UiThread)
            .BindTo(InformationButton, x => x.IsVisible)
            .DisposeWith(Disposables);

        BorderlessEntry.Events().TextChanged
            .Subscribe(e =>
            {
                TextChanged?.Invoke(this, e);
            }).DisposeWith(Disposables);

        BorderlessEntry.Events().Completed
            .Subscribe(e =>
            {
                Completed?.Invoke(this, e);
            }).DisposeWith(Disposables);
    }
}