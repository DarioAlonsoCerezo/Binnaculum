using Binnaculum.Core;
using Binnaculum.Core.Import;

namespace Binnaculum.Extensions;

public static class CoreExtensions
{
    public static bool ToEnableButton(this CurrentImportStatus status)
    {
        var state = status.State;
        return state == ImportStateEnum.NotStarted ||
            state == ImportStateEnum.Cancelled ||
            state == ImportStateEnum.Completed ||
            state == ImportStateEnum.Failed;
    }

    public static bool ToShowProgress(this CurrentImportStatus status)
    {
        return !status.ToEnableButton();
    }

    public static bool ToShowResults(this CurrentImportStatus status)
    {
        var state = status.State;
        return state != ImportStateEnum.NotStarted;
    }

    public static bool ToShowStatus(this CurrentImportStatus status)
    {
        return status.Message != null;
    }

    public static string ToMessage(this CurrentImportStatus status)
    {
        var state = status.State;

        // SavingToDatabase state
        if (state == ImportStateEnum.SavingToDatabase)
        {
            if (status.RecordsProcessed != null && status.TotalRecords != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    ResourceKeys.Import_SavingData,
                    status.RecordsProcessed.Value,
                    status.TotalRecords.Value
                );
            }
            return ResourceKeys.Import_SavingData_Generic.ToLocalized();
        }

        // ProcessingFile state
        if (state == ImportStateEnum.ProcessingFile)
        {
            if (status.FileName != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    ResourceKeys.Import_ProcessingFile,
                    status.FileName.Value
                );
            }
            return ResourceKeys.Import_ProcessingFile_Generic.ToLocalized();
        }

        // ProcessingData state
        if (state == ImportStateEnum.ProcessingData)
        {
            if (status.RecordsProcessed != null && status.TotalRecords != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    ResourceKeys.Import_ProcessingRecords,
                    status.RecordsProcessed.Value,
                    status.TotalRecords.Value
                );
            }
            return ResourceKeys.Import_ProcessingRecords_Generic.ToLocalized();
        }

        // CalculatingSnapshots state
        if (state == ImportStateEnum.CalculatingSnapshots)
        {
            if (status.RecordsProcessed != null && status.TotalRecords != null && status.ProcessedDate != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    "Import_CalculatingSnapshots",
                    status.RecordsProcessed.Value,
                    status.TotalRecords.Value,
                    status.ProcessedDate
                );
            }
            return ResourceKeys.Import_CalculatingSnapshots_Generic.ToLocalized();
        }

        // Validating state
        if (state == ImportStateEnum.Validating)
        {
            return ResourceKeys.Import_Validating.ToLocalized();
        }

        // Cancelled state
        if (state == ImportStateEnum.Cancelled)
        {
            return ResourceKeys.Import_Cancelled.ToLocalized();
        }

        // Failed state
        if (state == ImportStateEnum.Failed)
        {
            return ResourceKeys.Import_Failed.ToLocalized();
        }

        // Completed state
        if (state == ImportStateEnum.Completed)
        {
            return ResourceKeys.Import_Completed.ToLocalized();
        }

        return string.Empty;
    }

    // ==================== CHUNKED IMPORT STATUS EXTENSIONS ====================

    public static bool ToEnableButton(this CurrentChunkedImportStatus status)
    {
        var state = status.State;
        return state == ChunkedImportStateEnum.Idle ||
               state == ChunkedImportStateEnum.Cancelled ||
               state == ChunkedImportStateEnum.Completed ||
               state == ChunkedImportStateEnum.Failed;
    }

    public static bool ToShowProgress(this CurrentChunkedImportStatus status)
    {
        return !status.ToEnableButton();
    }

    public static bool ToShowResults(this CurrentChunkedImportStatus status)
    {
        return status.State != ChunkedImportStateEnum.Idle;
    }

    public static bool ToShowChunkInfo(this CurrentChunkedImportStatus status)
    {
        return status.ChunkNumber != null && status.TotalChunks != null;
    }

    public static bool ToShowTimeRemaining(this CurrentChunkedImportStatus status)
    {
        return status.EstimatedTimeRemaining != null;
    }

    public static string ToMessage(this CurrentChunkedImportStatus status)
    {
        var state = status.State;

        if (state == ChunkedImportStateEnum.ReadingFile && status.FileName != null)
        {
            return ResourceKeys.Import_Chunked_ReadingFile.ToLocalized(status.FileName);
        }

        if (state == ChunkedImportStateEnum.AnalyzingDates && status.FileName != null)
        {
            return ResourceKeys.Import_Chunked_AnalyzingDates.ToLocalized(status.FileName);
        }

        if (state == ChunkedImportStateEnum.ProcessingChunk && status.ChunkNumber != null && status.TotalChunks != null)
        {
            return ResourceKeys.Import_Chunked_ProcessingChunk.ToLocalized(status.ChunkNumber.Value, status.TotalChunks.Value);
        }

        if (state == ChunkedImportStateEnum.CalculatingSnapshots && status.SnapshotType != null &&
            status.SnapshotsProcessed != null && status.SnapshotsTotal != null)
        {
            var localizedType = status.ToLocalizedSnapshotType();
            return ResourceKeys.Import_Chunked_CalculatingSnapshots.ToLocalized(localizedType, status.SnapshotsProcessed.Value, status.SnapshotsTotal.Value);
        }

        if (state == ChunkedImportStateEnum.Completed && status.TotalMovements != null && status.TotalChunks != null)
        {
            return ResourceKeys.Import_Chunked_CompletedSummary.ToLocalized(status.TotalMovements.Value, status.TotalChunks.Value);
        }

        if (state == ChunkedImportStateEnum.Failed)
        {
            return ResourceKeys.Import_Chunked_State_Failed.ToLocalized();
        }

        if (state == ChunkedImportStateEnum.Cancelled)
        {
            return ResourceKeys.Import_Chunked_State_Cancelled.ToLocalized();
        }

        return $"Import_Chunked_State_{state}".ToLocalized();
    }

    //public static string ToDetailMessage(this CurrentChunkedImportStatus status)
    //{
    //    if (status.ChunkStartDate != null && status.ChunkEndDate != null)
    //    {
    //        return LocalizationResourceManager.Instance.GetString("Import_Chunked_ChunkDateRange", status.ChunkStartDate, status.ChunkEndDate);
    //    }

    //    if (status.CurrentPhase != null)
    //    {
    //        return status.ToLocalizedPhase();
    //    }

    //    return string.Empty;
    //}

    public static string ToChunkInfo(this CurrentChunkedImportStatus status)
    {
        if (status.ChunkNumber != null && status.TotalChunks != null)
        {
            return $"Chunk {status.ChunkNumber.Value} / {status.TotalChunks.Value}";
        }
        return string.Empty;
    }

    //public static string ToTimeRemainingText(this CurrentChunkedImportStatus status)
    //{
    //    if (!status.EstimatedTimeRemaining != null)
    //        return string.Empty;

    //    var formatted = FormatTimeSpan(status.EstimatedTimeRemaining.Value);
    //    return LocalizationResourceManager.Instance.GetString("Import_Chunked_TimeRemaining", formatted);
    //}

    //public static string ToDurationText(this CurrentChunkedImportStatus status)
    //{
    //    if (!status.Duration != null)
    //        return string.Empty;

    //    var formatted = FormatTimeSpan(status.Duration.Value);
    //    return LocalizationResourceManager.Instance.GetString("Import_Chunked_CompletedDuration", formatted);
    //}

    // private static string ToLocalizedPhase(this CurrentChunkedImportStatus status)
    // {
    //    if (status.CurrentPhase == null)
    //        return string.Empty;

    //    var key = $"Import_Phase_{status.CurrentPhase}";
    //    return LocalizationResourceManager.Instance[key].ToString() ?? status.CurrentPhase;
    // }

    private static string ToLocalizedSnapshotType(this CurrentChunkedImportStatus status)
    {
        if (status.SnapshotType == null)
            return string.Empty;

        var key = $"Import_SnapshotType_{status.SnapshotType.Value.Replace(" ", "")}";
        return LocalizationResourceManager.Instance[key].ToString() ?? status.SnapshotType.Value;
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
        {
            if (timeSpan.TotalSeconds < 5)
                return ResourceKeys.Time_Format_LessThanMinute.ToLocalized();

            return ResourceKeys.Time_Format_Seconds.ToLocalized((int)timeSpan.TotalSeconds);
        }
        else if (timeSpan.TotalMinutes < 60)
        {
            return ResourceKeys.Time_Format_Minutes.ToLocalized((int)timeSpan.TotalMinutes);
        }
        else
        {
            return ResourceKeys.Time_Format_Hours.ToLocalized(timeSpan.TotalHours.ToString("F1"));
        }
    }

    public static string ToLocalized(this string key) => LocalizationResourceManager.Instance.GetString(key);

    public static string ToLocalized(this string key, params object[] args) => LocalizationResourceManager.Instance.GetString(key, args);
}