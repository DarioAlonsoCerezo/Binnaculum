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

        var broker = this.WhenAnyValue(x => x.Broker)
            .WhereNotNull();

        broker.Select(x =>
            {
                if (x.Image != null)
                {
                    return x.Image!;
                }
                return null;
            })
            .WhereNotNull()
            .ObserveOn(UiThread)
            .BindTo(BrokerImage, x => x.ImagePath)
            .DisposeWith(Disposables);

        broker.ObserveOn(UiThread)
            .Select(x =>
            {
                if(x.Id < 0)
                {
                    BrokerAdd.IsVisible = true;
                    BrokerName.SetLocalizedText(x.Name);
                    BrokerImage.IsVisible = false;
                    return null;
                }
                return x.Name;
            })
            .WhereNotNull()
            .BindTo(BrokerName, x => x.Text)
            .DisposeWith(Disposables);

        BrokerAdd.Events().AddClicked
            .Subscribe(_ =>
            {
                BrokerSelected?.Invoke(this, Broker);
            }).DisposeWith(Disposables);
    }
}
