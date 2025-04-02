namespace Binnaculum.Pages;

public abstract class BasePage : ContentPage, IDisposable
{
    protected CompositeDisposable Disposables { get; }
    protected IScheduler UiThread;
    protected IScheduler BackgroundScheduler;
    protected AppTheme CurrentTheme { get; private set; }

    protected BasePage()
    {
        Disposables = new CompositeDisposable();
        while (SynchronizationContext.Current == null)
            Thread.Sleep(10);

        UiThread = new SynchronizationContextScheduler(SynchronizationContext.Current);
        BackgroundScheduler = new NewThreadScheduler(t => new Thread(t) { IsBackground = true });

        this.Events().ParentChanging
            .Where(x => x is { OldParent: null, NewParent: not null })
            .ObserveOn(UiThread)
            .Subscribe(_ => StartLoad()).DisposeWith(Disposables);

        this.Events().ParentChanging
            .Where(x => x is { OldParent: not null, NewParent: null })
            .Subscribe(_ => Dispose()).DisposeWith(Disposables);

        CurrentTheme = Application.Current!.RequestedTheme;
        Application.Current.RequestedThemeChanged += Current_RequestedThemeChanged;
    }

    private void Current_RequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ThemeChanged(e.RequestedTheme);
    }

    protected abstract void StartLoad();

    protected virtual void ThemeChanged(AppTheme theme)
    {
        CurrentTheme = theme;
    }

    public void Dispose()
    {
        Application.Current!.RequestedThemeChanged -= Current_RequestedThemeChanged;
        Disposables?.Dispose();
    }
}