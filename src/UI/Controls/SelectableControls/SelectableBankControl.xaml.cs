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

        var bank = this.WhenAnyValue(x => x.Bank)
            .WhereNotNull();

        bank.Select(x =>
            {
                if (x.Image != null)
                    return x.Image.Value;

                return null;
            })
            .WhereNotNull()
            .ObserveOn(UiThread)
            .BindTo(BankImage, x => x.ImagePath)
            .DisposeWith(Disposables);

        bank.ObserveOn(UiThread)
            .Select(x =>
            {
                if(x.Id < 0)
                {
                    BankName.SetLocalizedText(x.Name);
                    return null;
                }
                return x.Name;
            })
            .WhereNotNull()
            .BindTo(BankName, x => x.Text)
            .DisposeWith(Disposables);
    }
}