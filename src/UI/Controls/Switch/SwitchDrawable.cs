namespace Binnaculum.Controls;

public class SwitchDrawable : IDrawable
{
    // Properties
    public bool IsOn { get; set; }
    public Color TrackColor { get; set; }
    public Color ThumbColor { get; set; }

    // Animation
    internal float _animationPercent;

    public SwitchDrawable()
    {
        _animationPercent = 0f;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Enable anti-aliasing for smoother drawing
        canvas.Antialias = true;

        // Draw track and thumb
        DrawTrack(canvas, dirtyRect);
        DrawThumb(canvas, dirtyRect);
    }

    public virtual void DrawTrack(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        // Set track color
        canvas.FillColor = TrackColor;

        // iOS tracks are more pill-shaped with consistent corner radius
        float trackHeight = dirtyRect.Height * 0.8f; // Taller track for iOS style
        float trackWidth = dirtyRect.Width;
        float trackY = dirtyRect.Center.Y - (trackHeight / 2);

        // Create track rectangle with rounded corners
        var trackRect = new RectF(0, trackY, trackWidth, trackHeight);
        float trackCornerRadius = trackHeight / 2; // Fully rounded ends

        canvas.FillRoundedRectangle(trackRect, trackCornerRadius);

        canvas.RestoreState();
    }

    public virtual void DrawThumb(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        // Calculate track dimensions to position thumb inside track
        float trackHeight = dirtyRect.Height * 0.8f;
        float trackY = dirtyRect.Center.Y - (trackHeight / 2);

        // Keep the vertical thumb padding at the current value
        float thumbPadding = dirtyRect.Height * 0.1f;

        // Make the thumb slightly smaller to accommodate increased margin
        float thumbDiameter = trackHeight - (thumbPadding * 2);
        float thumbRadius = thumbDiameter / 2;

        // Calculate thumb positions with reduced horizontal edge margins
        // Reduced from 0.12f to 0.06f (half the previous value)
        float edgeMargin = dirtyRect.Width * 0.06f;
        float leftEdge = edgeMargin;
        float rightEdge = dirtyRect.Width - edgeMargin - thumbDiameter;

        // Linear interpolation between positions based on animation percent
        float thumbX = Lerp(leftEdge, rightEdge, _animationPercent);
        float thumbY = trackY + thumbPadding;

        // Draw subtle shadow for iOS feel
        if (IsOn)
        {
            // Shadow when switch is on is subtle
            canvas.SetShadow(new SizeF(0, 1), 1.5f, Colors.Black.WithAlpha(0.2f));
        }
        else
        {
            canvas.SetShadow(new SizeF(0, 1), 2f, Colors.Black.WithAlpha(0.3f));
        }

        // Draw thumb (circle)
        canvas.FillColor = ThumbColor;
        canvas.FillCircle(thumbX + thumbRadius, thumbY + thumbRadius, thumbRadius);

        canvas.RestoreState();
    }

    // Linear interpolation helper method
    private float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }
}

