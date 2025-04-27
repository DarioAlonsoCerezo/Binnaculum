namespace Binnaculum.Controls;

public partial class SelectableBrokerControl
{
    public event EventHandler<Core.Models.Broker>? BrokerSelected;

    public static readonly BindableProperty BrokerProperty = BindableProperty.Create(
        nameof(Broker),
        typeof(Core.Models.Broker),
        typeof(SelectableBrokerControl),
        default(Core.Models.Broker));

    public Core.Models.Broker Broker
    {
        get => (Core.Models.Broker)GetValue(BrokerProperty);
        set => SetValue(BrokerProperty, value);
    }

    public SelectableBrokerControl()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {
        ContentGesture.Events().Tapped
            .Where(_ => Broker != null)
            .Subscribe(_ =>
            {
                BrokerSelected?.Invoke(this, Broker);
            }).DisposeWith(Disposables);

        this.WhenAnyValue(x => x.Broker)
            .WhereNotNull()
            .Select(x => x.Image)
            .ObserveOn(UiThread)
            .BindTo(BrokerImage, x => x.ImagePath)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.Broker)
            .WhereNotNull()
            .Select(x => x.Name)
            .ObserveOn(UiThread)
            .BindTo(BrokerName, x => x.Text)
            .DisposeWith(Disposables);
    }
}
