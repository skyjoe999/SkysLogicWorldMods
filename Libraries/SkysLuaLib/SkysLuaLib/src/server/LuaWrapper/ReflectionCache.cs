using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Lua;
using SkysLuaLib.Server.LuaWrapper.WrappedObjects;

namespace SkysLuaLib.Server.LuaWrapper.ReflectionCache;

public interface CachedLookup
{
    LuaValue Get(object obj);
    void Set(object obj, object value);
}

public class FieldLookup(FieldInfo field) : CachedLookup
{
    // Could I compile it? Yes. Does that matter when compared to the rest of this hackery? NO!
    public LuaValue Get(object obj) => WrapperManager.Wrap(field.GetValue(obj), field.FieldType);
    public void Set(object obj, object value) => field.SetValue(obj, value);

    public static CachedLookup Cache(string key, Type type) =>
        type.GetField(key) is { } info ? new FieldLookup(info) : null;
}

public class PropertyLookup(PropertyInfo property) : CachedLookup
{
    private readonly MethodInfo GetMethod = property.GetGetMethod();
    private readonly MethodInfo SetMethod = property.GetSetMethod();
    public LuaValue Get(object obj) => WrapperManager.Wrap(GetMethod.Invoke(obj, []), property.PropertyType);
    public void Set(object obj, object value) => SetMethod.Invoke(obj, [value]);

    public static CachedLookup Cache(string key, Type type) =>
        type.GetProperty(key) is { } info ? new PropertyLookup(info) : null;
}

public class MethodLookup(Callable method) : CachedLookup
{
    public readonly Callable Method = method;
    public LuaValue Get(object obj) => Method;

    public void Set(object obj, object value) =>
        throw new ReadOnlyException($"Cannot set method '{Method.Name}'");

    public static CachedLookup Cache(string key, Type type)
    {
        try
        {
            var info = type.GetMethod(key);
            if (info is not null) return new MethodLookup(new DefinedMethod(info));
        }
        catch (AmbiguousMatchException)
        {
            var infos = type.GetMethods().Where(info => info.Name == key).ToArray();
            if (infos.Length != 0) return new MethodLookup(new AmbiguousMethod(infos));
        }

        return null;
    }
}
