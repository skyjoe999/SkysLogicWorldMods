using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicLog;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

public abstract class Callable : Wrapped
{
    public readonly string Name = "anon";

    protected Callable(object value) : base(value)
    {
    }

    protected Callable(object value, string name)
        : base(value) =>
        Metatable!["__call"] = new LuaFunction(Name = name, __call);

    public abstract LuaValue call(object instance, object[] arguments);

    public virtual ValueTask<int> __call(
        LuaFunctionExecutionContext context,
        CancellationToken ct
    )
    {
        if (context.GetArgument<Callable>(0).TryCall(
                unpackArgument(context.GetArgument(1)),
                unpackArguments(context.Arguments[2..]),
                out var ret,
                out var exception
            ))
            return context.ReturnTask(ret);
        if (context.State.Environment.TryGetValue("Logger", out var luaValue)
            && luaValue.TryRead<ILogicLogger>(out var Logger))
            Logger.Exception(exception);
        throw exception;
    }

    public bool TryCall(object instance, object[] arguments, out LuaValue ret, out Exception exception)
    {
        try
        {
            ret = call(instance, arguments);
            exception = null;
            return true;
        }
        catch (Exception e)
        {
            ret = LuaValue.Nil;
            exception = e;
            return false;
        }
    }

    public static object unpackArgument(LuaValue Argument)
        => Argument.TryRead(out IWrapped wrapper)
            ? wrapper.value
            : Argument.Type == LuaValueType.Boolean
                ? Argument.Read<bool>()
                : Argument.TryRead<object>(out var result)
                    ? result
                    : null;

    public static object[] unpackArguments(ReadOnlySpan<LuaValue> Arguments)
        => Arguments.ToArray().Select(unpackArgument).ToArray();
}
