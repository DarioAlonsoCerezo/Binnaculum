using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Core.Utilities;
using Microsoft.FSharp.Core;
using System.Reactive.Linq;

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
        BalanceRadioButton.IsChecked = true;
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        Icon.Events().IconClicked
            .Select(_ => (string)LocalizationResourceManager.Instance[ResourceKeys.FilePicker_Select_Image])
            .SelectMany(async title => await FilePickerService.pickImageAsync(title))
            .Where(x => x.Success)
            .Select(x => x.FilePath)
            .ObserveOn(UiThread)
            .Select(filePath =>
            {
                Icon.ImagePath = filePath;
                return new Models.Bank(_account.Bank.Id,
                    _account.Bank.Name,
                    new FSharpOption<string>(filePath),
                    _account.Bank.CreatedAt);
            })
            .CatchCoreError(Creator.SaveBank)
            .Subscribe()
            .DisposeWith(Disposables);

        BalanceRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Select(_ => MovementType.Balance)
            .Do(SetupSelection)
            .Subscribe()
            .DisposeWith(Disposables);

        InterestRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Select(_ => MovementType.Interest)
            .Do(SetupSelection)
            .Subscribe()
            .DisposeWith(Disposables);

        FeesRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Select(_ => MovementType.Fees)
            .Do(SetupSelection)
            .Subscribe()
            .DisposeWith(Disposables);

        Deposit.Events().DepositChanged
            .Select(x => x.Amount > 0)
            .ObserveOn(UiThread)
            .BindTo(Save, x => x.IsEnabled)
            .DisposeWith(Disposables);

        Save.Events().SaveClicked
            .Select(_ =>
            {
                var currency = Collections.GetCurrency(Deposit.DepositData.Currency);

                return new Models.BankAccountMovement(
                    0, 
                    Deposit.DepositData.TimeStamp,
                    Deposit.DepositData.Amount,
                    _account,
                    _account.Currency,
                    GetMovementType());
            })
            .CatchCoreError(Creator.SaveBankMovement)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private Models.BankAccountMovementType GetMovementType()
    {
        if(BalanceRadioButton.IsChecked == true)
            return Models.BankAccountMovementType.Balance;

        if(InterestRadioButton.IsChecked == true)
            return Models.BankAccountMovementType.Interest;

        return Models.BankAccountMovementType.Fee;
    }

    private void SetupSelection(MovementType selection)
    {
        BalanceRadioButton.IsChecked = selection == MovementType.Balance;
        InterestRadioButton.IsChecked = selection == MovementType.Interest;
        FeesRadioButton.IsChecked = selection == MovementType.Fees;
    }

    enum MovementType
    {
        Balance,
        Interest,
        Fees
    }
}