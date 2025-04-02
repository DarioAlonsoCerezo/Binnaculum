namespace Binnaculum.Controls;

public partial class AddButton
{
    public Action? AddAction { get; set; }
    
    public AddButton()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        AddTap.Events().Tapped
            .Do(_ => AddAction?.Invoke())
            .Subscribe().DisposeWith(Disposables);
    }
}