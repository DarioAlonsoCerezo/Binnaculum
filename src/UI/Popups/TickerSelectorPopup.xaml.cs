using Binnaculum.Core;
using Binnaculum.Core.Utilities;
using Binnaculum.Extensions;
using Markdig.Helpers;
using Microsoft.FSharp.Core;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace Binnaculum.Popups;

public partial class TickerSelectorPopup
{
    private ReadOnlyObservableCollection<Models.Ticker> _tickers;
    public ReadOnlyObservableCollection<Models.Ticker> Tickers => _tickers;

    private readonly IObservable<Func<Models.Ticker, bool>> _filterPredicate;
    private readonly BehaviorSubject<string> _searchTerms = new(string.Empty);
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
            .Subscribe(_ =>
            {                
                if (TickerEditorContainer.Opacity < 1.0 && _tickers.Count == 0)
                    ChangeContainerOpacity();
            })
            .DisposeWith(Disposables);

        TickerList.ItemsSource = Tickers;
        
        // Search bar text
        Searcher.Events().TextChanged
            .Subscribe(e =>
            {
                _searchTerms.OnNext(e.NewTextValue);
                TickerSymbolEntry.Text = e.NewTextValue.Trim();
            })
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

        TickerIcon.Events().IconClicked
            .Select(_ => (string)LocalizationResourceManager.Instance[ResourceKeys.FilePicker_Select_Image])
            .SelectMany(async title => await FilePickerService.pickImageAsync(title))
            .Where(x => x.Success)
            .Select(x => x.FilePath)
            .ObserveOn(UiThread)
            .Subscribe(x =>
            {
                TickerIcon.ImagePath = x;
            }).DisposeWith(Disposables);

        // Enable save when entry has text
        Observable
            .Merge(TickerSymbolEntry.Events().TextChanged, 
                    TickerNameEntry.Events().TextChanged)
            .Where(_ => Tickers.Count == 0)
            .Select(x => !string.IsNullOrWhiteSpace(x.NewTextValue))
            .ObserveOn(UiThread)
            .BindTo(SaveOrDiscard, x => x.IsButtonSaveEnabled)
            .DisposeWith(Disposables);

        // Update placeholder text for Icon when symbol entry changes
        TickerSymbolEntry.Events().TextChanged
            .Do(e => TickerIcon.PlaceholderText = e.NewTextValue.Trim())
            .Subscribe()
            .DisposeWith(Disposables);

        // Save new ticker by symbol using F# Creator
        SaveOrDiscard.Events().SaveClicked
            .Select(_ => BuildTicker())
            .CatchCoreError(Core.UI.Creator.SaveTicker)
            .Subscribe(Close)
            .DisposeWith(Disposables);
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

    private Task ChangeContainerOpacity()
    {
        return Task.Run(async () =>
        {
            await Task.Delay(300); // Allow UI to update before changing opacity
            await Application.Current!.Dispatcher.DispatchAsync(() =>
            {
                TickerEditorContainer.Opacity = 1.0;
            });
        });
    }

    private Models.Ticker BuildTicker()
    {
        var symbol = TickerSymbolEntry.Text.Trim().ToUpperInvariant();
        var name = string.IsNullOrWhiteSpace(TickerNameEntry.Text)
            ? FSharpOption<string>.None
            : FSharpOption<string>.Some(TickerNameEntry.Text.Trim());
        var imagePath = string.IsNullOrWhiteSpace(TickerIcon.ImagePath)
            ? FSharpOption<string>.None
            : FSharpOption<string>.Some(TickerIcon.ImagePath);
        return new Models.Ticker(0, symbol, imagePath, name);
    }
}
