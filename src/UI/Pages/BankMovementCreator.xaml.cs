using Binnaculum.Core;
using Binnaculum.Core.Utilities;

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
            .CatchCoreError(filePath => Core.UI.Creator.SaveBankIconChange(filePath, _account.Bank.Id))
            .ObserveOn(UiThread)
            .Subscribe(filePath =>
            {
                Icon.ImagePath = filePath;
            }).DisposeWith(Disposables);

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