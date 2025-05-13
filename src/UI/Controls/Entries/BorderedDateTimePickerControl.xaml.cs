namespace Binnaculum.Controls;

public partial class BorderedDateTimePickerControl
{
    public event EventHandler<DateTime> DateSelected;

    public static readonly BindableProperty HideTimeSelectorProperty =
        BindableProperty.Create(
            nameof(HideTimeSelector), 
            typeof(bool), 
            typeof(BorderedDateTimePickerControl), 
            false,
            propertyChanged: (bindable, old, newValue) =>
            {
                if(bindable is BorderedDateTimePickerControl control && newValue is bool hide)
                {
                    control.TimeControl.IsVisible = !hide;
                    control.Container.SetColumnSpan(control.DateControl, hide ? 2 : 1);
                }
            });

    public bool HideTimeSelector
    {
        get => (bool)GetValue(HideTimeSelectorProperty);
        set => SetValue(HideTimeSelectorProperty, value);
    }

    public DateTime Date { get; private set; }

    public BorderedDateTimePickerControl()
	{
		InitializeComponent();

        DateControl.Date = DateTime.Now;
        TimeControl.Time = DateTime.Now.TimeOfDay;
    }

    protected override void StartLoad()
    {
        DateControl.Events().DateSelected
            .Subscribe(_ =>
            {
                Date = DateControl.Date;
                if(TimeControl.IsVisible)
                    Date = Date.Add(TimeControl.Time);

                DateSelected?.Invoke(this, Date);
            })
            .DisposeWith(Disposables);

        TimeControl.Events().TimeSelected
            .Subscribe(_ =>
            {
                Date = Date.Add(TimeControl.Time);

                DateSelected?.Invoke(this, Date);
            })
            .DisposeWith(Disposables);
    }
}