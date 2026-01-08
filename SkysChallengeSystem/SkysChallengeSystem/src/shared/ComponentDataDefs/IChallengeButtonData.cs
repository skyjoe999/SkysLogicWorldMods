using System;
using JimmysUnityUtilities;
using LogicWorld.SharedCode.ComponentCustomData;

namespace SkysChallengeSystem.Shared.ComponentDataDefs;

public enum ButtonTypes
{
    Start,
    Cancel,
}

public interface IChallengeButtonData : IButtonData
{
    ButtonTypes ButtonType { get; set; }
}

public static class IChallengeButtonExtension
{
    public static readonly Color24 StartColor = new(60, 200, 60);
    public static readonly Color24 CancelColor = new(200, 60, 60);
    public static readonly Color24 StartIconColor = new(30, 158, 30);
    public static readonly Color24 CancelIconColor = new(157, 25, 22);

    public static void SetDataDefaultValues(this IChallengeButtonData data)
    {
        data.ButtonType = ButtonTypes.Start;
        data.ButtonColor = ButtonTypes.Start.ToColor();
        data.ButtonDown = false;
    }

    public static Color24 ToColor(this ButtonTypes type) =>
        type switch
        {
            ButtonTypes.Start => StartColor,
            ButtonTypes.Cancel => CancelColor,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static string ToIcon(this ButtonTypes type) =>
        type switch
        {
            ButtonTypes.Start => "f04b",
            ButtonTypes.Cancel => "f0e2",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static Color24 ToIconColor(this ButtonTypes type) =>
        type switch
        {
            ButtonTypes.Start => StartIconColor,
            ButtonTypes.Cancel => CancelIconColor,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
