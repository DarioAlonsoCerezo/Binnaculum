namespace Binnaculum
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Application.Current!.UserAppTheme = (AppTheme)Preferences.Get("AppTheme", (int)AppTheme.Unspecified);
            return new Window(new AppShell());
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }
    }
}