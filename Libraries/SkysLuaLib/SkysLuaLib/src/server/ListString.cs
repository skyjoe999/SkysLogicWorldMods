using System;
using System.Collections.Generic;
using System.Linq;

namespace SkysLuaLib.Server;

// Internal because its really only for me
internal static class ListString
{
  // Why AsListString and not ToListString? Purely for autocomplete reasons
  public static string AsListString(this IEnumerable<string> obj)
  {
    var iEnumerable = obj as string[] ?? obj.ToArray();
    if (iEnumerable.Length == 0)
      return "[]";
    return "[" + iEnumerable.Aggregate((a, b) => a + ", " + b) + "]";
  }

  public static string AsListString<T>(this IEnumerable<T> obj) => obj.Select(i => i.ToString()).AsListString();

  public static string AsListString<T>(this IEnumerable<T> obj, Func<T, string> func) =>
    obj.Select(func).AsListString();

  public static string AsListString<T, R>(this IEnumerable<T> obj, Func<T, R> func) =>
    obj.AsListString(i => func(i).ToString());
}
