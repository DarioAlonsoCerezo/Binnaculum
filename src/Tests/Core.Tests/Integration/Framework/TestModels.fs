namespace Core.Tests.Integration

/// <summary>
/// Shared test types and abstractions used across integration tests.
/// Provides common data structures for test verification and validation.
/// </summary>
module TestModels =

    /// <summary>
    /// Pairs expected data with a human-readable description for test verification.
    /// This type provides a standard way to document test expectations alongside the data.
    ///
    /// Example:
    /// ```fsharp
    /// let expectedSnapshots = [
    ///     { Data = snapshot1; Description = "After first deposit" }
    ///     { Data = snapshot2; Description = "After first trade" }
    /// ]
    /// ```
    /// </summary>
    type ExpectedSnapshot<'T> =
        {
            /// The expected data/snapshot to verify against
            Data: 'T
            /// Human-readable description explaining what this snapshot represents
            Description: string
        }

    /// <summary>
    /// Pairs expected operation data with a human-readable description for test verification.
    /// This type provides a standard way to document operation expectations alongside the data.
    ///
    /// Example:
    /// ```fsharp
    /// let expectedOperations = [
    ///     { Data = operation1; Description = "Cash-secured put opened" }
    ///     { Data = operation2; Description = "Covered call completed" }
    /// ]
    /// ```
    /// </summary>
    type ExpectedOperation<'T> =
        {
            /// The expected operation data to verify against
            Data: 'T
            /// Human-readable description explaining what this operation represents
            Description: string
        }

    /// <summary>
    /// Extract just the data from a list of expected snapshots.
    /// Useful when you need to pass data to verification functions.
    /// </summary>
    let getData (snapshots: ExpectedSnapshot<'T> list) : 'T list = snapshots |> List.map (fun s -> s.Data)

    /// <summary>
    /// Extract just the descriptions from a list of expected snapshots.
    /// Useful when building description functions for verification.
    /// </summary>
    let getDescriptions (snapshots: ExpectedSnapshot<'T> list) : string list =
        snapshots |> List.map (fun s -> s.Description)

    /// <summary>
    /// Create a description function from a list of expected snapshots.
    /// Returns a function that takes an index and returns the description at that index.
    ///
    /// Example:
    /// ```fsharp
    /// let descriptionFn = getDescriptionFunction expectedSnapshots
    /// descriptionFn 0 // Returns description for first snapshot
    /// ```
    /// </summary>
    let getDescriptionFunction (snapshots: ExpectedSnapshot<'T> list) : (int -> string) =
        fun i -> snapshots.[i].Description

    /// <summary>
    /// Extract just the data from a list of expected operations.
    /// Useful when you need to pass data to verification functions.
    /// </summary>
    let getOperationData (operations: ExpectedOperation<'T> list) : 'T list =
        operations |> List.map (fun o -> o.Data)

    /// <summary>
    /// Extract just the descriptions from a list of expected operations.
    /// Useful when building description functions for verification.
    /// </summary>
    let getOperationDescriptions (operations: ExpectedOperation<'T> list) : string list =
        operations |> List.map (fun o -> o.Description)

    /// <summary>
    /// Create a description function from a list of expected operations.
    /// Returns a function that takes an index and returns the description at that index.
    ///
    /// Example:
    /// ```fsharp
    /// let descriptionFn = getOperationDescriptionFunction expectedOperations
    /// descriptionFn 0 // Returns description for first operation
    /// ```
    /// </summary>
    let getOperationDescriptionFunction (operations: ExpectedOperation<'T> list) : (int -> string) =
        fun i -> operations.[i].Description
