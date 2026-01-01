using System.Linq;
using JimmysUnityUtilities;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper;

public static class LuaPretty
{
    public static string Pretty(this LuaValue v, int depth = -1)
    {
        switch (v.Type)
        {
            case LuaValueType.Table:
                v.TryRead<LuaTable>(out var t);
                return depth != 0 ? t.Pretty(depth - 1) : t.ToString();
            case LuaValueType.Function:
                v.TryRead<LuaFunction>(out var f);
                return f.Pretty();
            case LuaValueType.UserData:
                return "userdata : " + v.Read<object>();
            case LuaValueType.LightUserData:
            case LuaValueType.Nil:
            case LuaValueType.Boolean:
            case LuaValueType.String:
            case LuaValueType.Number:
            case LuaValueType.Thread:
            default:
                return v.ToString();
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
