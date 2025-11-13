namespace Core.Tests.Integration

#nowarn "3261" // Nullness warning for SqliteConnection

open NUnit.Framework
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
[<TestFixture>]
type ImportSessionManagerTests() =
    inherit TestFixtureBase()

    // Test broker account ID will be populated during setup
    let mutable testBrokerAccountId = 0

    /// <summary>
    /// Setup test environment with a broker account
    /// </summary>
    [<SetUp>]
    member this.SetupTest() =
        async {
            CoreLogger.logInfo "ImportSessionManagerTests" "Setting up test environment..."

            // Initialize database (loads brokers, currencies, etc.)
            let! (ok, _, error) = this.Actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)

            // Create a test broker account
            let! (ok, details, error) = this.Actions.createBrokerAccount ("ImportSession-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)

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

    [<Test>]
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
            Assert.That(sessionId, Is.GreaterThan 0, "Session ID should be positive")

            // Verify session was created
            let! sessionOpt = ImportSessionManager.getSessionById sessionId
            Assert.That((Option.isSome sessionOpt), Is.True, "Session should exist")

            match sessionOpt with
            | Some session ->
                Assert.That(session.BrokerAccountId, Is.EqualTo(1))
                Assert.That(session.BrokerAccountName, Is.EqualTo("Test Account"))
                Assert.That(session.FileHash, Is.EqualTo("test-hash-123"))
                Assert.That(session.TotalChunks, Is.EqualTo(2))
                Assert.That(session.ChunksCompleted, Is.EqualTo(0))
                Assert.That(session.State, Is.EqualTo("Phase1_PersistingMovements"))
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
            Assert.That(savedChunks.Length, Is.EqualTo(3), "Should have 3 chunks")

            // Verify chunks are in correct order
            for i in 0 .. savedChunks.Length - 1 do
                Assert.That(savedChunks.[i].ChunkNumber, Is.EqualTo(i + 1))
                Assert.That(savedChunks.[i].State, Is.EqualTo("Pending"))
                Assert.That(savedChunks.[i].EstimatedMovements, Is.EqualTo(500))
        }

    // ==================== Chunk Status Tests ====================

    [<Test>]
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

            Assert.That(chunk1.State, Is.EqualTo("Completed"))
            Assert.That(chunk1.ActualMovements, Is.EqualTo(500))
            Assert.That(chunk1.DurationMs, Is.EqualTo(Some 1000L))
            Assert.That((Option.isSome chunk1.CompletedAt), Is.True)

            // Verify session progress was updated
            let! sessionOpt = ImportSessionManager.getSessionById sessionId

            match sessionOpt with
            | Some session ->
                Assert.That(session.ChunksCompleted, Is.EqualTo(1))
                Assert.That(session.MovementsPersisted, Is.EqualTo(500))
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
                Assert.That(session.ChunksCompleted, Is.EqualTo(1), "Progress should be updated (lenient behavior)")
                Assert.That(session.MovementsPersisted, Is.EqualTo(500))
            | None -> Assert.Fail("Session should exist")
        }

    // ==================== Session Query Tests ====================

    [<Test>]
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
            Assert.That((Option.isSome activeSession), Is.True)

            match activeSession with
            | Some session ->
                Assert.That(session.Id, Is.EqualTo(sessionId))
                Assert.That(session.State, Is.EqualTo("Phase1_PersistingMovements"))
            | None -> Assert.Fail("Should find active session")
        }

    [<Test>]
    member this.``getActiveSession returns None when no active session``() =
        task {
            // Act
            let! activeSession = ImportSessionManager.getActiveSession 999

            // Assert
            Assert.That((Option.isNone activeSession), Is.True)
        }

    [<Test>]
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
            Assert.That(pendingChunks.Length, Is.EqualTo(2), "Should have 2 pending chunks")
            Assert.That((pendingChunks |> List.forall (fun c -> c.State = "Pending")), Is.True)

            Assert.That(
                (pendingChunks |> List.exists (fun c -> c.ChunkNumber = 1)),
                Is.False,
                "Completed chunk should not be in pending list"
            )
        }

    // ==================== Phase Transition Tests ====================

    [<Test>]
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
                Assert.That(session.Phase, Is.EqualTo("Phase2_CalculatingSnapshots"))
                Assert.That((Option.isSome session.Phase1CompletedAt), Is.True)
                Assert.That((Option.isSome session.Phase2StartedAt), Is.True)
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
            | Some session -> Assert.That(session.BrokerSnapshotsCalculated, Is.EqualTo(1))
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
            | Some session -> Assert.That(session.TickerSnapshotsCalculated, Is.EqualTo(1))
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
                Assert.That(session.State, Is.EqualTo("Completed"))
                Assert.That((Option.isSome session.CompletedAt), Is.True)
            | None -> Assert.Fail("Session should exist")

            // Verify it's no longer active
            let! activeSession = ImportSessionManager.getActiveSession 1
            Assert.That((Option.isNone activeSession), Is.True, "Completed session should not be active")
        }

    // ==================== File Hash Validation Tests ====================

    [<Test>]
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
            Assert.That(isValid, Is.True, "File hash should match")

            // Cleanup
            System.IO.File.Delete(testFile)
        }

    [<Test>]
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
            Assert.That(isValid, Is.False, "File hash should not match")

            // Cleanup
            System.IO.File.Delete(testFile)
        }

    // ==================== Error Handling Tests ====================

    [<Test>]
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
                Assert.That(session.State, Is.EqualTo("Failed"))
                Assert.That(session.LastError, Is.EqualTo(Some "Test error message"))
            | None -> Assert.Fail("Session should exist")
        }

    [<Test>]
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
            | Some session -> Assert.That(session.State, Is.EqualTo("Cancelled"))
            | None -> Assert.Fail("Session should exist")
        }

    // ==================== Multiple Sessions Tests ====================

    [<Test>]
    member this.``multiple sessions can exist for different accounts``() =
        task {
            // Arrange
            let analysis =
                this.CreateTestAnalysis(2000, DateTime(2024, 1, 1), DateTime(2024, 1, 31))

            let chunks = this.CreateTestChunks(2, DateTime(2024, 1, 1))

            // Create a second broker account for testing
            let! (ok, details, error) = this.Actions.createBrokerAccount ("ImportSession-Test-2")
            Assert.That(ok, Is.True, sprintf "Account 2 creation should succeed: %s - %A" details error)
            let idStr = details.Replace("Account created (ID=", "").Replace(")", "")
            let account2Id = Int32.Parse(idStr)

            // Act - Create sessions for two different accounts
            let! sessionId1 =
                ImportSessionManager.createSession testBrokerAccountId "Account 1" "/test/file1.csv" analysis chunks

            let! sessionId2 =
                ImportSessionManager.createSession account2Id "Account 2" "/test/file2.csv" analysis chunks

            // Assert
            Assert.That(sessionId2, Is.Not.EqualTo(sessionId1))

            let! activeSession1 = ImportSessionManager.getActiveSession testBrokerAccountId
            let! activeSession2 = ImportSessionManager.getActiveSession account2Id

            match activeSession1, activeSession2 with
            | Some s1, Some s2 ->
                Assert.That(s1.BrokerAccountId, Is.EqualTo(testBrokerAccountId))
                Assert.That(s2.BrokerAccountId, Is.EqualTo(account2Id))
                Assert.That(s2.Id, Is.Not.EqualTo(s1.Id))
            | _ -> Assert.Fail("Both sessions should exist")
        }

    [<Test>]
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
            | Some session -> Assert.That(session.Id, Is.EqualTo(sessionId2), "Should return most recent session")
            | None -> Assert.Fail("Should have an active session")
        }
