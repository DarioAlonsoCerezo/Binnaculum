---
applyTo: "src/UI/**/*.xaml"
---

# XAML UI Guidelines for Binnaculum

When working on XAML files, follow these Binnaculum-specific patterns:

## XAML Styling Standards
- Always use `XAMLStylerConfiguration.json` for formatting
- Follow existing attribute ordering: Layout ? Appearance ? Behavior ? Data
- Use existing ResourceDictionary patterns from `src/UI/Resources/`

## Investment UI Patterns
- Use `BrokerAccountTemplate` pattern for financial data display
- Implement responsive layouts for different screen sizes
- Follow existing color schemes for profit/loss indicators (green/red)

## Data Binding Patterns  
- Use `{Binding}` with proper `BindingContext` setup
- Implement `INotifyPropertyChanged` for reactive UI updates
- Use `Observable` chains for complex user interactions
- Always dispose subscriptions with `DisposeWith(Disposables)`

## Navigation Patterns
- Use `Navigation.PushModalAsync()` for modal dialogs
- Implement proper back navigation handling
- Use `Shell.Current.GoToAsync()` for app navigation

## Accessibility
- Add `AutomationId` for testability  
- Include `SemanticProperties.Description` for screen readers
- Ensure proper contrast ratios for financial data

## Performance
- Use `CollectionView` instead of `ListView` for better performance
- Implement virtualization for large investment portfolios
- Minimize binding converter complexity