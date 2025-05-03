using CommunityToolkit.Maui.Behaviors;

namespace Binnaculum.Controls;

public partial class TintedImage
{
    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(TintedImage), default(ImageSource));
    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly BindableProperty TintColorProperty =
        BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(TintedImage), default(Color));

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public TintedImage()
	{
		InitializeComponent();        
    }

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.ImageSource)
            .Where(x => x != null)
            .ObserveOn(UiThread)
            .BindTo(ImageControl, x => x.Source)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.TintColor)
            .Where(x => x != null)
            .ObserveOn(UiThread)
            .Do(color =>
            {
                ImageControl.Behaviors.Clear();
                ImageControl.Behaviors.Add(new IconTintColorBehavior { TintColor = color });
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }
}