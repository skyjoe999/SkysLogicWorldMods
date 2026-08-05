using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicLog;
using Lua;

namespace SkysLuaLib.Shared;

public abstract class Callable : Wrapped
{
    public readonly string Name = "anon";

    protected Callable(object value) : base(value)
    {
    }

    protected Callable(object value, string name) : base(value) =>
        Metatable!["__call"] = new LuaFunction(Name = name, Call);

    public abstract LuaValue Call(object instance, object[] arguments);

    public virtual async ValueTask<int> Call(
        LuaFunctionExecutionContext context,
        CancellationToken ct
    )
    {
        return context.GetArgument<Callable>(0).TryCall(
                context.HasArgument(1) ? UnpackArgument(context.GetArgument(1)) : null,
                context.HasArgument(2) ? UnpackArguments(context.Arguments[2..]) : [],
                out var ret,
                out var exception
            )
            ? context.Return(ret)
            : throw HandleException(context, exception, ct);
    }
    protected static Exception HandleException(
        LuaFunctionExecutionContext context,
        Exception exception,
        CancellationToken ct
    )
    {
        if (context.State.Environment.TryGetValue("Logger", out var luaValue)
            && luaValue.TryRead<ILogicLogger>(out var Logger))
        {
            Logger.Exception(exception);
            return new("See Logger for details");
        }
        return exception;
    }

    public bool TryCall(object instance, object[] arguments, out LuaValue ret, out Exception exception)
    {
        try
        {
            ret = Call(instance, arguments);
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

    public static object UnpackArgument(LuaValue Argument)
        => Argument.TryRead(out IWrapped wrapper)
            ? wrapper.Value
            : Argument.Type == LuaValueType.Boolean
                ? Argument.Read<bool>()
                : Argument.TryRead<object>(out var result)
                    ? result
                    : null;

    public static object[] UnpackArguments(ReadOnlySpan<LuaValue> Arguments)
        => [.. Arguments.ToArray().Select(UnpackArgument)];
}
