namespace Binnaculum.Popups;

public partial class ConfirmDeleteAllDataPopup
{
    public ConfirmDeleteAllDataPopup()
    {
        InitializeComponent();

        ApplyHeightPercentage(this, 0.45);

        CancelButton.Events().Clicked
            .Subscribe(_ =>
            {
                Close(false);
            }).DisposeWith(Disposables);

        ConfirmButton.Events().Clicked
            .Subscribe(_ =>
            {
                Close(true);
            }).DisposeWith(Disposables);
    }
}
