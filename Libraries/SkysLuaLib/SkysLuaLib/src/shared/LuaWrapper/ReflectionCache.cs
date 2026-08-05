using System;
using System.Linq;
using System.Reflection;
using Lua;

namespace SkysLuaLib.Shared;

public interface ICachedLookup
{
    LuaValue Get(object obj);
    void Set(object obj, object value);
}

public class FieldLookup(FieldInfo field) : ICachedLookup
{
    // Could I compile it? Yes. Does that matter when compared to the rest of this hackery? NO!
    public LuaValue Get(object obj) => WrapperManager.Wrap(field.GetValue(obj), field.FieldType);
    public void Set(object obj, object value) => field.SetValue(obj, value);

    public static ICachedLookup Cache(string key, Type type) =>
        type.GetField(key) is { } info ? new FieldLookup(info) : null;
}

public class PropertyLookup(PropertyInfo property) : ICachedLookup
{
    private readonly MethodInfo GetMethod = property.GetGetMethod();
    private readonly MethodInfo SetMethod = property.GetSetMethod();
    public LuaValue Get(object obj) => WrapperManager.Wrap(GetMethod.Invoke(obj, []), property.PropertyType);
    public void Set(object obj, object value) => SetMethod.Invoke(obj, [value]);

    public static ICachedLookup Cache(string key, Type type) =>
        type.GetProperty(key) is { } info ? new PropertyLookup(info) : null;
}

public class MethodLookup(Callable method) : ICachedLookup
{
    public readonly Callable Method = method;
    public LuaValue Get(object obj) => Method;

    public void Set(object obj, object value) =>
        throw new($"Cannot set method '{Method.Name}'");

    public static ICachedLookup Cache(string key, Type type)
    {
        try
        {
            return !(type.GetMethod(key) is { } info) ? null
                : new MethodLookup(new DefinedMethod(info));
        }
        catch (AmbiguousMatchException)
        {
            return !type.GetMethods().Any(info => info.Name == key) ? null
                : new MethodLookup(new AmbiguousMethod(key, type));
        }
    }
}
