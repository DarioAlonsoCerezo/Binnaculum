using DynamicData;
using System.Collections.ObjectModel;

namespace Binnaculum.Pages;

public partial class OverviewPage
{
    private double _minMargin = 0;
    private IDisposable? _animateHistoryMarginDisposable;
    private bool _hiding;

    private ReadOnlyObservableCollection<Core.Models.Account> _accounts;
    public ReadOnlyObservableCollection<Core.Models.Account> Accounts => _accounts;

    private ReadOnlyObservableCollection<Core.Models.Movement> _movements;
    public ReadOnlyObservableCollection<Core.Models.Movement> Movements => _movements;

    public OverviewPage()
    {
		InitializeComponent();
        SetupHistory();

        Core.UI.Collections.Accounts.Connect()
            .ObserveOn(UiThread)
            .Bind(out _accounts)
            .Subscribe(x =>
            {

            });

        Core.UI.Collections.Movements.Connect()
            .ObserveOn(UiThread)
            .Bind(out _movements)
            .Subscribe(x =>
            {
            });

        AccountsCarousel.ItemsSource = Accounts;
        MovementsCollectionView.ItemsSource = Movements;
    }

    protected override void StartLoad()
    {
        AddAccount.AddAction = async () => await Navigation.PushModalAsync(new AccountCreatorPage());

        var data = Core.UI.Overview.Data;

        //Here we initialize the database
        data.Where(x => !x.IsDatabaseInitialized)
            .Subscribe(_ => Task.Run(Core.UI.Overview.InitDatabase)).DisposeWith(Disposables);

        //Here we load the data from the database
        data.Where(x => !x.TransactionsLoaded && x.IsDatabaseInitialized)
            .Subscribe(_ => Task.Run(Core.UI.Overview.LoadData)).DisposeWith(Disposables);

        
    }

    private void SetupHistory()
    {
        this.Events().SizeChanged
            .Do(_ =>
            {
                if (Height > 0)
                {
                    var fromTop = Height * 0.5;
                    if (fromTop > 0)
                    {
                        _minMargin = fromTop;
                        HistoryContainer.Margin = new Thickness(0, fromTop, 0, 0);
                    }
                }
            }).Subscribe();

        HistoryGestured.Events().PanUpdated
            .Where(x => !HistoryBackground.IsVisible)
            .Where(args => args.StatusType == GestureStatus.Running)
            .Do(args =>
            {
                var marginTop = HistoryContainer.Margin.Top + args.TotalY;
                if (marginTop < 0)
                    marginTop = 0;
                if (marginTop > _minMargin)
                    marginTop = _minMargin;

                HistoryContainer.Margin = new Thickness(0, marginTop, 0, 0);
            })
            .Subscribe();

        HistoryGestured.Events().PanUpdated
            .Where(x => HistoryBackground.IsVisible)
            .Where(args => args.StatusType == GestureStatus.Running)
            .Do(args =>
            {
                var marginTop = History.Margin.Top + args.TotalY;
                if (marginTop < 64)
                    marginTop = 64;
                if (marginTop > _minMargin)
                    marginTop = _minMargin;

                History.Margin = new Thickness(0, marginTop, 0, 0);
            })
            .Subscribe();

        HistoryGestured.Events().PanUpdated
            .Where(x => x.StatusType == GestureStatus.Completed)
            .Select(_ => GetPercentage(HistoryBackground.IsVisible
                ? History.Margin.Top
                : HistoryContainer.Margin.Top))
            .Subscribe(x =>
            {
                _animateHistoryMarginDisposable?.Dispose();

                _hiding = x > 50;
                _animateHistoryMarginDisposable = AnimateHistoryMaring();
            });
    }

    private double GetPercentage(double reference) => reference / (Height * 0.5) * 100;

    private IDisposable AnimateHistoryMaring() => Observable.Interval(TimeSpan.FromMilliseconds(16))
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(x =>
        {
            var shouldDispose = false;
            var containerMarginTop = HistoryContainer.Margin.Top;
            var historyMarginTop = History.Margin.Top;

            if (!_hiding)
            {
                HistoryBackground.IsVisible = true;
                containerMarginTop += 16;
                historyMarginTop += 16;
                if (containerMarginTop > 0)
                {
                    containerMarginTop = 0;
                    historyMarginTop = 64;
                    shouldDispose = true;
                }

                if (historyMarginTop > 64)
                    historyMarginTop = 64;
            }
            else
            {
                HistoryBackground.IsVisible = false;
                containerMarginTop -= 16;
                historyMarginTop -= 16;
                if (containerMarginTop < _minMargin)
                {
                    containerMarginTop = _minMargin;
                    historyMarginTop = 0;
                    shouldDispose = true;
                }                
                if(historyMarginTop < 0)
                    historyMarginTop = 0;
            }


            HistoryContainer.Margin = new Thickness(0, containerMarginTop, 0, 0);
            History.Margin = new Thickness(0, historyMarginTop, 0, 0);

            if (shouldDispose)
                _animateHistoryMarginDisposable?.Dispose();
        });

    
}