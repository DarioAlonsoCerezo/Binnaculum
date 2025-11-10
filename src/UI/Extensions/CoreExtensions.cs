using Binnaculum.Core.Import;
using Microsoft.FSharp.Core;

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
                    "Import_SavingData",
                    status.RecordsProcessed.Value,
                    status.TotalRecords.Value
                );
            }
            return LocalizationResourceManager.Instance["Import_SavingData_Generic"].ToString() ?? string.Empty;
        }

        // ProcessingFile state
        if (state == ImportStateEnum.ProcessingFile)
        {
            if (status.FileName != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    "Import_ProcessingFile",
                    status.FileName.Value
                );
            }
            return LocalizationResourceManager.Instance["Import_ProcessingFile_Generic"].ToString() ?? string.Empty;
        }

        // ProcessingData state
        if (state == ImportStateEnum.ProcessingData)
        {
            if (status.RecordsProcessed != null && status.TotalRecords != null)
            {
                return LocalizationResourceManager.Instance.GetString(
                    "Import_ProcessingRecords",
                    status.RecordsProcessed.Value,
                    status.TotalRecords.Value
                );
            }
            return LocalizationResourceManager.Instance["Import_ProcessingRecords_Generic"].ToString() ?? string.Empty;
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
            return LocalizationResourceManager.Instance["Import_CalculatingSnapshots_Generic"].ToString() ?? string.Empty;
        }

        // Validating state
        if (state == ImportStateEnum.Validating)
        {
            return LocalizationResourceManager.Instance["Import_Validating"].ToString() ?? string.Empty;
        }

        // Cancelled state
        if (state == ImportStateEnum.Cancelled)
        {
            return LocalizationResourceManager.Instance["Import_Cancelled"].ToString() ?? string.Empty;
        }

        // Failed state
        if (state == ImportStateEnum.Failed)
        {
            return LocalizationResourceManager.Instance["Import_Failed"].ToString() ?? string.Empty;
        }

        // Completed state
        if (state == ImportStateEnum.Completed)
        {
            return LocalizationResourceManager.Instance["Import_Completed"].ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}