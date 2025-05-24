using Binnaculum.Core;
using Markdig.Helpers;
using System.Reactive.Subjects;

namespace Binnaculum.Popups;

public partial class TickerSelectorPopup
{
    private ReadOnlyObservableCollection<Models.Ticker> _tickers;
    public ReadOnlyObservableCollection<Models.Ticker> Tickers => _tickers;

    private readonly IObservable<Func<Models.Ticker, bool>> _filterPredicate;
    private readonly BehaviorSubject<string> _searchTerms = new BehaviorSubject<string>(string.Empty);
    private readonly BehaviorSubject<bool> _addTickerEnabled = new(false);

    public TickerSelectorPopup()
    {
        InitializeComponent();
        ApplyHeightPercentage(Container, 0.5, true);

        var merged = Observable.Merge(_searchTerms, _addTickerEnabled.Select(_ => string.Empty));
            

        // Create filter predicate for search
        _filterPredicate = merged
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(BuildFilterPredicate)
            .ObserveOn(UiThread);

        // Connect tickers collection
        Core.UI.Collections.Tickers.Connect()
            .Filter(_filterPredicate)
            .Sort(SortExpressionComparer<Models.Ticker>.Ascending(t => t.Symbol))
            .ObserveOn(UiThread)
            .Bind(out _tickers)
            .Subscribe()
            .DisposeWith(Disposables);

        TickerList.ItemsSource = Tickers;

        // Search bar text
        Searcher.Events().TextChanged
            .Subscribe(e => _searchTerms.OnNext(e.NewTextValue))
            .DisposeWith(Disposables);

        // Discard
        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        AddTicker.Events().AddClicked
            .Do(_ => _addTickerEnabled.OnNext(true))
            .Select(_ => false)
            .ObserveOn(UiThread)
            .BindTo(AddTicker, x => x.IsVisible)
            .DisposeWith(Disposables);

        // Enable save when entry has text
        //NewTickerEntry.Events().TextChanged
        //    .Select(x => !string.IsNullOrWhiteSpace(x.NewTextValue))
        //    .ObserveOn(UiThread)
        //    .BindTo(SaveOrDiscard, x => x.IsButtonSaveEnabled)
        //    .DisposeWith(Disposables);

        // Save new ticker by symbol using F# Creator
        //SaveOrDiscard.Events().SaveClicked
        //    .SelectMany(async _ =>
        //    {
        //        var symbol = NewTickerEntry.Text.Trim();
        //        await  Creator.SaveTickerSymbol(symbol);
        //        return Collections.Tickers.Items.FirstOrDefault(t => t.Symbol == symbol);
        //    })
        //    .Where(saved => saved != null)
        //    .Subscribe(saved =>
        //    {
        //        NewTickerEntry.Text = string.Empty;
        //        Close(saved);
        //    })
        //    .DisposeWith(Disposables);
    }

    private Func<Models.Ticker, bool> BuildFilterPredicate(string term)
    {
        if (_addTickerEnabled.Value) return _ => false;
        if (string.IsNullOrWhiteSpace(term)) return _ => true;
        var upper = term.Trim().ToUpperInvariant();
        return t =>
            (t.Symbol?.ToUpperInvariant().Contains(upper) ?? false) ||
            (t.Name != null && t.Name.Value.ToUpperInvariant().Contains(upper));
    }

    private void SelectableTickerControl_TickerSelected(object sender, Models.Ticker ticker)
    {
        Close(ticker);
    }
}
