using Microsoft.Extensions.Logging;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner;

/// <summary>
/// Main Visual Test Runner application entry point.
/// </summary>
public partial class VisualTestRunnerApp : Application
{
    private readonly ILogger<VisualTestRunnerApp>? _logger;

    public VisualTestRunnerApp()
    {
        InitializeComponent();
        
        // Try to get logger from service provider if available
        try
        {
            _logger = Handler?.MauiContext?.Services?.GetService<ILogger<VisualTestRunnerApp>>();
        }
        catch
        {
            // Logger not available, continue without it
        }
        
        _logger?.LogInformation("Visual Test Runner App initialized");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new VisualTestRunnerShell())
        {
            Title = "Visual Test Runner"
        };
        
        _logger?.LogInformation("Visual Test Runner window created");
        return window;
    }

    protected override void OnStart()
    {
        base.OnStart();
        _logger?.LogInformation("Visual Test Runner App started");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _logger?.LogInformation("Visual Test Runner App sleeping");
    }

    protected override void OnResume()
    {
        base.OnResume();
        _logger?.LogInformation("Visual Test Runner App resumed");
    }
}