namespace Binnaculum.Controls;

public partial class SelectableBankControl
{
    public event EventHandler<Core.Models.Bank>? BankSelected;

    public static readonly BindableProperty BankProperty = BindableProperty.Create(
        nameof(Bank),
        typeof(Core.Models.Bank),
        typeof(SelectableBankControl),
        default(Core.Models.Bank));

    public Core.Models.Bank Bank
    {
        get => (Core.Models.Bank)GetValue(BankProperty);
        set => SetValue(BankProperty, value);
    }

    public SelectableBankControl()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        ContentGesture.Events().Tapped
            .Where(_ => Bank != null)
            .Subscribe(_ =>
            {
                BankSelected?.Invoke(this, Bank);
            }).DisposeWith(Disposables);

        this.WhenAnyValue(x => x.Bank)
            .WhereNotNull()
            .Select(x =>
            {
                if (x.Image != null)
                    return x.Image.Value;

                return null;
            })
            .WhereNotNull()
            .ObserveOn(UiThread)
            .BindTo(BankImage, x => x.ImagePath)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.Bank)
            .WhereNotNull()
            .Select(x => x.Name)
            .ObserveOn(UiThread)
            .BindTo(BankName, x => x.Text)
            .DisposeWith(Disposables);
    }
}