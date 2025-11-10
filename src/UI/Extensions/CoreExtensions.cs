using Android.Net.Wifi.Aware;
using Binnaculum.Core.Import;
using CSharpMath;

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
        return status.Message != null ? status.Message.Value : string.Empty;
    }
}