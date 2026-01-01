using System;
using System.Threading.Tasks;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper;

public static class LuaValueExtension
{
    // Why isn't this the normal implementation???
    // (Because it wants me to be using async is why)
    public static ValueTask<int> ReturnTask(this LuaFunctionExecutionContext context)
        => new ValueTask<int>(context.Return());

    public static ValueTask<int> ReturnTask(this LuaFunctionExecutionContext context, LuaValue result)
        => new ValueTask<int>(context.Return(result));

    public static ValueTask<int> ReturnTask(this LuaFunctionExecutionContext context, LuaValue r0, LuaValue r1)
        => new ValueTask<int>(context.Return(r0, r1));

    public static ValueTask<int> ReturnTask(this LuaFunctionExecutionContext context,
        LuaValue r0,
        LuaValue r1,
        LuaValue r2
    ) => new ValueTask<int>(context.Return(r0, r1, r2));

    public static ValueTask<int> ReturnTask(this LuaFunctionExecutionContext context, ReadOnlySpan<LuaValue> results)
        => new ValueTask<int>(context.Return(results));
}