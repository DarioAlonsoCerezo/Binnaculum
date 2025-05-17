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
        Observable
            .Merge(
                Add.Events().AddClicked.Select(_ => Unit.Default),
                AddMovementContainerGesture.Events().Tapped.Select(_ => Unit.Default),
                AddMovementTextGesture.Events().Tapped.Select(_ => Unit.Default))
            .Select(async _ =>
            {
                await Navigation.PushModalAsync(new BankMovementCreator(_bankAccount!));
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

        AddMovementContainer.VerticalOptions = _account!.HasMovements
            ? LayoutOptions.End
            : LayoutOptions.Center;

        AddMovementContainer.HorizontalOptions = _account!.HasMovements
            ? LayoutOptions.Start
            : LayoutOptions.Center;

        Add.Scale = _account!.HasMovements ? 0.6 : 1;
        AddMovementContainer.Spacing = _account!.HasMovements ? 0 : 12;
    }
}