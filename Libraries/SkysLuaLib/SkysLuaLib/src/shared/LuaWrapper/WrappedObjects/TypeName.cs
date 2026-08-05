using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lua;

namespace SkysLuaLib.Shared;

public class TypeName : Callable
{
    public readonly Type BaseType;
    public readonly AmbiguousMethod ConstructorMethod;
    private static readonly Dictionary<Type, TypeName> LoadedTypeNames = [];

    private TypeName(Type baseType) : base(null, baseType.Name)
    {
        BaseType = baseType;
        ConstructorMethod = new(BaseType.Name, BaseType);
        // TODO: inner types
        var wrapper = WrapperManager.GetWrapper(BaseType);
        Metatable["__index"] = new LuaFunction(baseType.Name + ":__index", async (context, ct) =>
        {
            if (UnpackArgument(context.Arguments[1]) as string == "typeof")
                return context.Return(WrapperManager.Wrap(BaseType));
            return context.Return(await context.State.CallAsync(wrapper.IndexFunc, context.Arguments, ct));
        });
        Metatable["__newindex"] = wrapper.NewindexFunc;
    }

    public static TypeName For<T>() => For(typeof(T));

    public static TypeName For(Type type) => LoadedTypeNames.GetValueOrDefault(type) ?? (LoadedTypeNames[type] = new TypeName(type));

    public override async ValueTask<int> Call(
        LuaFunctionExecutionContext context,
        CancellationToken ct
    )
    {
        return context.GetArgument<Callable>(0).TryCall(
                null,
                context.HasArgument(1) ? UnpackArguments(context.Arguments[1..]) : [],
                out var ret,
                out var exception
            )
            ? context.Return(ret)
            : throw HandleException(context, exception, ct);
    }

    public override LuaValue Call(object instance, object[] arguments)
    {
        try
        {
            return WrapperManager.Wrap(Activator.CreateInstance(BaseType, arguments));
        }
        catch (MissingMethodException)
        {
            return ConstructorMethod.Call(instance, arguments);
        }
    }

    public override string ToString() => $"{GetType().Name}({BaseType.Name})";
}
