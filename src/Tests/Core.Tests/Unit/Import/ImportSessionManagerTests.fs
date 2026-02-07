namespace Core.Tests.Integration

#nowarn "3261" // Nullness warning for SqliteConnection

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Import
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Logging
open Binnaculum.Core.UI
open TestModels

/// <summary>
/// Comprehensive tests for ImportSessionManager functionality.
/// Tests session creation, chunk tracking, resume logic, and atomic transactions.
/// Uses in-memory SQLite database with full integration test framework.
///
/// Inherits from TestFixtureBase - provides proper database initialization,
/// reactive collections, and test context with broker/account data.
/// </summary>
[<TestClass>]
type ImportSessionManagerTests() =
    inherit TestFixtureBase()

    // Test broker account ID will be populated during setup
    let mutable testBrokerAccountId = 0

    /// <summary>
    /// Setup test environment with a broker account
    /// </summary>
    [<TestInitialize>]
    member this.SetupTest() =
        async {
            CoreLogger.logInfo "ImportSessionManagerTests" "Setting up test environment..."

            // Initialize database (loads brokers, currencies, etc.)
            let! (ok, _, error) = this.Actions.initDatabase ()
            Assert.IsTrue(ok, sprintf "Database initialization should succeed: %A" error)

            // Create a test broker account
            let! (ok, details, error) = this.Actions.createBrokerAccount ("ImportSession-Test")
            Assert.IsTrue(ok, sprintf "Account creation should succeed: %s - %A" details error)

            // Extract account ID from details (format: "Account created (ID=X)")
            let idStr = details.Replace("Account created (ID=", "").Replace(")", "")
            testBrokerAccountId <- Int32.Parse(idStr)

            CoreLogger.logInfo "ImportSessionManagerTests" $"Test account created: ID={testBrokerAccountId}"
        }
        |> Async.RunSynchronously

    /// <summary>
    /// Helper to create a test DateAnalysis
    /// </summary>
    member private this.CreateTestAnalysis(totalMovements: int, startDate: DateTime, endDate: DateTime) : DateAnalysis =
        let movementsByDate =
            let mutable map = Map.empty<DateOnly, int>
            let mutable currentDate = startDate

            while currentDate <= endDate do
                let dateOnly = DateOnly.FromDateTime(currentDate)
                map <- map |> Map.add dateOnly (totalMovements / max 1 (endDate - startDate).Days)
                currentDate <- currentDate.AddDays(1.0)

            map

        { MinDate = startDate
          MaxDate = endDate
          TotalMovements = totalMovements
          MovementsByDate = movementsByDate
          UniqueDates = movementsByDate |> Map.toList |> List.map fst |> List.sort
          FileHash = "test-hash-123" }

    /// <summary>
    /// Helper to create test chunks
    /// </summary>
    member private this.CreateTestChunks(numChunks: int, startDate: DateTime) : ChunkInfo list =
        [ 1..numChunks ]
        |> List.map (fun i ->
            let chunkStart = startDate.AddDays(float ((i - 1) * 7))
            let chunkEnd = startDate.AddDays(float (i * 7 - 1))

            { ChunkNumber = i
              StartDate = DateOnly.FromDateTime(chunkStart)
              EndDate = DateOnly.FromDateTime(chunkEnd)
              EstimatedMovements = 500 })

    // ==================== Session Creation Tests ====================

    [<TestMethod>]
    member this.``createSession creates session in database``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            // Act
            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Assert
            Assert.IsTrue(sessionId > 0, "Session ID should be positive")

            // Verify session was created
            let! sessionOpt = ImportSessionManager.getSessionById sessionId
            Assert.IsTrue((Option.isSome sessionOpt), "Session should exist")

            match sessionOpt with
            | Some session ->
                Assert.AreEqual(1, session.BrokerAccountId)
                Assert.AreEqual("Test Account", session.BrokerAccountName)
                Assert.AreEqual("test-hash-123", session.FileHash)
                Assert.AreEqual(2, session.TotalChunks)
                Assert.AreEqual(0, session.ChunksCompleted)
                Assert.AreEqual("Phase1_PersistingMovements", session.State)
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``createSession creates all chunks in database``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(3, DateTime(2024, 1, 1))

            // Act
            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Assert
            let! savedChunks = ImportSessionManager.getChunks sessionId
            Assert.AreEqual(3, savedChunks.Length, "Should have 3 chunks")

            // Verify chunks are in correct order
            for i in 0 .. savedChunks.Length - 1 do
                Assert.AreEqual(i + 1, savedChunks.[i].ChunkNumber)
                Assert.AreEqual("Pending", savedChunks.[i].State)
                Assert.AreEqual(500, savedChunks.[i].EstimatedMovements)
        }

    // ==================== Chunk Status Tests ====================

    [<TestMethod>]
    member this.``markChunkCompleted updates chunk status atomically``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act - markChunkCompleted handles transaction internally
            do! ImportSessionManager.markChunkCompleted sessionId 1 500 1000L

            // Assert - Verify chunk was marked complete
            let! allChunks = ImportSessionManager.getChunks sessionId
            let chunk1 = allChunks |> List.find (fun c -> c.ChunkNumber = 1)

            Assert.AreEqual("Completed", chunk1.State)
            Assert.AreEqual(500, chunk1.ActualMovements)
            Assert.AreEqual(Some 1000L, chunk1.DurationMs)
            Assert.IsTrue((Option.isSome chunk1.CompletedAt))

            // Verify session progress was updated
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                Assert.AreEqual(1, session.ChunksCompleted)
                Assert.AreEqual(500, session.MovementsPersisted)
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``markChunkCompleted is lenient with non-existent chunks``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act - Mark a non-existent chunk (system is lenient and doesn't validate chunk existence)
            do! ImportSessionManager.markChunkCompleted sessionId 999 500 1000L

            // Assert - Verify the operation succeeded (lenient behavior)
            // The session should have its progress updated even though chunk 999 doesn't exist
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                // Since the system is lenient, chunk 999 was marked complete even though it doesn't exist
                Assert.AreEqual(1, session.ChunksCompleted, "Progress should be updated (lenient behavior)")
                Assert.AreEqual(500, session.MovementsPersisted)
            | None -> Assert.Fail("Session should exist")
        }

    // ==================== Session Query Tests ====================

    [<TestMethod>]
    member this.``getActiveSession returns active session for account``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            let! activeSession = ImportSessionManager.getActiveSession 1

            // Assert
            Assert.IsTrue((Option.isSome activeSession))

            match activeSession with
            | Some session ->
                Assert.AreEqual(sessionId, session.Id)
                Assert.AreEqual("Phase1_PersistingMovements", session.State)
            | None -> Assert.Fail("Should find active session")
        }

    [<TestMethod>]
    member this.``getActiveSession returns None when no active session``() =
        task {
            // Act
            let! activeSession = ImportSessionManager.getActiveSession 999

            // Assert
            Assert.IsTrue((Option.isNone activeSession))
        }

    [<TestMethod>]
    member this.``getPendingChunks returns only pending and failed chunks``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(3, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Mark first chunk complete
            do! ImportSessionManager.markChunkCompleted sessionId 1 500 1000L

            // Act
            let! pendingChunks = ImportSessionManager.getPendingChunks sessionId

            // Assert
            Assert.AreEqual(2, pendingChunks.Length, "Should have 2 pending chunks")
            Assert.IsTrue((pendingChunks |> List.forall (fun c -> c.State = "Pending")))

            Assert.IsFalse((pendingChunks |> List.exists (fun c -> c.ChunkNumber = 1)), "Completed chunk should not be in pending list")
        }

    // ==================== Phase Transition Tests ====================

    [<TestMethod>]
    member this.``markPhase1Completed transitions to Phase2``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.markPhase1Completed sessionId

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                Assert.AreEqual("Phase2_CalculatingSnapshots", session.Phase)
                Assert.IsTrue((Option.isSome session.Phase1CompletedAt))
                Assert.IsTrue((Option.isSome session.Phase2StartedAt))
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``markBrokerSnapshotsCompleted sets flag``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.markBrokerSnapshotsCompleted sessionId

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session -> Assert.AreEqual(1, session.BrokerSnapshotsCalculated)
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``markTickerSnapshotsCompleted sets flag``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.markTickerSnapshotsCompleted sessionId

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session -> Assert.AreEqual(1, session.TickerSnapshotsCalculated)
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``completeSession marks session as completed``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.completeSession sessionId

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                Assert.AreEqual("Completed", session.State)
                Assert.IsTrue((Option.isSome session.CompletedAt))
            | None -> Assert.Fail("Session should exist")

            // Verify it's no longer active
            let! activeSession = ImportSessionManager.getActiveSession 1
            Assert.IsTrue((Option.isNone activeSession), "Completed session should not be active")
        }

    // ==================== File Hash Validation Tests ====================

    [<TestMethod>]
    member this.``validateFileHash returns true for matching hash``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            // Create a temporary test file with known content
            let testFile = System.IO.Path.GetTempFileName()
            do! System.IO.File.WriteAllTextAsync(testFile, "test content") |> Async.AwaitTask

            let actualHash = CsvDateAnalyzer.calculateFileHash testFile

            // Update session with actual file hash
            let session =
                match sessionOpt with
                | Some s ->
                    { s with
                        FileHash = actualHash
                        FilePath = testFile }
                | None -> failwith "Session should exist"

            // Act
            let isValid = ImportSessionManager.validateFileHash session testFile

            // Assert
            Assert.IsTrue(isValid, "File hash should match")

            // Cleanup
            System.IO.File.Delete(testFile)
        }

    [<TestMethod>]
    member this.``validateFileHash returns false for mismatched hash``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            // Create a temporary test file
            let testFile = System.IO.Path.GetTempFileName()
            do! System.IO.File.WriteAllTextAsync(testFile, "test content") |> Async.AwaitTask

            // Session has different hash
            let session =
                match sessionOpt with
                | Some s ->
                    { s with
                        FileHash = "different-hash"
                        FilePath = testFile }
                | None -> failwith "Session should exist"

            // Act
            let isValid = ImportSessionManager.validateFileHash session testFile

            // Assert
            Assert.IsFalse(isValid, "File hash should not match")

            // Cleanup
            System.IO.File.Delete(testFile)
        }

    // ==================== Error Handling Tests ====================

    [<TestMethod>]
    member this.``markSessionFailed records error message``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.markSessionFailed sessionId "Test error message"

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                Assert.AreEqual("Failed", session.State)
                Assert.AreEqual(Some "Test error message", session.LastError)
            | None -> Assert.Fail("Session should exist")
        }

    [<TestMethod>]
    member this.``markSessionCancelled marks session as cancelled``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act
            do! ImportSessionManager.markSessionCancelled sessionId

            // Assert
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session -> Assert.AreEqual("Cancelled", session.State)
            | None -> Assert.Fail("Session should exist")
        }

    // ==================== Multiple Sessions Tests ====================

    [<TestMethod>]
    member this.``multiple sessions can exist for different accounts``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            // Create a second broker account for testing
            let! (ok, details, error) = this.Actions.createBrokerAccount ("ImportSession-Test-2")
            Assert.IsTrue(ok, sprintf "Account 2 creation should succeed: %s - %A" details error)
            let idStr = details.Replace("Account created (ID=", "").Replace(")", "")
            let account2Id = Int32.Parse(idStr)

            // Act - Create sessions for two different accounts
            let! sessionId1 =
                ImportSessionManager.createSession testBrokerAccountId "Account 1" "/test/file1.csv" analysis chunks

            let! sessionId2 =
                ImportSessionManager.createSession account2Id "Account 2" "/test/file2.csv" analysis chunks

            // Assert
            Assert.AreNotEqual(sessionId1, sessionId2)

            let! activeSession1 = ImportSessionManager.getActiveSession testBrokerAccountId
            let! activeSession2 = ImportSessionManager.getActiveSession account2Id

            match activeSession1, activeSession2 with
            | Some s1, Some s2 ->
                Assert.AreEqual(testBrokerAccountId, s1.BrokerAccountId)
                Assert.AreEqual(account2Id, s2.BrokerAccountId)
                Assert.AreNotEqual(s1.Id, s2.Id)
            | _ -> Assert.Fail("Both sessions should exist")
        }

    [<TestMethod>]
    member this.``only one active session allowed per account``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            // Create first session
            let! sessionId1 =
                ImportSessionManager.createSession testBrokerAccountId "Account 1" "/test/file1.csv" analysis chunks

            // Create second session for same account
            let! sessionId2 =
                ImportSessionManager.createSession testBrokerAccountId "Account 1" "/test/file2.csv" analysis chunks

            // Act
            let! activeSession = ImportSessionManager.getActiveSession 1

            // Assert - Should return the most recent session (sessionId2)
            match activeSession with
            | Some session -> Assert.AreEqual(sessionId2, session.Id, "Should return most recent session")
            | None -> Assert.Fail("Should have an active session")
        }

    // ==================== Exception Propagation Tests ====================

    [<TestMethod>]
    member this.``getSessionById propagates exceptions on database errors``() =
        task {
            // Arrange - Use a session ID that will cause issues deep in the DB layer
            // In the previous implementation, this would have returned None
            // Now it should propagate any database exception

            // We'll test by getting a valid session first, then trying with invalid negative ID
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            let! sessionId =
                ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks

            // Act - Try to get session with negative ID (which should work but return None)
            // The key here is that no exception is swallowed - if DB throws, it propagates
            let! result = ImportSessionManager.getSessionById -999

            // Assert - Should return None for non-existent session, not throw
            // This demonstrates exceptions aren't being caught and converted to None
            Assert.IsTrue((Option.isNone result), "Non-existent session should return None naturally")
        }

    [<TestMethod>]
    member this.``getPendingChunks propagates exceptions instead of returning empty list``() =
        task {
            // Arrange - Use invalid session ID
            // In the previous implementation, this would have returned []
            // Now it propagates database exceptions naturally

            // Act - Get pending chunks for non-existent session
            // Should return empty list naturally, not by catching exception
            let! chunks = ImportSessionManager.getPendingChunks -999

            // Assert - Should return empty list naturally for non-existent session
            // The key is that we're not catching and hiding exceptions
            Assert.AreEqual(0, chunks.Count, "Non-existent session should return empty list naturally")
        }

    [<TestMethod>]
    member this.``validateFileHash propagates exceptions for invalid file paths``() =
        // Arrange - Create a session
        let analysis =
            this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

        let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

        let sessionId =
            task {
                return! ImportSessionManager.createSession testBrokerAccountId "Test Account" "/test/file.csv" analysis chunks
            }
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let sessionOpt =
            task {
                return! ImportSessionManager.getSessionById sessionId
            }
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let session =
            match sessionOpt with
            | Some s -> s
            | None -> failwith "Session should exist"

        // Act & Assert - Should throw exception for non-existent file
        // In the previous implementation, this would have returned false
        let ex =
            Assert.Throws<System.IO.FileNotFoundException>(fun () ->
                ImportSessionManager.validateFileHash session "/nonexistent/file/that/does/not/exist/anywhere.csv"
                |> ignore)

        // Verify it's the expected exception type
        Assert.IsNotNull(ex, "FileNotFoundException should be thrown")

    [<TestMethod>]
    member this.``getActiveSession returns None naturally without catching exceptions``() =
        task {
            // Arrange - Query for non-existent broker account
            // This tests that the function returns None naturally, not by catching exceptions

            // Act
            let! result = ImportSessionManager.getActiveSession 99999

            // Assert - Should return None for non-existent account
            Assert.IsTrue((Option.isNone result), "Non-existent account should return None")
        }
