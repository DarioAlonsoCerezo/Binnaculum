namespace Binnaculum;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        SentrySdk.CaptureMessage("Hello Sentry");
    }
}