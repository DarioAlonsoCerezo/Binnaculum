using Microsoft.Maui.Controls.Shapes;

namespace Binnaculum.Controls;

public partial class ButtonAdd
{
    public event EventHandler? AddClicked;

    // Base values for scaling
    private const double BASE_SIZE = 100;
    private const double BASE_STROKE_THICKNESS = 3;
    private const double BASE_CORNER_RADIUS = 20;
    private const double BASE_PLUS_WIDTH = 30;
    private const double BASE_PLUS_HEIGHT = 3;
    
    public static readonly BindableProperty ScaleButtonProperty = BindableProperty.Create(
        nameof(ScaleButton),
        typeof(double),
        typeof(ButtonAdd),
        1.0,
        propertyChanged: OnScaleChanged);

    public double ScaleButton
    {
        get => (double)GetValue(ScaleButtonProperty);
        set => SetValue(ScaleButtonProperty, value);
    }

    private static void OnScaleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ButtonAdd button && newValue is double scale)
        {
            // Scale the overall size
            button.WidthRequest = BASE_SIZE * scale;
            button.HeightRequest = BASE_SIZE * scale;

            // Scale the border properties
            if (button.ButtonBorder != null)
            {
                button.ButtonBorder.StrokeThickness = BASE_STROKE_THICKNESS * scale;

                if (button.ButtonBorder.StrokeShape is RoundRectangle roundRect)
                {
                    roundRect.CornerRadius = new CornerRadius(BASE_CORNER_RADIUS * scale);
                }
            }

            // Scale the plus sign
            if (button.HorizontalLine != null)
            {
                button.HorizontalLine.WidthRequest = BASE_PLUS_WIDTH * scale;
                button.HorizontalLine.HeightRequest = BASE_PLUS_HEIGHT * scale;
            }

            if (button.VerticalLine != null)
            {
                button.VerticalLine.WidthRequest = BASE_PLUS_HEIGHT * scale;
                button.VerticalLine.HeightRequest = BASE_PLUS_WIDTH * scale;
            }
        }
    }

    public ButtonAdd()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        BorderGesture.Events().Tapped
            .Do(_ => AddClicked?.Invoke(this, EventArgs.Empty))
            .Subscribe().DisposeWith(Disposables);
    }
}