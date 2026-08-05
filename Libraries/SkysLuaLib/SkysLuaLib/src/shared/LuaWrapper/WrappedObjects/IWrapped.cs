using System;
using Lua;

namespace SkysLuaLib.Shared;

public interface IWrapped : ILuaUserData
{
    object Value { get; }

    public static LuaTable GenerateDefaultTable(Type type) =>
        new()
        {
            ["__index"] = WrapperManager.GetWrapper(type).IndexFunc,
            ["__newindex"] = WrapperManager.GetWrapper(type).NewindexFunc,
            ["__tostring"] = new LuaFunction(type.Name + ":ToString",
                async (context, _) => context.Return(Callable.UnpackArgument(context.Arguments[0]).ToString())
            ),
        };
}
