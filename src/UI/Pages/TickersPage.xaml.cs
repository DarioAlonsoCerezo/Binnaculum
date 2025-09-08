using Binnaculum.Core;

namespace Binnaculum.Pages;

public partial class TickersPage
{
    private ReadOnlyObservableCollection<Models.TickerSnapshot> _tickers;
    public ReadOnlyObservableCollection<Models.TickerSnapshot> Tickers => _tickers;
    
    public TickersPage()
	{
		InitializeComponent();

        Core.UI.Collections.TickerSnapshots.Connect()
            .Sort(SortExpressionComparer<Models.TickerSnapshot>.Ascending(t => t.Ticker.Symbol))
            .ObserveOn(UiThread)
            .Bind(out _tickers)
            .Subscribe();

        TickersCollectionView.ItemsSource = Tickers;
    }

    protected override void StartLoad()
    {
        
    }
}