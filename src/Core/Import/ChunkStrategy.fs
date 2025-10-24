namespace Binnaculum.Core.Import

open System

/// <summary>
/// Information about a single import chunk.
/// Used to guide chunk-by-chunk processing strategy.
/// </summary>
type ChunkInfo =
    { ChunkNumber: int
      StartDate: DateOnly
      EndDate: DateOnly
      EstimatedMovements: int }

/// <summary>
/// Smart chunking strategy for import processing.
/// Splits large imports into manageable chunks to avoid memory pressure on mobile devices.
/// </summary>
module ChunkStrategy =

    /// <summary>
    /// Maximum movements per chunk - tuned for mobile device memory constraints.
    /// 2000 movements ≈ 2MB memory on typical mobile device.
    /// </summary>
    let MAX_MOVEMENTS_PER_CHUNK = 2000

    /// <summary>
    /// Minimum chunk size in days - avoid creating too many tiny chunks.
    /// </summary>
    let MIN_CHUNK_DAYS = 1

    /// <summary>
    /// Default chunk size in days for normal trading volume.
    /// </summary>
    let DEFAULT_CHUNK_DAYS = 7

    /// <summary>
    /// Maximum chunk size in days - even with low volume, limit chunk size.
    /// </summary>
    let MAX_CHUNK_DAYS = 14

    /// <summary>
    /// Create weekly chunks from date analysis.
    /// Adapts chunk size based on movement volume to stay within memory limits.
    /// </summary>
    let createWeeklyChunks (analysis: DateAnalysis) : ChunkInfo list =
        if analysis.TotalMovements = 0 then
            // No movements to process
            []
        else
            let mutable chunks = []
            let mutable chunkNumber = 1
            let mutable currentStart = analysis.MinDate.Date
            let maxDate = analysis.MaxDate.Date

            while currentStart <= maxDate do
                // Try default 7-day chunk first
                let potentialEnd = currentStart.AddDays(float DEFAULT_CHUNK_DAYS)
                let actualEnd = min potentialEnd maxDate

                // Count movements in this potential chunk
                let movementCount =
                    analysis.MovementsByDate
                    |> Map.filter (fun date _ ->
                        let dateTime = date.ToDateTime(TimeOnly.MinValue)
                        dateTime >= currentStart && dateTime <= actualEnd)
                    |> Map.fold (fun acc _ count -> acc + count) 0

                // Determine final chunk size based on volume
                let finalEnd, finalCount =
                    if movementCount > MAX_MOVEMENTS_PER_CHUNK then
                        // Too many movements - reduce chunk size
                        // Try 3-day chunk
                        let reducedEnd = min (currentStart.AddDays(3.0)) maxDate

                        let reducedCount =
                            analysis.MovementsByDate
                            |> Map.filter (fun date _ ->
                                let dateTime = date.ToDateTime(TimeOnly.MinValue)
                                dateTime >= currentStart && dateTime <= reducedEnd)
                            |> Map.fold (fun acc _ count -> acc + count) 0

                        if reducedCount > MAX_MOVEMENTS_PER_CHUNK then
                            // Still too many - go to 1-day chunks
                            let singleDayEnd = min (currentStart.AddDays(1.0)) maxDate

                            let singleDayCount =
                                analysis.MovementsByDate
                                |> Map.filter (fun date _ ->
                                    let dateTime = date.ToDateTime(TimeOnly.MinValue)
                                    dateTime >= currentStart && dateTime <= singleDayEnd)
                                |> Map.fold (fun acc _ count -> acc + count) 0

                            (singleDayEnd, singleDayCount)
                        else
                            (reducedEnd, reducedCount)
                    elif
                        movementCount < (MAX_MOVEMENTS_PER_CHUNK / 4)
                        && (actualEnd - currentStart).TotalDays < float MAX_CHUNK_DAYS
                    then
                        // Low volume - can extend to 14 days
                        let extendedEnd = min (currentStart.AddDays(float MAX_CHUNK_DAYS)) maxDate

                        let extendedCount =
                            analysis.MovementsByDate
                            |> Map.filter (fun date _ ->
                                let dateTime = date.ToDateTime(TimeOnly.MinValue)
                                dateTime >= currentStart && dateTime <= extendedEnd)
                            |> Map.fold (fun acc _ count -> acc + count) 0

                        if extendedCount <= MAX_MOVEMENTS_PER_CHUNK then
                            (extendedEnd, extendedCount)
                        else
                            (actualEnd, movementCount)
                    else
                        // Normal volume - use 7-day chunk
                        (actualEnd, movementCount)

                // Create chunk
                let chunk =
                    { ChunkNumber = chunkNumber
                      StartDate = DateOnly.FromDateTime(currentStart)
                      EndDate = DateOnly.FromDateTime(finalEnd)
                      EstimatedMovements = finalCount }

                chunks <- chunks @ [ chunk ]
                chunkNumber <- chunkNumber + 1

                // Move to next chunk
                currentStart <- finalEnd.AddDays(1.0)

            chunks

    /// <summary>
    /// Create fixed-size chunks (for testing or simple scenarios).
    /// </summary>
    let createFixedChunks (analysis: DateAnalysis) (chunkSizeDays: int) : ChunkInfo list =
        if analysis.TotalMovements = 0 then
            []
        else
            let mutable chunks = []
            let mutable chunkNumber = 1
            let mutable currentStart = analysis.MinDate.Date
            let maxDate = analysis.MaxDate.Date

            while currentStart <= maxDate do
                let chunkEnd = min (currentStart.AddDays(float chunkSizeDays)) maxDate

                let movementCount =
                    analysis.MovementsByDate
                    |> Map.filter (fun date _ ->
                        let dateTime = date.ToDateTime(TimeOnly.MinValue)
                        dateTime >= currentStart && dateTime <= chunkEnd)
                    |> Map.fold (fun acc _ count -> acc + count) 0

                let chunk =
                    { ChunkNumber = chunkNumber
                      StartDate = DateOnly.FromDateTime(currentStart)
                      EndDate = DateOnly.FromDateTime(chunkEnd)
                      EstimatedMovements = movementCount }

                chunks <- chunks @ [ chunk ]
                chunkNumber <- chunkNumber + 1
                currentStart <- chunkEnd.AddDays(1.0)

            chunks

    /// <summary>
    /// Get summary statistics about chunking strategy.
    /// Useful for logging and diagnostics.
    /// </summary>
    let getChunkingSummary (chunks: ChunkInfo list) : string =
        if chunks.IsEmpty then
            "No chunks created (no movements to process)"
        else
            let totalMovements = chunks |> List.sumBy (fun c -> c.EstimatedMovements)
            let avgMovementsPerChunk = totalMovements / chunks.Length
            let maxMovementsChunk = chunks |> List.maxBy (fun c -> c.EstimatedMovements)
            let minMovementsChunk = chunks |> List.minBy (fun c -> c.EstimatedMovements)

            sprintf
                "Chunking Summary: %d chunks, %d total movements, avg %d movements/chunk (min: %d, max: %d)"
                chunks.Length
                totalMovements
                avgMovementsPerChunk
                minMovementsChunk.EstimatedMovements
                maxMovementsChunk.EstimatedMovements
