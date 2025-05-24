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
            TickerGesture.Events().Tapped
                .Subscribe(_ => TickerSelected?.Invoke(this, _ticker))
                .DisposeWith(Disposables);
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext is Models.Ticker ticker)
            {
                _ticker = ticker;
                TickerSymbol.Text = ticker.Symbol;
                // Convert F# option<string> to string
                TickerName.Text = ticker.Name?.Value ?? string.Empty;
            }
        }
    }
}
