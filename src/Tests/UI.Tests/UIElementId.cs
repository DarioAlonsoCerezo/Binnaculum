namespace UI.Tests;

public static class UIElementId
{
    public static class Shell
    {
        //com.darioalonso.binnacle:id/navigation_bar_item_large_label_view
        public const string Overview = "navigation_bar_item_large_label_view";
        public const string Tickers = "navigation_bar_item_large_label_view";
        public const string Settings = "navigation_bar_item_large_label_view";
    }

    public static class Page
    {
        public static class Overview
        {
            public const string Title = "OverviewTitle";
            public const string CarouselIndicator = "OverviewCarouselIndicator";
            public const string CollectionIndicator = "OverviewCollectionIndicator";
            public const string EmptyMovementCollectionText = "EmptyMovementCollectionText";
            public const string EmptyAccountText = "EmptyAccountText";
        }
    }

    public static class Templates
    {
        public static class Ticker
        {
            public const string TickerTemplate = "TickerTemplate";
            public const string TickerName = "TickerTemplateName";
        }
    }
}
