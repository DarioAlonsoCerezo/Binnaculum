namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner;

/// <summary>
/// Main navigation shell for the Visual Test Runner.
/// Provides tabbed access to Test Discovery, Execution, and Results.
/// </summary>
public partial class VisualTestRunnerShell : Shell
{
    public VisualTestRunnerShell()
    {
        InitializeComponent();
        
        // Register navigation routes for programmatic navigation
        Routing.RegisterRoute("testdiscovery", typeof(Pages.TestDiscoveryPage));
        Routing.RegisterRoute("testexecution", typeof(Pages.TestExecutionPage));
        Routing.RegisterRoute("testresults", typeof(Pages.TestResultsPage));
    }
}