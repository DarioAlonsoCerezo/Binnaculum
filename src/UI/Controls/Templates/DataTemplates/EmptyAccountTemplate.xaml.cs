using Binnaculum.Pages;

namespace Binnaculum.Controls;

public partial class EmptyAccountTemplate
{
	public EmptyAccountTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        AddTap.Events().Tapped
            .ObserveOn(UiThread)
            .SelectMany(async _ =>
            {
                await Navigation.PushModalAsync(new AccountCreatorPage());
                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }
}