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
            .Subscribe(_ =>
            {
                Navigation.PushModalAsync(new AccountCreatorPage());
            })
            .DisposeWith(Disposables);
    }
}