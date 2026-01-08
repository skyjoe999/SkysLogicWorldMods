using JimmysUnityUtilities;

namespace SkysChallengeSystem.Shared.ComponentDataDefs;

public interface IChallengeDisplayData
{
    const float ChallengeDisplayMaxFontSize = 5.0f;
    const float ChallengeDisplayMinFontSize = 0.3f;
    const float ChallengeDisplayFontSizeStep = 0.1f;
    string DisplayText { get; set; }
    bool IsError { get; set; }
    float FontSize { get; set; }
    int SizeX { get; set; }
    int SizeY { get; set; }
    const string SuperSecretTemporaryString = "__IsDisplay__"; // Temporary quick fix until UI rework
}

public static class IChallengeDisplayDataExtension
{
    public static readonly Color24 TextColor = new(200, 200, 200);
    public static readonly Color24 ErrorColor = new(200, 40, 40);

    public static void SetDataDefaultValues(this IChallengeDisplayData data)
    {
        data.DisplayText = "";
        data.IsError = false;
        data.FontSize = 0.8f;
        data.SizeX = 1;
        data.SizeY = 1;
    }

    public static void OverridePickedUp(this IChallengeDisplayData data)
    {
        data.DisplayText = "";
        data.IsError = false;
    }

    public static Color24 ToColor(this IChallengeDisplayData data)
    {
        return data.IsError ? ErrorColor : TextColor;
    }
}
