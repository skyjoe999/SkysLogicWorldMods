// #define ErrorOnOOB // If not, OOB reads will return nil

using System;
using System.Collections;
using Lua;
using SkysLuaLib.Server.LuaWrapper.WrappedObjects;

namespace SkysLuaLib.Server.LuaWrapper.Wrappers;

public class ReadableListWrapper(Type type, Wrapper parent = null)
    : Wrapper(type, parent)
{
    public override Wrapper CreateSubWrapper(Type new_type) => new ReadableListWrapper(new_type, this);

    protected override bool TryInstanceGetter(IWrapped obj, object key, out LuaValue ret)
    {
        ret = LuaValue.Nil;
        if (!IsIndex(key, out var _key)
            || obj.value is not IList list
            || IsOOB(obj, _key, list, key)) return false;

        ret = WrapperManager.Wrap(list[_key - 1]);
        return true;
    }

    public static bool IsIndex(object value) => IsIndex(value, out _);

    public static bool IsIndex(object value, out int index)
    {
        try
        {
            index = Index(value);
            return true;
        }
        catch (ArgumentException)
        {
            index = 0;
            return false;
        }
    }

    public static int Index(object value)
    {
        return value switch
        {
            double d => (int)Math.Floor(d),
            float f => (int)Math.Floor(f),
            bool b => b ? 1 : 0,
            int i => i,
            _ => throw new ArgumentException($"Value '{value}' of type '{value.GetType().Name} is not a valid index")
        };
    }

    // No project wide compiler flags '^'
    protected static bool IsOOB(IWrapped obj, int _key, IList list, object key)
    {
#if ErrorOnOOB
        if (_key <= 0 || _key > list.Count) throw IndexError(obj, key);
#else
        return _key <= 0 || _key > list.Count;
#endif
    }
}
