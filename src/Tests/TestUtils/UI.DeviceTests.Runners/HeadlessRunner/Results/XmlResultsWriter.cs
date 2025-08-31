using System.Xml;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;

/// <summary>
/// Writes test results in xUnit XML format for CI/CD integration.
/// </summary>
public class XmlResultsWriter : IResultsWriter
{
    public async Task WriteResultsAsync(TestExecutionResults results, string? outputPath = null)
    {
        var xmlOutput = GenerateXunitXml(results);
        
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, xmlOutput);
        }
        else
        {
            Console.WriteLine(xmlOutput);
        }
    }

    private static string GenerateXunitXml(TestExecutionResults results)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n"
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);

        xmlWriter.WriteStartElement("testsuites");
        
        // Group tests by assembly/class for better organization
        var testsByAssembly = results.Results
            .GroupBy(r => GetAssemblyName(r.TestName))
            .ToList();

        var totalDuration = results.Results.Sum(r => r.Duration.TotalSeconds);
        
        xmlWriter.WriteAttributeString("tests", results.TotalCount.ToString());
        xmlWriter.WriteAttributeString("failures", results.FailedCount.ToString());
        xmlWriter.WriteAttributeString("skipped", results.SkippedCount.ToString());
        xmlWriter.WriteAttributeString("time", totalDuration.ToString("F3"));
        xmlWriter.WriteAttributeString("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));

        foreach (var assemblyGroup in testsByAssembly)
        {
            WriteTestSuite(xmlWriter, assemblyGroup.Key, assemblyGroup.ToList());
        }

        xmlWriter.WriteEndElement(); // testsuites
        xmlWriter.Flush();

        return stringWriter.ToString();
    }

    private static void WriteTestSuite(XmlWriter xmlWriter, string assemblyName, List<TestExecutionResult> tests)
    {
        xmlWriter.WriteStartElement("testsuite");
        
        var suiteDuration = tests.Sum(t => t.Duration.TotalSeconds);
        var suiteFailures = tests.Count(t => t.Status == TestCaseStatus.Failed);
        var suiteSkipped = tests.Count(t => t.Status == TestCaseStatus.Skipped);
        
        xmlWriter.WriteAttributeString("name", assemblyName);
        xmlWriter.WriteAttributeString("tests", tests.Count.ToString());
        xmlWriter.WriteAttributeString("failures", suiteFailures.ToString());
        xmlWriter.WriteAttributeString("skipped", suiteSkipped.ToString());
        xmlWriter.WriteAttributeString("time", suiteDuration.ToString("F3"));
        xmlWriter.WriteAttributeString("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));

        foreach (var test in tests.OrderBy(t => t.TestName))
        {
            WriteTestCase(xmlWriter, test);
        }

        xmlWriter.WriteEndElement(); // testsuite
    }

    private static void WriteTestCase(XmlWriter xmlWriter, TestExecutionResult test)
    {
        xmlWriter.WriteStartElement("testcase");
        
        var (className, methodName) = SplitTestName(test.TestName);
        
        xmlWriter.WriteAttributeString("classname", className);
        xmlWriter.WriteAttributeString("name", methodName);
        xmlWriter.WriteAttributeString("time", test.Duration.TotalSeconds.ToString("F3"));

        switch (test.Status)
        {
            case TestCaseStatus.Failed:
                xmlWriter.WriteStartElement("failure");
                xmlWriter.WriteAttributeString("message", test.ErrorMessage ?? "Test failed");
                if (!string.IsNullOrWhiteSpace(test.StackTrace))
                {
                    xmlWriter.WriteString(test.StackTrace);
                }
                xmlWriter.WriteEndElement(); // failure
                break;

            case TestCaseStatus.Skipped:
                xmlWriter.WriteStartElement("skipped");
                xmlWriter.WriteAttributeString("message", "Test skipped");
                xmlWriter.WriteEndElement(); // skipped
                break;
        }

        xmlWriter.WriteEndElement(); // testcase
    }

    private static string GetAssemblyName(string fullTestName)
    {
        // Extract assembly name from full test name
        // Format is typically: AssemblyName.ClassName.MethodName
        var parts = fullTestName.Split('.');
        return parts.Length > 2 ? parts[0] : "UnknownAssembly";
    }

    private static (string ClassName, string MethodName) SplitTestName(string fullTestName)
    {
        var lastDotIndex = fullTestName.LastIndexOf('.');
        if (lastDotIndex > 0 && lastDotIndex < fullTestName.Length - 1)
        {
            var className = fullTestName[..lastDotIndex];
            var methodName = fullTestName[(lastDotIndex + 1)..];
            return (className, methodName);
        }
        
        return (fullTestName, "UnknownMethod");
    }
}