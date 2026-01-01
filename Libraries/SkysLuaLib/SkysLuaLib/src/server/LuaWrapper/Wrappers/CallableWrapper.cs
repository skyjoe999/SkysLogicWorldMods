using System;
using System.Reflection;
using Lua;
using SkysLuaLib.Server.LuaWrapper.WrappedObjects;

namespace SkysLuaLib.Server.LuaWrapper.Wrappers;

public class CallableWrapper(Type type, Wrapper parent = null)
    : Wrapper(type, parent)
{
    public override Wrapper CreateSubWrapper(Type new_type) => new CallableWrapper(new_type, this);

    public override LuaValue Wrap(object obj) =>
        new DefinedMethod(obj as MethodInfo);
}
