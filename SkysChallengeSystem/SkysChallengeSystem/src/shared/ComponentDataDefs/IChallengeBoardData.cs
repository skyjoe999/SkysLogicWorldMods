using JimmysUnityUtilities;
#if LW_SIDE_SERVER
#else
using LogicWorld.ClientCode;
#endif

namespace SkysChallengeSystem.Shared.ComponentDataDefs;

public interface IChallengeBoardData : CircuitBoard.IData
{
    string ChallengeFullPath { get; set; }
    byte[] RunningData { get; set; } // Doesn't need to be decoded by the client but... shrug
}

public static class IChallengeBoardDataExtension
{
    public static void SetDataDefaultValues(this IChallengeBoardData data)
    {
        data.SizeX = 2;
        data.SizeZ = 2;
        data.Color = new Color24(120, 120, 120);
        data.ChallengeFullPath = "";
        data.RunningData = [];
    }

    public static void OverridePickedUp(this IChallengeBoardData data)
    {
        data.SizeX = 2;
        data.SizeZ = 2;
        data.ChallengeFullPath = "";
        data.RunningData = [];
    }
}

#if LW_SIDE_SERVER
public class CircuitBoard
{
    public interface IData
    {
        int SizeX { get; set; }
        int SizeZ { get; set; }
        Color24 Color { get; set; }
    }
}
#endif
