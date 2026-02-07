namespace Tests

open System
open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Import.IBKRModels
open Binnaculum.Core.Import.IBKRStatementParser
open Binnaculum.Core.Import.IBKRSectionFilter

/// <summary>
/// Comprehensive tests for IBKR import system
/// Tests parsing, data conversion, and privacy compliance
/// </summary>
[<TestClass>]
type IBKRImportTests() =

    let testDataPath =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "TestData", "IBKR_Samples")


    /// <summary>
    /// Test privacy compliance with edge cases
    /// </summary>
    [<TestMethod>]
    member this.``Should handle edge cases and maintain privacy compliance``() =
        let filePath = Path.Combine(testDataPath, "ibkr_edge_cases_sample.csv")
        Assert.IsTrue(File.Exists(filePath), "Test file not found: " + filePath)

        let result = parseCsvFile filePath

        Assert.IsTrue(result.Success, "Parse should succeed. Errors: " + String.Join("; ", result.Errors))
        Assert.IsTrue(result.SkippedSections.Length > 0, "Should skip sensitive sections")

        // Verify sensitive sections were skipped
        let skippedReasons = result.SkippedSections

        Assert.IsTrue(skippedReasons |> List.exists (fun s -> s.Contains("Privacy")), "Should skip privacy-sensitive sections")

    /// <summary>
    /// Test section classification for privacy compliance
    /// </summary>
    [<TestMethod>]
    member this.``Should classify sections correctly for privacy compliance``() =
        // Test sensitive sections are skipped
        let accountInfoSection = classifySection "Account Information"

        match accountInfoSection with
        | SkippedSection reason ->
            Assert.IsTrue(reason.Contains("Privacy"), "Account Information should be skipped for privacy")
        | _ -> Assert.Fail("Account Information should be classified as skipped")

        // Test parsable sections are allowed
        let tradesSection = classifySection "Trades"
        Assert.IsTrue(shouldProcessSection tradesSection, "Trades section should be processed")

        let depositsSection = classifySection "Deposits & Withdrawals"
        Assert.IsTrue(shouldProcessSection depositsSection, "Deposits section should be processed")


    /// <summary>
    /// Test error handling with malformed CSV
    /// </summary>
    [<TestMethod>]
    member this.``Should handle malformed CSV gracefully``() =
        let malformedCsv =
            @"Invalid,CSV,Structure
This,is,not,a,valid,IBKR,statement
Missing,required,fields"

        let result = parseCsvContent malformedCsv

        // Should not crash, may succeed with empty data or fail gracefully
        Assert.IsNotNull(result, "Result should not be null")

        if not result.Success then
            Assert.IsTrue(result.Errors.Length > 0, "Should report parsing errors")

    /// <summary>
    /// Test privacy validation
    /// </summary>
    [<TestMethod>]
    member this.``Should validate privacy compliance``() =
        let testData = createEmptyStatementData ()
        let validationErrors = validatePrivacyCompliance testData

        // Empty data should pass privacy validation
        Assert.AreEqual(0, validationErrors.Length, "Empty data should have no privacy violations")
