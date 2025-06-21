namespace Binnaculum.Extensions;

using Binnaculum.Popups;
using System;
using System.Threading.Tasks;

public static class RxExtensions
{
    /// <summary>
    /// Logs debug information for each element in the observable sequence.
    /// </summary>
    public static IObservable<T> LogWhileDebug<T>(this IObservable<T> source, string log)
    {
        return source.Do(_ =>
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {log}]");
#endif
        });
    }

    /// <summary>
    /// Executes a task and catches any exceptions that might occur.
    /// </summary>
    public static IObservable<T> CatchCoreError<T>(this IObservable<T> source, Func<Task> task, bool informUser = false)
    {
        Task.Run(async () =>
        {
            try
            {
                await task.Invoke();
            }
            catch (AggregateException agEx)
            {
                var innerException = agEx.InnerException ?? agEx;
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {innerException.Message}]");

#if DEBUG
                // In DEBUG mode, always show the error
                await ShowErrorPopup(innerException);
#else
                // In Release mode, only show if informUser is true
                if (informUser)
                {
                    await ShowErrorPopup(innerException);
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {ex.Message}]");

#if DEBUG
                // In DEBUG mode, always show the error
                await ShowErrorPopup(ex);
#else
                // In Release mode, only show if informUser is true
                if (informUser)
                {
                    await ShowErrorPopup(ex);
                }
#endif
            }
        });
        return source;
    }

    /// <summary>
    /// Executes a task with the source value as input and catches any exceptions that might occur.
    /// </summary>
    public static IObservable<T> CatchCoreError<T>(this IObservable<T> source, Func<T, Task> task, bool informUser = false)
    {
        return source.Do(value => 
        {
            Task.Run(async () =>
            {
                try
                {
                    await task.Invoke(value);
                }
                catch (AggregateException agEx)
                {
                    var innerException = agEx.InnerException ?? agEx;
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {innerException.Message}]");

#if DEBUG
                    // In DEBUG mode, always show the error
                    await ShowErrorPopup(innerException);
#else
                    // In Release mode, only show if informUser is true
                    if (informUser)
                    {
                        await ShowErrorPopup(innerException);
                    }
#endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {ex.Message}]");

#if DEBUG
                    // In DEBUG mode, always show the error
                    await ShowErrorPopup(ex);
#else
                    // In Release mode, only show if informUser is true
                    if (informUser)
                    {
                        await ShowErrorPopup(ex);
                    }
#endif
                }
            });
        });
    }

    /// <summary>
    /// Shows an error popup with exception details.
    /// </summary>
    private static async Task ShowErrorPopup(Exception exception)
    {
        if (Application.Current != null &&
            Application.Current.Windows.Count > 0)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
            {
                await Task.Delay(0);
                var errorMessage = FormatExceptionMessage(exception);
                var popup = new MarkdownMessagePopup
                {
                    Text = errorMessage
                };
                var mainWindow = Application.Current.Windows[0];
                if (mainWindow.Page is Shell shell)
                {
                    // Use the extension method from PopupExtensions
                    PopupExtensions.Show(popup);
                }
            });
        }
    }

    /// <summary>
    /// Formats an exception message for display in the markdown viewer.
    /// </summary>
    private static string FormatExceptionMessage(Exception exception)
    {
        string safeStackTrace = exception.StackTrace ?? "No stack trace available";
        
        return "# Error\n" +
               "**Message:** " + exception.Message + "\n\n" +
               "**Type:** " + exception.GetType().Name + "\n\n" +
               "**Stack Trace:**\n" +
               "```\n" + safeStackTrace + "\n```";
    }
}