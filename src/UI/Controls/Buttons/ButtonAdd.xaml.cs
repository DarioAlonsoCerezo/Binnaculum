namespace Binnaculum.Controls;

public partial class ButtonAdd
{
    public Action? AddAction { get; set; }
    
    public ButtonAdd()
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