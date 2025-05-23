﻿using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

#if IOS || MACCATALYST
using UIKit;
#endif

#if ANDROID
using Android.Graphics.Drawables;
using Microsoft.Maui.Controls.Platform;
#endif

namespace Binnaculum.Handlers;

public partial class BorderlessTimePickerHandler : TimePickerHandler
{
    public BorderlessTimePickerHandler()
    {
        
    }
}

#if ANDROID
public partial class BorderlessTimePickerHandler : TimePickerHandler
{
    protected override MauiTimePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        using (var gradientDrawable = new GradientDrawable())
        {
            gradientDrawable.SetColor(global::Android.Graphics.Color.Transparent);
            nativeView.SetBackground(gradientDrawable);
        }

        nativeView.Gravity = Android.Views.GravityFlags.CenterHorizontal;

        return nativeView;
    }
}
#endif

#if IOS
public partial class BorderlessTimePickerHandler : TimePickerHandler
{
    protected override MauiTimePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.BorderStyle = UIKit.UITextBorderStyle.None;

        nativeView.TextAlignment = UITextAlignment.Center;

        return nativeView;
    }
}
#endif

#if MACCATALYST
public partial class BorderlessTimePickerHandler : TimePickerHandler
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
public partial class BorderlessTimePickerHandler : TimePickerHandler
{
    protected override Microsoft.UI.Xaml.Controls.TimePicker CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

        return nativeView;
    }
}
#endif