using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

#if ANDROID
using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls.Platform;
#endif

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace Binnaculum.Handlers;

public partial class BorderlessEditorHandler : EditorHandler
{
}

#if ANDROID
public partial class BorderlessEditorHandler : EditorHandler
{
    protected override AppCompatEditText CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        using (var gradientDrawable = new GradientDrawable())
        {
            gradientDrawable.SetColor(global::Android.Graphics.Color.Transparent);
            nativeView.SetBackground(gradientDrawable);
        }

        return nativeView;
    }
}
#endif

#if IOS || MACCATALYST
public partial class BorderlessEditorHandler : EditorHandler
{
    /* No any custom implementation required
     * Just keeping this handler to prevent build errors.
     */
}
#endif

#if WINDOWS
public partial class BorderlessEditorHandler : EditorHandler
{
    protected override TextBox CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        nativeView.Style = null;
        return nativeView;
    }
}
#endif
