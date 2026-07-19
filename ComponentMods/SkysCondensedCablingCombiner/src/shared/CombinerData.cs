namespace SkysCondensedCablingCombiner.Shared;

public interface ICombinerData
{
    int BitsPerInput { get; set; }
}

public static class ICombinerDataExtension
{
    public static void Initialize(this ICombinerData data) => data.BitsPerInput = 1;
}

