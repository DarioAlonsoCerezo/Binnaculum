using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#if ANDROID
using Android.Graphics.Drawables;
using Microsoft.Maui.Controls.Platform;
#endif
#if IOS || MACCATALYST
using UIKit;
#endif
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace Binnaculum.Handlers;

public partial class BorderlessDatePickerHandler : DatePickerHandler
{
    public BorderlessDatePickerHandler()
    {
    }

    public BorderlessDatePickerHandler(IPropertyMapper? mapper = null) : base(mapper)
    {
    }
}

#if ANDROID
public partial class BorderlessDatePickerHandler : DatePickerHandler
{
    protected override MauiDatePicker CreatePlatformView()
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

#if IOS
public partial class BorderlessDatePickerHandler : DatePickerHandler
{
    protected override MauiDatePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.BorderStyle = UITextBorderStyle.None;

        return nativeView;
    }

}
#endif

#if MACCATALYST
public partial class BorderlessDatePickerHandler : DatePickerHandler
{
    protected override UIDatePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.Alpha = 0f;

        return nativeView;
    }
}
#endif

#if WINDOWS
public partial class BorderlessDatePickerHandler : DatePickerHandler
{
    protected override CalendarDatePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();
        nativeView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        return nativeView;
    }
}
#endif