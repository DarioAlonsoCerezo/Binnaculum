namespace Tests

open System
open System.IO
open NUnit.Framework
open Binnaculum.Core.Import.IBKRModels
open Binnaculum.Core.Import.IBKRStatementParser
open Binnaculum.Core.Import.IBKRSectionFilter

/// <summary>
/// Comprehensive tests for IBKR import system
/// Tests parsing, data conversion, and privacy compliance
/// </summary>
[<TestFixture>]
type IBKRImportTests() =

    let testDataPath = Path.Combine(__SOURCE_DIRECTORY__, "TestData", "IBKR_Samples")


    /// <summary>
    /// Test privacy compliance with edge cases
    /// </summary>
    [<Test>]
    member this.``Should handle edge cases and maintain privacy compliance``() =
        let filePath = Path.Combine(testDataPath, "ibkr_edge_cases_sample.csv")
        Assert.That(File.Exists(filePath), Is.True, "Test file not found: " + filePath)

        let result = parseCsvFile filePath

        Assert.That(result.Success, Is.True, "Parse should succeed. Errors: " + String.Join("; ", result.Errors))
        Assert.That(result.SkippedSections.Length, Is.GreaterThan(0), "Should skip sensitive sections")

        // Verify sensitive sections were skipped
        let skippedReasons = result.SkippedSections

        Assert.That(
            skippedReasons |> List.exists (fun s -> s.Contains("Privacy")),
            Is.True,
            "Should skip privacy-sensitive sections"
        )

    /// <summary>
    /// Test section classification for privacy compliance
    /// </summary>
    [<Test>]
    member this.``Should classify sections correctly for privacy compliance``() =
        // Test sensitive sections are skipped
        let accountInfoSection = classifySection "Account Information"

        match accountInfoSection with
        | SkippedSection reason ->
            Assert.That(reason.Contains("Privacy"), Is.True, "Account Information should be skipped for privacy")
        | _ -> Assert.Fail("Account Information should be classified as skipped")

        // Test parsable sections are allowed
        let tradesSection = classifySection "Trades"
        Assert.That(shouldProcessSection tradesSection, Is.True, "Trades section should be processed")

        let depositsSection = classifySection "Deposits & Withdrawals"
        Assert.That(shouldProcessSection depositsSection, Is.True, "Deposits section should be processed")


    /// <summary>
    /// Test error handling with malformed CSV
    /// </summary>
    [<Test>]
    member this.``Should handle malformed CSV gracefully``() =
        let malformedCsv =
            @"Invalid,CSV,Structure
This,is,not,a,valid,IBKR,statement
Missing,required,fields"

        let result = parseCsvContent malformedCsv

        // Should not crash, may succeed with empty data or fail gracefully
        Assert.That(result, Is.Not.Null, "Result should not be null")

        if not result.Success then
            Assert.That(result.Errors.Length, Is.GreaterThan(0), "Should report parsing errors")

    /// <summary>
    /// Test privacy validation
    /// </summary>
    [<Test>]
    member this.``Should validate privacy compliance``() =
        let testData = createEmptyStatementData ()
        let validationErrors = validatePrivacyCompliance testData

        // Empty data should pass privacy validation
        Assert.That(validationErrors.Length, Is.EqualTo(0), "Empty data should have no privacy violations")
