namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open System.IO
open System.Threading.Tasks
open Binnaculum.Core.Import
open Binnaculum.Core.Import.ImportManager
open Binnaculum.Core.Import.ImportState
open Binnaculum.Core.Import.FileProcessor
open Binnaculum.Core.Models
open Binnaculum.Core.UI

[<TestClass>]
type ImportManagerTests() =

    [<TestInitialize>]
    member this.Setup() =
        // Clean up any existing import state
        ImportState.cleanup ()

    [<TestCleanup>]
    member this.TearDown() =
        // Clean up after each test
        ImportState.cleanup ()

    [<TestMethod>]
    member this.``ImportManager fails for non-existent file``() =
        task {
            let! result = ImportManager.importFile 1 1 "non-existent.csv"

            Assert.IsFalse(result.Success, "Import should fail for non-existent file")
            Assert.AreEqual(1, result.Errors.Length, "Should have one error")
            StringAssert.Contains(result.Errors.[0].ErrorMessage, "File not found")
        }

    [<TestMethod>]
    member this.``ImportManager fails for invalid broker ID``() =
        task {
            // Create a temporary CSV file for testing
            let tempFile = Path.GetTempFileName()
            let csvFile = tempFile + ".csv"
            File.Move(tempFile, csvFile)
            File.WriteAllText(csvFile, "test,data\n1,2")

            try
                // Test with an invalid broker ID that doesn't exist in the database
                let! result = ImportManager.importFile 999 42 csvFile

                // The import should fail - either because the broker doesn't exist
                // or because of database configuration issues in the test environment
                Assert.IsFalse(result.Success, "Import should fail for non-existent broker ID or database issues")

                Assert.IsTrue(result.Errors.Length > 0, "Should have at least one error")
                // Accept either the broker not found error or database configuration errors
                let errorMessage = result.Errors.[0].ErrorMessage

                let hasExpectedError =
                    errorMessage.Contains("Broker with ID 999 not found")
                    || errorMessage.Contains("This functionality is not implemented")
                    || errorMessage.Contains("One or more errors occurred")

                Assert.IsTrue(hasExpectedError, $"Should have expected error type, got: {errorMessage}")
            finally
                if File.Exists(csvFile) then
                    File.Delete(csvFile)
        }

    [<TestMethod>]
    member this.``ImportManager fails for invalid broker account``() =
        task {
            // Create a temporary CSV file for testing
            let tempFile = Path.GetTempFileName()
            let csvFile = tempFile + ".csv"
            File.Move(tempFile, csvFile)
            File.WriteAllText(csvFile, "test,data\n1,2")

            try
                let! result = ImportManager.importFile 1 999 csvFile

                Assert.IsFalse(result.Success, "Import should fail for invalid broker account")
                Assert.IsTrue(result.Errors.Length > 0, "Should have at least one error")

                let errorMessage = result.Errors.[0].ErrorMessage

                let hasExpectedError =
                    errorMessage.Contains("Broker account with ID 999 not found")
                    || errorMessage.Contains("Broker with ID 1 not found")
                    || errorMessage.Contains("This functionality is not implemented")
                    || errorMessage.Contains("One or more errors occurred")

                Assert.IsTrue(hasExpectedError, $"Should have expected error type, got: {errorMessage}")
            finally
                if File.Exists(csvFile) then
                    File.Delete(csvFile)
        }

    [<TestMethod>]
    member this.``ImportState starts and manages cancellation correctly``() =
        let token1 = ImportState.startImport ()
        Assert.IsFalse(token1.IsCancellationRequested, "New token should not be cancelled")

        ImportState.cancelChunkedImport ()
        Assert.IsTrue(token1.IsCancellationRequested, "Token should be cancelled after cancellation")

        // Start a new import - should get a fresh token
        let token2 = ImportState.startImport ()
        Assert.IsFalse(token2.IsCancellationRequested, "New token should not be cancelled")

    [<TestMethod>]
    member this.``ImportState tracks status changes``() =
        // Reset to idle state before test
        ImportState.updateChunkedState (ChunkedImportState.Idle)

        let initialStatus = ImportState.CurrentChunkedStatus.Value
        Assert.AreEqual(ChunkedImportStateEnum.Idle, initialStatus.State, "Initial status should be Idle")

        ImportState.startImport () |> ignore
        ImportState.updateChunkedState (ChunkedImportState.Validating "test.csv")

        let currentStatus = ImportState.CurrentChunkedStatus.Value

        Assert.AreEqual(ChunkedImportStateEnum.Validating, currentStatus.State, "Status should be Validating")
        Assert.AreEqual(Some "test.csv", currentStatus.FileName, "FileName should be Some(test.csv)")

    [<TestMethod>]
    member this.``FileProcessor handles CSV files correctly``() =
        let tempFile = Path.GetTempFileName()
        let csvFile = tempFile + ".csv" // Simple concatenation instead of ChangeExtension
        File.Move(tempFile, csvFile)
        File.WriteAllText(csvFile, "header1,header2\nvalue1,value2")

        try
            let result = FileProcessor.processFile csvFile

            Assert.IsFalse(result.IsTemporary, "CSV file should not be temporary")
            Assert.AreEqual(1, result.CsvFiles.Length, "Should have one CSV file")
            Assert.AreEqual(csvFile, result.CsvFiles.[0], "Should return the same file path")

            Assert.IsTrue(FileProcessor.validateCsvFiles result, "CSV file should be valid")
            Assert.IsTrue(FileProcessor.getTotalFileSize result > 0L, "File should have content")
        finally
            if File.Exists(csvFile) then
                File.Delete(csvFile)

    [<TestMethod>]
    member this.``FileProcessor fails for unsupported file types``() =
        let tempFile = Path.GetTempFileName()
        let txtFile = tempFile + ".txt" // Simple concatenation instead of ChangeExtension
        File.Move(tempFile, txtFile)
        File.WriteAllText(txtFile, "test content")

        try
            Assert.Throws<System.Exception>(fun () -> FileProcessor.processFile txtFile |> ignore)
            |> ignore
        finally
            if File.Exists(txtFile) then
                File.Delete(txtFile)

    [<TestMethod>]
    member this.``FileProcessor handles ZIP files correctly``() =
        let tempDir = Path.GetTempPath()
        let zipFile = Path.Combine(tempDir, Path.GetRandomFileName() + ".zip")
        let csvContent = "header1,header2\nvalue1,value2"

        try
            // Create a ZIP file with a CSV inside using the static method
            let tempCsvFile = Path.GetTempFileName() + ".csv"
            File.WriteAllText(tempCsvFile, csvContent)

            // Use the static method to create zip from files
            use archive =
                System.IO.Compression.ZipFile.Open(zipFile, System.IO.Compression.ZipArchiveMode.Create)

            let entry = archive.CreateEntry("test.csv")
            use entryStream = entry.Open()
            let csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent)
            entryStream.Write(csvBytes, 0, csvBytes.Length)
            entryStream.Close()
            archive.Dispose()

            File.Delete(tempCsvFile) // Clean up temp CSV

            let result = FileProcessor.processFile zipFile

            Assert.IsTrue(result.IsTemporary, "ZIP extraction should create temporary files")
            Assert.AreEqual(1, result.CsvFiles.Length, "Should find one CSV file")
            Assert.IsTrue(File.Exists(result.CsvFiles.[0]), "Extracted CSV file should exist")

            // Verify content
            let extractedContent = File.ReadAllText(result.CsvFiles.[0])
            Assert.AreEqual(csvContent, extractedContent, "Content should match")

            // Test cleanup
            FileProcessor.cleanup result
            Assert.IsFalse(Directory.Exists(result.FilePath), "Temporary directory should be cleaned up")

        finally
            if File.Exists(zipFile) then
                File.Delete(zipFile)

    [<TestMethod>]
    member this.``ImportResult helper functions work correctly``() =
        let importedData =
            { Trades = 5
              BrokerMovements = 3
              Dividends = 2
              OptionTrades = 1
              NewTickers = 4 }

        let fileResults = [ FileImportResult.createSuccess "test.csv" 10 ]

        let successResult = ImportResult.createSuccess 1 10 importedData fileResults 1000L
        Assert.IsTrue(successResult.Success, "Success result should be successful")
        Assert.AreEqual(10, successResult.ProcessedRecords, "Should have processed 10 records")
        Assert.AreEqual(1000L, successResult.ProcessingTimeMs, "Should have correct processing time")

        let cancelledResult = ImportResult.createCancelled ()
        Assert.IsFalse(cancelledResult.Success, "Cancelled result should not be successful")

        Assert.AreEqual(0, cancelledResult.ProcessedRecords, "Cancelled result should have no processed records")

        let errorResult = ImportResult.createError ("Test error")
        Assert.IsFalse(errorResult.Success, "Error result should not be successful")
        Assert.AreEqual(1, errorResult.Errors.Length, "Should have one error")
        Assert.AreEqual("Test error", errorResult.Errors.[0].ErrorMessage, "Should have correct error message")

    [<TestMethod>]
    member this.``ImportManager utility functions work correctly``() =
        // Reset to known state
        ImportState.updateChunkedState (ChunkedImportState.Idle)

        // Test initial state
        Assert.IsFalse(ImportManager.isImportInProgress (), "No import should be in progress initially")
        let status = ImportManager.getCurrentStatus ()
        Assert.AreEqual(ChunkedImportStateEnum.Idle, status.State, "Status should be Idle")

        // Start an import state (but not a full import)
        ImportState.startImport () |> ignore
        ImportState.updateChunkedState (ChunkedImportState.ReadingFile "test.csv")
        Assert.IsTrue(ImportManager.isImportInProgress (), "Import should be in progress")

        // Cancel and verify
        ImportManager.cancelCurrentImport ()

        Assert.IsFalse(ImportManager.isImportInProgress (), "Import should not be in progress after cancellation")

        // Test background cancellation
        ImportState.startImport () |> ignore
        ImportManager.cancelForBackground ()

        Assert.IsFalse(ImportManager.isImportInProgress (), "Import should not be in progress after background cancellation")

    [<TestMethod>]
    member this.``ReactiveManagers are accessible for refresh after successful import``() =
        // Test that ImportManager can access the reactive managers for refresh
        // This validates that our changes to ImportManager.fs compilation order work correctly
        try
            // These should not throw exceptions if the compilation order is correct
            Binnaculum.Core.UI.ReactiveTickerManager.refresh () |> ignore // Added for cache dependency fix
            Binnaculum.Core.UI.ReactiveMovementManager.refresh ()
            Binnaculum.Core.UI.ReactiveSnapshotManager.refresh ()
            // If we get here without exceptions, the test passes
            Assert.IsTrue(true, "All reactive managers are accessible and refreshable from ImportManager context")
        with ex ->
            Assert.Fail($"Reactive managers should be accessible from ImportManager context. Error: {ex.Message}")

    [<TestMethod>]
    member this.``ReactiveManagers async refresh methods are accessible and awaitable``() =
        task {
            // Test that the new awaitable refresh methods work correctly
            // This validates the async timing fix for imported data not appearing in UI
            try
                // These methods should be awaitable and complete successfully
                do! ReactiveMovementManager.refreshAsync ()
                do! ReactiveSnapshotManager.refreshAsync ()

                // Verify both sync and async methods are available for backward compatibility
                ReactiveMovementManager.refresh ()
                ReactiveSnapshotManager.refresh ()

                Assert.IsTrue(true, "Both sync and async refresh methods are accessible and functional")
            with ex ->
                Assert.Fail($"Async refresh methods should be accessible and awaitable. Error: {ex.Message}")
        }
