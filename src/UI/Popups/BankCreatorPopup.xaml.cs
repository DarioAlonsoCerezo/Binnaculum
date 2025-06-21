
using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Core.Utilities;
using Microsoft.FSharp.Core;

namespace Binnaculum.Popups;

public partial class BankCreatorPopup
{
	public BankCreatorPopup()
	{
		InitializeComponent();

        ForceFillWidth();

        var events = SaveOrDiscard.Events();

        events.DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        BankNameEntry.Events().TextChanged
            .Select(x => !string.IsNullOrWhiteSpace(x.NewTextValue) && x.NewTextValue.Length > 2)
            .ObserveOn(UiThread)
            .BindTo(SaveOrDiscard, x => x.IsButtonSaveEnabled)
            .DisposeWith(Disposables);

        events.SaveClicked.
            Select(_ => new Models.Bank(0, 
                BankNameEntry.Text, 
                new FSharpOption<string>(Icon.ImagePath),
                DateTime.Now))
            .CatchCoreError(Creator.SaveBank)
            .Subscribe(_ =>
            {
                Close();
            })
            .DisposeWith(Disposables);

        Icon.Events().IconClicked
            .Select(_ => (string)LocalizationResourceManager.Instance[ResourceKeys.FilePicker_Select_Image])
            .SelectMany(async title => await FilePickerService.pickImageAsync(title))
            .Where(x => x.Success)
            .Select(x => x.FilePath)
            .ObserveOn(UiThread)
            .Subscribe(x =>
            {
                Icon.ImagePath = x;
            }).DisposeWith(Disposables);
    }
}