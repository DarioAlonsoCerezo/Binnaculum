using Binnaculum.Core;

namespace Binnaculum.Controls
{
    public partial class SelectableTickerControl
    {
        private Models.Ticker _ticker;
        public event EventHandler<Models.Ticker> TickerSelected;

        public SelectableTickerControl()
        {
            InitializeComponent();
        }

        protected override void StartLoad()
        {
            
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext is Models.Ticker ticker)
            {
                _ticker = ticker;
                Disposables?.Clear();
                SetupData();
            }
        }

        private void SetupData()
        {
            Icon.PlaceholderText = _ticker.Symbol?.Trim() ?? string.Empty;
            Icon.ImagePath = _ticker.Image?.Value ?? string.Empty;
            TickerSymbol.Text = _ticker.Symbol;
            // Convert F# option<string> to string
            TickerName.Text = _ticker.Name?.Value ?? string.Empty;

            TickerGesture.Events().Tapped
            .Subscribe(_ => TickerSelected?.Invoke(this, _ticker))
            .DisposeWith(Disposables);
        }
    }
}
