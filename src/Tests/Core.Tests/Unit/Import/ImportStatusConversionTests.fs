namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Import

/// <summary>
/// Tests for ImportStatus to CurrentImportStatus conversion
/// Ensures C#-friendly types correctly represent F# discriminated union states
/// </summary>
[<TestClass>]
type ImportStatusConversionTests() =

    /// <summary>
    /// Test conversion of NotStarted state
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts NotStarted correctly``() =
        let status = ImportStatus.NotStarted
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.NotStarted, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of Validating state with file path
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts Validating correctly``() =
        let filePath = "/test/path/file.csv"
        let status = ImportStatus.Validating filePath
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.Validating, converted.State)
        Assert.AreEqual(Some filePath, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of ProcessingFile state with file name and progress
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts ProcessingFile correctly``() =
        let fileName = "test.csv"
        let progress = 0.75
        let status = ImportStatus.ProcessingFile(fileName, progress)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.ProcessingFile, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(Some fileName, converted.FileName)
        Assert.AreEqual(Some progress, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of ProcessingData state with record counts
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts ProcessingData correctly``() =
        let processed = 50
        let total = 100
        let status = ImportStatus.ProcessingData(processed, total)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.ProcessingData, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(Some processed, converted.RecordsProcessed)
        Assert.AreEqual(Some total, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of SavingToDatabase state with message and progress
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts SavingToDatabase correctly``() =
        let message = "Saving records to database..."
        let progress = 0.9
        let processed = 90
        let total = 100
        let status = ImportStatus.SavingToDatabase(message, progress, processed, total)
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.SavingToDatabase, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(Some progress, converted.Progress)
        Assert.AreEqual(Some processed, converted.RecordsProcessed)
        Assert.AreEqual(Some total, converted.TotalRecords)
        Assert.AreEqual(Some message, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of Completed state with import result
    /// </summary>
    [<TestMethod>]
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

        let status = ImportStatus.Completed result
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.Completed, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(Some result, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of Cancelled state with cancellation reason
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts Cancelled correctly``() =
        let reason = "User cancelled import"
        let status = ImportStatus.Cancelled reason
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.Cancelled, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(Some reason, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(None, converted.Error)

    /// <summary>
    /// Test conversion of Failed state with error message
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus.fromImportStatus converts Failed correctly``() =
        let error = "Import failed due to invalid data"
        let status = ImportStatus.Failed error
        let converted = CurrentImportStatus.fromImportStatus status

        Assert.AreEqual(ImportStateEnum.Failed, converted.State)
        Assert.AreEqual(None, converted.FilePath)
        Assert.AreEqual(None, converted.FileName)
        Assert.AreEqual(None, converted.Progress)
        Assert.AreEqual(None, converted.RecordsProcessed)
        Assert.AreEqual(None, converted.TotalRecords)
        Assert.AreEqual(None, converted.Message)
        Assert.AreEqual(None, converted.Result)
        Assert.AreEqual(Some error, converted.Error)

    /// <summary>
    /// Test that all ImportStateEnum values map to correct integer values
    /// </summary>
    [<TestMethod>]
    member this.``ImportStateEnum has correct integer values``() =
        Assert.AreEqual(0, int ImportStateEnum.NotStarted)
        Assert.AreEqual(1, int ImportStateEnum.Validating)
        Assert.AreEqual(2, int ImportStateEnum.ProcessingFile)
        Assert.AreEqual(3, int ImportStateEnum.ProcessingData)
        Assert.AreEqual(4, int ImportStateEnum.SavingToDatabase)
        Assert.AreEqual(5, int ImportStateEnum.CalculatingSnapshots)
        Assert.AreEqual(6, int ImportStateEnum.Completed)
        Assert.AreEqual(7, int ImportStateEnum.Cancelled)
        Assert.AreEqual(8, int ImportStateEnum.Failed)

    /// <summary>
    /// Test that conversion preserves all data for all ImportStatus cases
    /// Ensures no data loss during conversion to C#-friendly format
    /// </summary>
    [<TestMethod>]
    member this.``CurrentImportStatus conversion preserves all data``() =
        let testCases =
            [ (ImportStatus.NotStarted, ImportStateEnum.NotStarted)
              (ImportStatus.Validating "test.csv", ImportStateEnum.Validating)
              (ImportStatus.ProcessingFile("file.csv", 0.5), ImportStateEnum.ProcessingFile)
              (ImportStatus.ProcessingData(50, 100), ImportStateEnum.ProcessingData)
              (ImportStatus.SavingToDatabase("Saving...", 0.8, 80, 100), ImportStateEnum.SavingToDatabase)
              (ImportStatus.CalculatingSnapshots(50, 100, "2025-11-10"), ImportStateEnum.CalculatingSnapshots)
              (ImportStatus.Cancelled "User cancelled", ImportStateEnum.Cancelled)
              (ImportStatus.Failed "Error occurred", ImportStateEnum.Failed) ]

        for (duStatus, expectedState) in testCases do
            let converted = CurrentImportStatus.fromImportStatus duStatus
            Assert.AreEqual(expectedState, converted.State)
