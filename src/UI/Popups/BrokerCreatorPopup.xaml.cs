using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Core.Utilities;

namespace Binnaculum.Popups;

public partial class BrokerCreatorPopup
{
	public BrokerCreatorPopup()
	{
		InitializeComponent();

        var events = SaveOrDiscard.Events();

        events.DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        BrokerNameEntry.Events().TextChanged
            .Select(x => !string.IsNullOrWhiteSpace(x.NewTextValue) && x.NewTextValue.Length > 2)
            .ObserveOn(UiThread)
            .BindTo(SaveOrDiscard, x => x.IsButtonSaveEnabled)
            .DisposeWith(Disposables);

        events.SaveClicked
            .SelectMany(async _ =>
            {
                await Creator.SaveBroker(BrokerNameEntry.Text, Icon.ImagePath);
                return Unit.Default;
            })
            .Subscribe(_ =>
            {
                Close();
            })
            .DisposeWith(Disposables);

        Container.WidthRequest = DeviceDisplay.MainDisplayInfo.Width;

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