﻿using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

#if ANDROID
using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls.Platform;
#endif

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Notifications;
#endif

namespace Binnaculum.Handlers;

public partial class BorderlessEntryHandler : EntryHandler
{
    public BorderlessEntryHandler()
    {
    }

    public BorderlessEntryHandler(IPropertyMapper? mapper = null) : base(mapper)
    {
    }
}

#if ANDROID
public partial class BorderlessEntryHandler : EntryHandler
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
public partial class BorderlessEntryHandler  : EntryHandler
{
    protected override MauiTextField CreatePlatformView()
    {
        var nativeView = base.CreatePlatformView();

        nativeView.BorderStyle = UIKit.UITextBorderStyle.None;

        return nativeView;
    }
}

#endif

#if WINDOWS
public partial class BorderlessEntryHandler : EntryHandler
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