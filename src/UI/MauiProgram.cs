using Binnaculum.Controls;
using Binnaculum.Handlers;
using Indiko.Maui.Controls.Markdown;

namespace Binnaculum;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMarkdownView()
            .UseSentry(options =>
            {
                // Sentry DSN is injected at compile time via BuildConfig
                options.Dsn = BuildConfig.SentryDsn;
                options.Debug = true;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
                fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<BorderlessDatePicker, BorderlessDatePickerHandler>();
                handlers.AddHandler<BorderlessEditor, BorderlessEditorHandler>();
                handlers.AddHandler<BorderlessEntry, BorderlessEntryHandler>();
                handlers.AddHandler<BorderlessPicker, BorderlessPickerHandler>();
                handlers.AddHandler<BorderlessTimePicker, BorderlessTimePickerHandler>();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}