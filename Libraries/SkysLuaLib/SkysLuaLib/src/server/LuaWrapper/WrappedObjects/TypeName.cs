using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

public class TypeName : Callable
{
    public readonly Type type;
    private static readonly Dictionary<Type, TypeName> loadedTypeNames = new();

    private TypeName(Type type) : base(null, type.Name)
    {
        this.type = type;
        var wrapper = WrapperManager.GetWrapper(type);
        Metatable!["__index"] = wrapper.__index;
        Metatable!["__newindex"] = wrapper.__newindex;
    }

    public static TypeName For<T>() => For(typeof(T));

    public static TypeName For(Type type)
    {
        return loadedTypeNames.TryGetValue(type, out var tn)
            ? tn
            : loadedTypeNames[type] = new TypeName(type);
    }


    public override LuaValue call(object instance, object[] arguments) => call([instance, ..arguments]);

    public LuaValue call(in object[] arguments)
    {
        try
        {
            return WrapperManager.Wrap(Activator.CreateInstance(type, arguments));
        }
        catch (MissingMethodException)
        {
            var constructor = type.GetConstructor(arguments.Select(o => o.GetType()).ToArray());
            if (constructor is not null)
                return WrapperManager.Wrap(constructor.Invoke(arguments));

            // Error time
            var sb = new StringBuilder()
                .Append("Could not match arguments ")
                .Append(arguments.AsListString(a => $"({a.GetType()}){a}"))
                .AppendLine()
                .Append("Candidates include: ");
            foreach (var i in type.GetConstructors())
                sb.AppendLine().Append("\t" + i.GetParameters().AsListString(p => p.ParameterType.Name));
            throw new ArgumentException(sb.ToString());
        }
    }

    public override string ToString() => $"{GetType().Name}({type.Name})";
}
