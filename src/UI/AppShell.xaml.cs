namespace Binnaculum;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        try
        {
            ForcedException();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void ForcedException() => throw new Exception("This is a forced exception to check the integration is working");
}