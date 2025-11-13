using Binnaculum.Core;
using System.Reactive.Subjects;

namespace Binnaculum.Pages;

public partial class OverviewPage
{
    private double _minMargin = 0;
    private IDisposable? _animateHistoryMarginDisposable;
    private bool _hiding;

    private ReadOnlyObservableCollection<Models.OverviewSnapshot> _snapshots;
    public ReadOnlyObservableCollection<Models.OverviewSnapshot> Snapshots => _snapshots;

    private ReadOnlyObservableCollection<Models.Movement> _movements;
    public ReadOnlyObservableCollection<Models.Movement> Movements => _movements;

    IObservable<Func<Models.Movement, bool>> _filterPredicate;
    BehaviorSubject<Models.OverviewSnapshot?> _selected = new(null);

    public OverviewPage()
    {
		InitializeComponent();
        SetupHistory();

        Core.UI.Collections.Snapshots.Connect()
            .ObserveOn(UiThread)
            .Bind(out _snapshots)
            .Subscribe();

        _filterPredicate = _selected
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(BuildFilterPredicate)
            .ObserveOn(UiThread);

        Core.UI.Collections.Movements.Connect()
            .Filter(_filterPredicate)
            .Sort(SortExpressionComparer<Models.Movement>.Descending(m => m.TimeStamp))
            .ObserveOn(UiThread)
            .Bind(out _movements)
            .Subscribe();

        Core.UI.Collections.Movements.Connect()
            .ObserveOn(UiThread)
            .Select(changes => changes.Any(change =>
                change.Reason == ListChangeReason.Add &&
                change.Item.Current.Type != Models.AccountMovementType.EmptyMovement))
            .DistinctUntilChanged()
            .BindTo(MovementSearcher, x => x.IsVisible)
            .DisposeWith(Disposables);

        Core.UI.SavedPrefereces.UserPreferences
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x.AllowCreateAccount)
            .BindTo(AddAccount, x => x.IsVisible);

        AccountsCarousel.ItemsSource = Snapshots;
        MovementsCollectionView.ItemsSource = Movements;
    }

    protected override void StartLoad()
    {
        AddAccount.Events().AddClicked
            .ObserveOn(UiThread)
            .Select(async _ =>
            {
                await Navigation.PushModalAsync(new AccountCreatorPage());
            })
            .Subscribe()
            .DisposeWith(Disposables);

        var data = Core.UI.Overview.Data;

        //Here we initialize the database
        data.Where(x => !x.IsDatabaseInitialized)
            .Subscribe(_ => InitDatabase()).DisposeWith(Disposables);

        //Here we load the data from the database
        data.Where(x => !x.TransactionsLoaded && x.IsDatabaseInitialized)
            .Subscribe(_ => LoadData()).DisposeWith(Disposables);

        data.Where(x => x.IsDatabaseInitialized)
            .Throttle(TimeSpan.FromMilliseconds(300), UiThread)
            .Subscribe(x => CarouselIndicator.IsVisible = false)
            .DisposeWith(Disposables);

        data.Where(x => x.TransactionsLoaded)
            .Throttle(TimeSpan.FromMilliseconds(300), UiThread)
            .Subscribe(x => CollectionIndicator.IsVisible = false)
            .DisposeWith(Disposables);

        AccountsCarousel.Events().CurrentItemChanged
            .Select(x => x.CurrentItem)
            .Subscribe(x =>
            {
                if(x is Core.Models.OverviewSnapshot snapshot)
                {
                    _selected.OnNext(snapshot);
                }
            })
            .DisposeWith(Disposables);
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

    private Func<Models.Movement, bool> BuildFilterPredicate(Models.OverviewSnapshot? selected)
    {
        if (selected == null)
            return _ => true;

        if (selected.Type.IsBankAccount)
        {
            return x =>
            {
                if (x.Type.IsBankAccountMovement)
                    return x.BankAccountMovement.Value.BankAccount.Id.Equals(selected.Bank.Value.Bank.Id);

                return false;
            };
        }


        return x =>
        {
            if (x.Type.IsBankAccountMovement)
                return false;

            if(x.Type.IsBrokerMovement)
                return x.BrokerMovement.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);
            
            if(x.Type.IsTrade)
                return x.Trade.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if(x.Type.IsDividend)
                return x.Dividend.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if(x.Type.IsDividendDate)
                return x.DividendDate.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if(x.Type.IsDividendTax)
                return x.DividendTax.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if(x.Type.IsOptionTrade)
                return x.OptionTrade.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            return false;
        };
    }

    private void InitDatabase()
    {
        Task.Run(async () =>
        {
            try
            {
                await Core.UI.Overview.InitDatabase();
            }
            catch (AggregateException agEx)
            {
                // F# async exceptions are often wrapped in AggregateException
                var innerException = agEx.InnerException ?? agEx;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", innerException.Message, "Ok");
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", ex.Message, "Ok");
                });
            }
        });
    }

    private void LoadData()
    {
        Task.Run(async () =>
        {
            try
            {
                await Core.UI.Overview.LoadData();
            }
            catch (AggregateException agEx)
            {
                // F# async exceptions are often wrapped in AggregateException
                var innerException = agEx.InnerException ?? agEx;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", innerException.Message, "Ok");
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlertAsync("Error", ex.Message, "Ok");
                });
            }
        });
    }
}