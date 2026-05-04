using LogicWorld.SharedCode.ExtraData;

namespace SkysColorfulClusters.Shared;

public static class ClusterColorExtraDataManager
{
    private static ExtraDataAccessor<string> ExtraData;
    private static byte[] ExtraDataBytes
    {
        get => System.Convert.FromBase64String(ExtraData.Data);
        set => ExtraData?.SetData(System.Convert.ToBase64String(value));
    }

    public static void SetupExtraData(ExtraData extraData)
    {
        if (ExtraData is not null)
            return;
        ExtraData = extraData.GetDataAccessor("SkysColorfulClusters.ClusterColors", "");
        ExtraData.RunAsSoonAsDataAvailable(_ => ReadFromExtraData());
    }

    public static void ReadFromExtraData() => ClusterColorManager.DeserializeData(ExtraDataBytes);
    public static void WriteToExtraData() => ExtraDataBytes = ClusterColorManager.SerializeData();
}
