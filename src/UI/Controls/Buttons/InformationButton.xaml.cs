using Binnaculum.Popups;
using CommunityToolkit.Maui.Views;

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
                var popup = new MarkdownMessagePopup()
                {
                    Text = Text,
                };
                var appMainpage = Application.Current!.Windows[0].Page!;
                if (appMainpage is NavigationPage navigator)
                    navigator.ShowPopup(popup);
            })
            .DisposeWith(Disposables);
    }

    protected override void StartLoad()
    {
        
    }
}