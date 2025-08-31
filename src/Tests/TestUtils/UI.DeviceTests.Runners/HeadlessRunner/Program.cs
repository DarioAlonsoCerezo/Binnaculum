using System.CommandLine;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner;

/// <summary>
/// Console application entry point for the Binnaculum Headless Test Runner.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the headless test runner console application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Create and execute the command line interface
            var rootCommand = ArgumentParser.CreateRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled error: {ex.Message}");
            if (args.Contains("--verbosity") && args.Contains("detailed"))
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }
}