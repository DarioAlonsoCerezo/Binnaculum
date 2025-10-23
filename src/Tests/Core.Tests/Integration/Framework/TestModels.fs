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
