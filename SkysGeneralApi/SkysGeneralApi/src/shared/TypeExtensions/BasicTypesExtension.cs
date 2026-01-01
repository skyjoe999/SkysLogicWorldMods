using System.Collections.Generic;
using System.Linq;

namespace SkysGeneralLib.Shared.TypeExtensions;

public static class BasicTypesExtension
{
    public static string format(this IDictionary<object, object> dict)
    {
        return dict.Keys
            .ToArray()
            .Zip(dict.Values, (a, b) => a + ":" + b)
            .ToList().Aggregate((a, b) => a + ", " + b);
    }

    public static string format(this byte[] self, int maxItems = 10)
    {
        if (self.Length == 0) return "[]";
        return "[" +
               (self.Length < maxItems
                   ? self.ToList()
                   : self.ToList().GetRange(0, maxItems))
               .ConvertAll((a) => "0123456789ABCDEF"[a / 16].ToString() + "0123456789ABCDEF"[a % 16])
               .Aggregate((a, b) => a + ", " + b)
               + (self.Length > maxItems ? ", +" + (self.Length - maxItems) + " more]" : "]");
    }

    public static string Aggregate(
        this IEnumerable<string> obj,
        string sep = ", ",
        string prefix = "",
        string postfix = ""
    )
    {
        var iEnumerable = obj as string[] ?? obj.ToArray();
        if (iEnumerable.Length == 0)
            return prefix + postfix;
        return prefix + iEnumerable.Aggregate((a, b) => a + sep + b) + postfix;
    }
}
