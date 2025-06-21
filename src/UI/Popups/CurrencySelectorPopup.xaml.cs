using Binnaculum.Core;
using Binnaculum.Core.UI;
using System.Reactive.Subjects;

namespace Binnaculum.Popups;

public partial class CurrencySelectorPopup
{
    private ReadOnlyObservableCollection<Models.Currency> _currencies;
    public ReadOnlyObservableCollection<Models.Currency> Currencies => _currencies;

    private readonly IObservable<Func<Models.Currency, bool>> _filterPredicate;
    private readonly BehaviorSubject<string> _searchTerms = new BehaviorSubject<string>(string.Empty);
    
    public CurrencySelectorPopup()
	{
		InitializeComponent();

        ApplyHeightPercentage(this, 0.5, true);

        // Create a filter predicate that will update when search terms change
        _filterPredicate = _searchTerms
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(BuildFilterPredicate)
            .ObserveOn(UiThread);

        // Connect the currency cache with sorting and filtering
        Collections.Currencies.Connect()
            .Filter(_filterPredicate)
            .Sort(SortExpressionComparer<Core.Models.Currency>.Ascending(c =>
                c.Code == "USD" ? 0 :
                c.Code == "EUR" ? 1 :
                c.Code == "GBP" ? 2 : 3)
                .ThenByAscending(c => c.Code))            
            .ObserveOn(UiThread)
            .Bind(out _currencies)
            .Subscribe()
            .DisposeWith(Disposables);

        CurrencyList.ItemsSource = Currencies;

        Discard.Events().DiscardClicked
            .Subscribe(_ =>
            {
                Close();
            })
            .DisposeWith(Disposables);

        // Connect the searcher's text changed events to the filter
        Searcher.Events().TextChanged
            .Subscribe(e => _searchTerms.OnNext(e.NewTextValue))
            .DisposeWith(Disposables);
    }

    // Build a filter predicate based on search term
    private Func<Core.Models.Currency, bool> BuildFilterPredicate(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _ => true; // Show all items when search is empty

        searchTerm = searchTerm.Trim().ToLowerInvariant();

        return currency =>
            (currency.Title?.ToLowerInvariant().Contains(searchTerm) ?? false) ||
            (currency.Code?.ToLowerInvariant().Contains(searchTerm) ?? false);
    }

    private void SelectableCurrencyControl_CurrencySelected(object sender, Models.Currency e)
    {
        Close(e);
    }
}