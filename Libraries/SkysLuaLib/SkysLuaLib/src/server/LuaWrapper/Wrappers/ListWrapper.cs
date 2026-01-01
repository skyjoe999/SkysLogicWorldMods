using System;
using System.Collections;
using SkysLuaLib.Server.LuaWrapper.WrappedObjects;

namespace SkysLuaLib.Server.LuaWrapper.Wrappers;

public class ListWrapper(Type type, Wrapper parent = null)
    : ReadableListWrapper(type, parent)
{
    public override Wrapper CreateSubWrapper(Type new_type) => new ListWrapper(new_type, this);

    protected override bool TryInstanceSetter(IWrapped obj, object key, object value, out Exception exception)
    {
        exception = null;

        if (!IsIndex(key, out var _key)
            || obj.value is not IList list
            || IsOOB(obj, _key, list, key)) return false;

        list[_key] = value;
        return true;
    }
}
