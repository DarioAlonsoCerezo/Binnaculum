namespace Binnaculum.Extensions;

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {ex.Message}]");
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {ex.Message}]");
                }
            });
        });
    }
}