namespace Binnaculum;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Core.UI.Overview.Data.Where(x => !x.IsDatabaseInitialized)
            .Take(1)
            .CatchCoreError(Core.UI.Overview.InitCore)
            .Subscribe();
    }
}