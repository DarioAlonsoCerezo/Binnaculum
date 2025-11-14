using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class InformationButton
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        propertyName: nameof(Text),
        returnType: typeof(string),
        declaringType: typeof(InformationButton),
        defaultValue: default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private readonly TapGestureRecognizer _controlTap;

    public InformationButton()
	{
        _controlTap = new TapGestureRecognizer();
        
        InitializeComponent();

        GestureRecognizers.Add(_controlTap);
        _controlTap.Events().Tapped
            .Subscribe(_ =>
            {
                new MarkdownMessagePopup()
                {
                    Text = Text,
                }.Show();
            })
            .DisposeWith(Disposables);
    }

    protected override void StartLoad()
    {
        
    }
}