using Binnaculum.Pages;

namespace Binnaculum.Controls;

public partial class BankAccountTemplate
{
    private Core.Models.Account? _account;
    private Core.Models.BankAccount? _bankAccount;
    private Core.Models.Bank? _bank;

    public BankAccountTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        Add.Events().AddClicked
            .CombineLatest(
                AddMovementContainerGesture.Events().Tapped,
                AddMovementTextGesture.Events().Tapped)
            .Do(_ =>
            {
                //TODO: Navigate to movement creator page for this account
            })
            .Subscribe()
            .DisposeWith(Disposables);

        BankAccountGesture.Events().Tapped
            .Select(async _ =>
            {
                await Navigation.PushModalAsync(new BankAccountPage());
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Core.Models.Account account)
        {
            _account = account;
            _bankAccount = account.Bank.Value;
            _bank = account.Bank.Value?.Bank;

            SetupValues();            
        }
    }

    private void SetupValues()
    {
        if (_bank?.Image != null)
            Icon.ImagePath = _bank!.Image.Value;
        else
            Icon.ImagePath = "bank";

        AccountNumber.Text = _bankAccount?.Name;

        AddMovementContainer.IsVisible = !_account!.HasMovements;
    }
}