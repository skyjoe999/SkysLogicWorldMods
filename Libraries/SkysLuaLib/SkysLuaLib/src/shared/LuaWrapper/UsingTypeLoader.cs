using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lua;

namespace SkysLuaLib.Shared;

public class UsingTypeLoader
{
    private static readonly Lazy<Dictionary<string, List<Type>>> _Namespaces;
    public static Dictionary<string, List<Type>> Namespaces => _Namespaces.Value;

    static UsingTypeLoader()
    {
        // Load lazily incase we want assemblies from other mods.
        _Namespaces = new(() => AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsPublic && type.Namespace is not null)
            .GroupBy(type => type.Namespace)
            .ToDictionary(group => group.Key, group => group.ToList())
        );
    }

    public static LuaFunction UsingFunc => new("using", __call);

    private static ValueTask<int> __call(LuaFunctionExecutionContext context, CancellationToken ct)
    {
        return context.ReturnTask(new Span<LuaValue>(
            [.. context.Arguments
                .ToArray()
                .Select(v => v.Read<string>())
                .Select(SetupNamespace)]
        ));

        LuaValue SetupNamespace(string name)
        {
            if (!Namespaces.TryGetValue(name, out var list))
                return LuaValue.Nil;
            if (context.State.Environment.ContainsKey(name))
                return context.State.Environment[name];
            var table = new LuaTable();
            foreach (var t in list) context.State.Environment[t.Name] = table[t.Name] = TypeName.For(t);
            context.State.Environment[name] = table;
            return table;
        }
    }
}
