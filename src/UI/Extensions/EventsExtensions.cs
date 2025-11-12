using Binnaculum.Controls;
using System.Runtime.CompilerServices;

namespace Binnaculum.Extensions;

public static class EventsExtensions
{
    public static RxBindableObjectEvents Events(this BindableObject data) => new(data);
    public static RxTapGestureRecognizerEvents Events(this TapGestureRecognizer data) => new(data);
    public static RxPanGestureRecognizerEvents Events(this PanGestureRecognizer data) => new(data);
    public static RxEntryEvents Events(this Entry item) => new(item);
    public static RxEditorEvents Events(this Editor item) => new(item);
    public static RxInputViewEvents Events(this InputView item) => new(item);
    public static RxImageEvents Events(this Image item) => new(item);
    public static RxCollectionViewEvents Events(this CollectionView item) => new(item);
    public static RxWebViewEvents Events(this WebView item) => new(item);
    public static RxDatePickerEvents Events(this DatePicker item) => new(item);
    public static RxTimePickerEvents Events(this TimePicker item) => new(item);
    public static RxVisualElementEvents Events(this VisualElement item) => new(item);
    public static RxButtonEvents Events(this Button item) => new(item);
    public static RxPageEvents Events(this Page item) => new(item);
    public static RxNavigationPageEvents Events(this NavigationPage item) => new(item);
    public static RxSliderEvents Events(this Slider item) => new(item);
    public static RxStepperEvents Events(this Stepper item) => new(item);
    public static RxSwipeViewEvents Events(this SwipeView item) => new(item);
    public static RxSwipeItemEvents Events(this SwipeItem item) => new(item);
    public static RxPickerEvents Events(this Picker item) => new(item);
    public static RxRefreshViewEvents Events(this RefreshView item) => new(item);
    public static RxRadioButtonEvents Events(this RadioButton item) => new(item);
    public static RxSelectableBrokerEvents Events(this SelectableBrokerControl item) => new(item);
    public static RxSelectableBankEvents Events(this SelectableBankControl item) => new(item);
    public static RxSelectableItemControlEvents Events(this SelectableItemControl item) => new(item);
    public static RxButtonAddEvents Events(this ButtonAdd item) => new(item);
    public static RxButtonSaveEvents Events(this ButtonSave item) => new(item);
    public static RxButtonDiscardEvents Events(this ButtonDiscard item) => new(item);
    public static RxButtonAddOrDiscardEvents Events(this ButtonSaveOrDiscard item) => new(item);
    public static RxCarouselViewEvents Events(this CarouselView item) => new(item);
    public static RxBorderedConversionControlEvents Events(this BorderedConversionControl item) => new(item);
    public static RxBorderedEntry Events(this BorderedEntry item) => new(item);
    public static RxBorderedFeeAndCommissionControlEvents Events(this BorderedFeeAndCommissionControl item) => new(item);
    public static RxBorderedDateTimePickerControlEvents Events(this BorderedDateTimePickerControl item) => new(item);
    public static RxExpanderEvents Events(this Expander item) => new(item);
    public static RxIconControlEvents Events(this IconControl item) => new(item);
    public static RxEditableIconControlEvents Events(this EditableIconControl item) => new(item);
    public static RxCustomRadioButtonEvents Events(this CustomRadioButton item) => new(item);
    public static RxCustomSwitchEvents Events(this CustomSwitch item) => new(item);
    public static RxSelectableCurrencyEvents Events(this SelectableCurrencyControl item) => new(item);
    public static RxItemSelectorControlEvents Events(this ItemSelectorControl item) => new(item);
    public static RxBrokerMovementControlEvents Events(this BrokerMovementControl item) => new(item);
    public static RxBankMovementControlEvents Events(this BankMovementControl item) => new(item);
    public static RxBorderedEditorEvents Events(this BorderedEditor item) => new(item);
    public static RxTradeControlEvents Events(this TradeControl item) => new(item);
    public static RxDividendReceivedControlEvents Events(this DividendControl item) => new(item);
    public static RxOptionTradeControlEvents Events(this OptionTradeControl item) => new(item);
}
