namespace Binnaculum.Popups;

public partial class MarkdownMessagePopup
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
    propertyName: nameof(Text),
    returnType: typeof(string),
    declaringType: typeof(MarkdownMessagePopup),
    defaultValue: string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MarkdownMessagePopup()
	{
		InitializeComponent();

        ApplyHeightPercentage(this, 0.65);

        OkButton.Events().Clicked
            .Subscribe(_ =>
            {
                Close();
            }).DisposeWith(Disposables);

    }
}