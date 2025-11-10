namespace Tests

open System
open NUnit.Framework
open Binnaculum.Core.Import

/// <summary>
/// Tests for ImportStatus to CurrentImportStatus conversion
/// Ensures C#-friendly types correctly represent F# discriminated union states
/// </summary>
[<TestFixture>]
type ImportStatusConversionTests() =

    /// <summary>
    /// Test conversion of NotStarted state
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts NotStarted correctly``() =
        let status = NotStarted
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.NotStarted))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of Validating state with file path
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts Validating correctly``() =
        let filePath = "/test/path/file.csv"
        let status = Validating filePath
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.Validating))
        Assert.That(converted.FilePath, Is.EqualTo(Some filePath))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of ProcessingFile state with file name and progress
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts ProcessingFile correctly``() =
        let fileName = "test.csv"
        let progress = 0.75
        let status = ProcessingFile(fileName, progress)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.ProcessingFile))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(Some fileName))
        Assert.That(converted.Progress, Is.EqualTo(Some progress))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of ProcessingData state with record counts
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts ProcessingData correctly``() =
        let processed = 50
        let total = 100
        let status = ProcessingData(processed, total)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.ProcessingData))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(Some processed))
        Assert.That(converted.TotalRecords, Is.EqualTo(Some total))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of SavingToDatabase state with message and progress
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts SavingToDatabase correctly``() =
        let message = "Saving records to database..."
        let progress = 0.9
        let processed = 90
        let total = 100
        let status = SavingToDatabase(message, progress, processed, total)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.SavingToDatabase))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(Some progress))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(Some processed))
        Assert.That(converted.TotalRecords, Is.EqualTo(Some total))
        Assert.That(converted.Message, Is.EqualTo(Some message))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of Completed state with import result
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts Completed correctly``() =
        let result =
            ImportResult.createSuccess
                1
                100
                { Trades = 50
                  BrokerMovements = 30
                  Dividends = 20
                  OptionTrades = 0
                  NewTickers = 5 }
                []
                1000L

        let status = Completed result
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.Completed))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(Some result))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of Cancelled state with cancellation reason
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts Cancelled correctly``() =
        let reason = "User cancelled import"
        let status = Cancelled reason
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.Cancelled))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(Some reason))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(None))

    /// <summary>
    /// Test conversion of Failed state with error message
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus.fromImportStatus converts Failed correctly``() =
        let error = "Import failed due to invalid data"
        let status = Failed error
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.That(converted.State, Is.EqualTo(ImportStateEnum.Failed))
        Assert.That(converted.FilePath, Is.EqualTo(None))
        Assert.That(converted.FileName, Is.EqualTo(None))
        Assert.That(converted.Progress, Is.EqualTo(None))
        Assert.That(converted.RecordsProcessed, Is.EqualTo(None))
        Assert.That(converted.TotalRecords, Is.EqualTo(None))
        Assert.That(converted.Message, Is.EqualTo(None))
        Assert.That(converted.Result, Is.EqualTo(None))
        Assert.That(converted.Error, Is.EqualTo(Some error))

    /// <summary>
    /// Test that all ImportStateEnum values map to correct integer values
    /// </summary>
    [<Test>]
    member this.``ImportStateEnum has correct integer values``() =
        Assert.That(int ImportStateEnum.NotStarted, Is.EqualTo(0))
        Assert.That(int ImportStateEnum.Validating, Is.EqualTo(1))
        Assert.That(int ImportStateEnum.ProcessingFile, Is.EqualTo(2))
        Assert.That(int ImportStateEnum.ProcessingData, Is.EqualTo(3))
        Assert.That(int ImportStateEnum.SavingToDatabase, Is.EqualTo(4))
        Assert.That(int ImportStateEnum.Completed, Is.EqualTo(5))
        Assert.That(int ImportStateEnum.Cancelled, Is.EqualTo(6))
        Assert.That(int ImportStateEnum.Failed, Is.EqualTo(7))

    /// <summary>
    /// Test that conversion preserves all data for all ImportStatus cases
    /// Ensures no data loss during conversion to C#-friendly format
    /// </summary>
    [<Test>]
    member this.``CurrentImportStatus conversion preserves all data``() =
        let testCases =
            [ (NotStarted, ImportStateEnum.NotStarted)
              (Validating "test.csv", ImportStateEnum.Validating)
              (ProcessingFile("file.csv", 0.5), ImportStateEnum.ProcessingFile)
              (ProcessingData(50, 100), ImportStateEnum.ProcessingData)
              (SavingToDatabase("Saving...", 0.8, 80, 100), ImportStateEnum.SavingToDatabase)
              (Cancelled "User cancelled", ImportStateEnum.Cancelled)
              (Failed "Error occurred", ImportStateEnum.Failed) ]

        for (duStatus, expectedState) in testCases do
            let converted = CurrentImportStatus.fromImportStatus duStatus
            Assert.That(converted.State, Is.EqualTo(expectedState))
