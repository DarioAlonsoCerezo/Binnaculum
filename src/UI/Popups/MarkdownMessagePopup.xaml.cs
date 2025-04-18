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

        OkButton.Events().Clicked
            .Subscribe(_ =>
            {
                Close();
            }).DisposeWith(Disposables);

    }
}