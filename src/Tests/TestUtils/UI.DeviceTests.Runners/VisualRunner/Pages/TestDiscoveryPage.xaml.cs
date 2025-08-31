using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.Pages;

/// <summary>
/// Page for discovering, browsing, and selecting tests to run.
/// </summary>
public partial class TestDiscoveryPage : ContentPage
{
    public TestDiscoveryPage()
    {
        InitializeComponent();
        
        // Initialize the view model
        BindingContext = new TestRunnerViewModel();
        
        // Auto-discover tests when page loads
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        if (BindingContext is TestRunnerViewModel viewModel)
        {
            await viewModel.RefreshTestsAsync();
        }
    }
}