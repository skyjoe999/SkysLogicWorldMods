using System.Reflection;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

public class DefinedMethod(MethodInfo info) : Callable(info, info.Name + ":__call")
{
  public override LuaValue call(object instance, object[] arguments) =>
    WrapperManager.Wrap(info.Invoke(instance, arguments));
}
