using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lua;
using SkysLuaLib.Server.LuaWrapper.WrappedObjects;

namespace SkysLuaLib.Server.LuaWrapper;

public class UsingTypeLoader
{
    public static readonly Dictionary<string, List<Type>> Namespaces = new();

    static UsingTypeLoader()
    {
        var types = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(ass => ass.GetTypes());
        foreach (var type in types)
        {
            if (!type.IsPublic) continue;
            var key = type.Namespace;
            if (key is null) continue;
            if (!Namespaces.ContainsKey(key))
                Namespaces.Add(key, []);
            Namespaces[key].Add(type);
        }
    }

    public static LuaFunction usingFunc => new("using", __call);

    private static ValueTask<int> __call(LuaFunctionExecutionContext context, CancellationToken ct)
    {
        return context.ReturnTask(new Span<LuaValue>(
            context.Arguments
                .ToArray()
                .Select(v => v.Read<string>())
                .Select(SetupNamespace)
                .ToArray()
        ));

        LuaValue SetupNamespace(string name)
        {
            if (!Namespaces.TryGetValue(name, out var list) || context.State.Environment.ContainsKey(name))
                return LuaValue.Nil;
            var table = new LuaTable();
            foreach (var t in list) context.State.Environment[t.Name] = table[t.Name] = TypeName.For(t);
            context.State.Environment[name] = table;
            return table;
        }
    }
}
