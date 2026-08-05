using System.Linq;
using JimmysUnityUtilities;
using Lua;

namespace SkysLuaLib.Shared;

public static class LuaPretty
{
    public static string Pretty(this LuaValue value, int depth = -1)
    {
        switch (value.Type)
        {
            case LuaValueType.Table:
                value.TryRead<LuaTable>(out var t);
                return depth != 0 ? t.Pretty(depth - 1) : t.ToString();
            case LuaValueType.Function:
                value.TryRead<LuaFunction>(out var f);
                return f.Pretty();
            case LuaValueType.UserData:
                return "userdata : " + value.Read<object>();
            case LuaValueType.LightUserData:
            case LuaValueType.Nil:
            case LuaValueType.Boolean:
            case LuaValueType.String:
            case LuaValueType.Number:
            case LuaValueType.Thread:
            default:
                return value.ToString();
        }
    }

    public static string Pretty(this LuaTable t, int depth = -1)
    {
        if (t.IsEmpty()) return "{}";
        return "{" + t.ToArray()
            .Convert(p => p.Key.Pretty(depth) + " = " + p.Value.Pretty(depth))
            .Aggregate((s1, s2) => s1 + ", " + s2) + "}";
    }

    public static string Pretty(this LuaFunction f)
    {
        return "function(" + f.Name + ")";
    }
}
