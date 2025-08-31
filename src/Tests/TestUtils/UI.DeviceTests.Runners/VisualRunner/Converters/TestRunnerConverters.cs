using System.Globalization;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.Converters;

/// <summary>
/// Converter to invert boolean values.
/// </summary>
public class InvertedBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

/// <summary>
/// Converter for test status to color mapping.
/// </summary>
public class TestStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TestCaseStatus status)
        {
            return status switch
            {
                TestCaseStatus.Passed => Application.Current?.Resources["TestPassedColor"] as Color ?? Colors.Green,
                TestCaseStatus.Failed => Application.Current?.Resources["TestFailedColor"] as Color ?? Colors.Red,
                TestCaseStatus.Running => Application.Current?.Resources["TestRunningColor"] as Color ?? Colors.Orange,
                TestCaseStatus.Skipped => Application.Current?.Resources["TestSkippedColor"] as Color ?? Colors.Blue,
                _ => Application.Current?.Resources["TestPendingColor"] as Color ?? Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converter for expand/collapse button text.
/// </summary>
public class ExpandCollapseTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
            return isExpanded ? "▼" : "▶";
        return "▶";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converter for integer to boolean (true if not equal to parameter).
/// </summary>
public class IntToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramString && int.TryParse(paramString, out int paramInt))
        {
            return intValue != paramInt;
        }
        return value is int val && val > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converter to count selected tests.
/// </summary>
public class SelectedTestCountConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TestRunnerViewModel viewModel)
        {
            var selectedCount = viewModel.TestAssemblies
                .SelectMany(a => a.TestClasses)
                .SelectMany(c => c.TestCases)
                .Count(tc => tc.IsSelected);
            return selectedCount.ToString();
        }
        return "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}