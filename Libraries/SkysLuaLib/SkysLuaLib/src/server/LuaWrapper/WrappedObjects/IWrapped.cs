using System;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

public interface IWrapped : ILuaUserData
{
    object value { get; }

    public static LuaTable GenerateDefaultTable(Type type) =>
        new()
        {
            ["__index"] = WrapperManager.GetWrapper(type).__index,
            ["__newindex"] = WrapperManager.GetWrapper(type).__newindex,
            ["__tostring"] = new LuaFunction(type.Name + ":ToString",
                (context, _) =>
                    context.ReturnTask(Callable.unpackArgument(context.Arguments[0]).ToString())
            ),
        };
}
