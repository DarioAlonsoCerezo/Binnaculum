namespace Binnaculum.Controls;

public abstract class BaseContentView : ContentView, IDisposable
{
    protected CompositeDisposable Disposables { get; }
    protected IScheduler UiThread;
    protected IScheduler BackgroundScheduler;
    protected AppTheme CurrentTheme { get; private set; }

    /// This is used to prevent multiple calls to StartLoad when the view is loaded.
    /// This can happen when we use this view as a Datatemplate or inside it and we are moving
    /// between different elements in a list.
    private bool _startLoadedExecuted { get; set; }

    protected BaseContentView()
    {
        Disposables = new CompositeDisposable();
        while (SynchronizationContext.Current == null)
            Thread.Sleep(10);

        UiThread = new SynchronizationContextScheduler(SynchronizationContext.Current);
        BackgroundScheduler = new NewThreadScheduler(t => new Thread(t) { IsBackground = true });

        this.Events().Loaded
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Where(_ => !_startLoadedExecuted)
            .Do(_ => StartLoad())
            .Do(_ => _startLoadedExecuted = true)
            .ObserveOn(UiThread)
            .Subscribe().DisposeWith(Disposables);

        this.Events().ParentChanging
            .Where(x => x.OldParent != null && x.NewParent == null)
            .Subscribe(_ => Dispose()).DisposeWith(Disposables);

        CurrentTheme = Application.Current!.RequestedTheme;

        Application.Current.RequestedThemeChanged += RequestedThemeChanged;
    }

    private void RequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ThemeChanged(e.RequestedTheme);
    }

    protected virtual void ThemeChanged(AppTheme theme)
    {
        CurrentTheme = theme;
    }

    protected abstract void StartLoad();

    public void Dispose()
    {
        Application.Current!.RequestedThemeChanged -= RequestedThemeChanged;
        Disposables?.Dispose();
    }
}
