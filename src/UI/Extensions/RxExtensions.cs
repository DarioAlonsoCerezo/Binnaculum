namespace Binnaculum.Extensions;

public static class RxExtensions
{
    public static IObservable<T> LogWhileDebug<T>(this IObservable<T> source, string log)
    {
        return source.Do(_ =>
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:hh-mm-ss:fff} - {log}]");
#endif
        });
    }
}