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
}