using Microsoft.Maui.Animations;

namespace Binnaculum.Controls;

public class RadioButtonDrawable : IDrawable
{
    // Properties
    public bool IsChecked { get; set; }
    public Color BorderColor { get; set; }
    public Color FillColor { get; set; }

    // Animation
    internal float _animationPercent;

    public RadioButtonDrawable()
    {
        _animationPercent = 0f;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Enable anti-aliasing for smoother drawing
        canvas.Antialias = true;

        // Draw the radio button
        DrawRadioButton(canvas, dirtyRect);
    }

    private void DrawRadioButton(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        // Calculate dimensions
        float diameter = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float radius = diameter / 2;
        float centerX = dirtyRect.Center.X;
        float centerY = dirtyRect.Center.Y;
        float strokeWidth = diameter * 0.1f; // 10% of diameter for stroke width

        // Draw outer circle (border)
        canvas.StrokeColor = BorderColor;
        canvas.StrokeSize = strokeWidth;
        canvas.DrawCircle(centerX, centerY, radius - (strokeWidth / 2));

        // Draw inner circle (fill) when checked with animation
        if (IsChecked || _animationPercent > 0)
        {
            float innerRadius = (radius - strokeWidth - 2) * _animationPercent;
            canvas.FillColor = FillColor;
            canvas.FillCircle(centerX, centerY, innerRadius);
        }

        canvas.RestoreState();
    }

    public void StartAnimation(bool isChecked)
    {
        // This will be handled by the animation manager in the CustomRadioButton class
    }

    // Linear interpolation helper method
    private float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }
}
