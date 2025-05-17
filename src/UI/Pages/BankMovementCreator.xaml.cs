using Binnaculum.Core;

namespace Binnaculum.Pages;

public partial class BankMovementCreator
{
    private readonly Models.BankAccount _account;

	public BankMovementCreator(Models.BankAccount account)
	{
		InitializeComponent();

        _account = account;
        Icon.ImagePath = _account.Bank.Image.Value;
        AccountName.Text = _account.Name;
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }
}