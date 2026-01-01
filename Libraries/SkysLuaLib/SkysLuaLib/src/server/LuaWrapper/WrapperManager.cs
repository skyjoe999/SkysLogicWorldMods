using System;
using System.Collections.Generic;
using System.Reflection;
using Lua;
using SkysLuaLib.Server.LuaWrapper.Wrappers;

namespace SkysLuaLib.Server.LuaWrapper;

public static class WrapperManager
{
    private static readonly Dictionary<Type, Wrapper> loadedTypes = new();

    static WrapperManager()
    {
        // Non-wrappable types (ie. are already wrapped by LuaValue)
        _ = new NonWrappable<NullType>();
        _ = new NonWrappable<int>();
        _ = new NonWrappable<long>();
        _ = new NonWrappable<float>();
        _ = new NonWrappable<double>();
        _ = new NonWrappable<string>();
        _ = new NonWrappable<bool>();
        _ = new NonWrappable<LuaValue>();
        _ = new NonWrappable<LuaTable>();
        _ = new NonWrappable<LuaFunction>();
        _ = new NonWrappable<LuaState>();
        _ = new NonWrappable<ILuaUserData>();
        // Callables get special treatment
        _ = new CallableWrapper(typeof(MethodInfo));
    }

    public static Wrapper GetWrapper(Type type)
    {
        return loadedTypes.TryGetValue(type ?? typeof(NullType), out var value)
            ? value
            : GetWrapper(type!.BaseType).CreateSubWrapper(type);
    }

    public static LuaValue Wrap(object obj) => GetWrapper(obj?.GetType()).Wrap(obj!);
    public static LuaValue Wrap(object obj, Type type) => GetWrapper(type).Wrap(obj!);

    public static void RegisterWrapper(Type type, Wrapper wrapper) => loadedTypes.Add(type, wrapper);
    public static void RegisterWrapper(Wrapper wrapper) => loadedTypes.Add(wrapper.type, wrapper);

    private class NullType; // Because null isn't really a type so it can't be a key

    private class NonWrappable<T>() : Wrapper(typeof(T))
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override LuaValue Wrap(object obj) => LuaValue.FromObject(obj);
    }
}
