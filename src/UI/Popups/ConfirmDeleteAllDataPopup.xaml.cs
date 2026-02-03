namespace Binnaculum.Popups;

public partial class ConfirmDeleteAllDataPopup
{
    private bool _confirmed = false;

    public ConfirmDeleteAllDataPopup()
    {
        InitializeComponent();

        ApplyHeightPercentage(this, 0.45);

        CancelButton.Events().Clicked
            .Subscribe(_ =>
            {
                _confirmed = false;
                Close(false);
            }).DisposeWith(Disposables);

        ConfirmButton.Events().Clicked
            .Subscribe(_ =>
            {
                _confirmed = true;
                Close(true);
            }).DisposeWith(Disposables);
    }
}
