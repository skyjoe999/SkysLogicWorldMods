using System.Reflection;
using Lua;

namespace SkysLuaLib.Shared;

public class DefinedMethod(MethodInfo info) : Callable(info, info.Name + ":__call")
{
    public override LuaValue Call(object instance, object[] arguments) =>
       WrapperManager.Wrap(info.Invoke(instance, arguments));
}
