namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for applications that support screenshot capabilities.
/// Provides screenshot functionality as part of the UITest framework.
/// </summary>
public interface IScreenshotSupportedApp
{
    /// <summary>
    /// Take a screenshot of the current app state.
    /// </summary>
    /// <returns>Screenshot data as byte array</returns>
    byte[] Screenshot();

    /// <summary>
    /// Take a screenshot and save it to the specified path.
    /// </summary>
    /// <param name="filePath">Path where to save the screenshot</param>
    void SaveScreenshot(string filePath);
}