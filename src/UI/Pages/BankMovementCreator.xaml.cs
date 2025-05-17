using Binnaculum.Core;
using Binnaculum.Core.Utilities;
using Binnaculum.Popups;

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
            .Select(x =>
            {
                SaveBankIcon(x.FilePath);
                return x.FilePath;
            })
            .ObserveOn(UiThread)
            .Subscribe(x =>
            {
                Icon.ImagePath = x;
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

    private Task SaveBankIcon(string filePath)
    {
        return Task.Run(async () =>
        {
            try
            {
                await Core.UI.Creator.SaveBankIconChange(filePath, _account.Bank.Id);
            }
            catch (AggregateException agEx)
            {
                // F# async exceptions are often wrapped in AggregateException
                var innerException = agEx.InnerException ?? agEx;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", innerException.Message, "Ok");
                });
            }
            catch (Exception ex)
            {
                // Regular exception handling
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var errorMessage = LocalizationResourceManager.Instance["Error_Changing_Icon"];
                    var popup = new MarkdownMessagePopup
                    {
                        Text = $"{errorMessage}\n\n```\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n```"
                    };
                    popup.Show();
                });
            }
        });
    }
}