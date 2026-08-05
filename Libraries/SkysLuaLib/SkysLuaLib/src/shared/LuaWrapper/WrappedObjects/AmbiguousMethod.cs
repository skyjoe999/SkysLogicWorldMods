using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lua;

namespace SkysLuaLib.Shared;

// TODO: Add "SemiAmbiguousMethod" which checks argument count first.
// If anyone has a nicer solution im all ears
public class AmbiguousMethod(string MethodName, Type BaseType) : Callable(null, MethodName + ":__call")
{
    private readonly Dictionary<Type[], (MethodBase method, Func<object[], object[]> conversion)> MethodsByTypes = new(new TypeMatcher());
    private readonly bool IsConstructor = MethodName == BaseType.Name;

    public override LuaValue Call(object instance, object[] arguments)
    {
        // TODO: Add out parameters
        // TODO: Add generic parameters
        // Maybe this was a mistake?
        var types = arguments
            .Select(argument => argument?.GetType() ?? typeof(object))
            .ToArray();

        if (!MethodsByTypes.TryGetValue(types, out var value))
            value = MethodsByTypes[types] =
                GetMethodFor(types) is { } m ? (m, null) :
                GetMethodByAliasing() ??
                throw new ArgumentException(ErrorMessage(types));

        if (value.conversion is not null)
            arguments = value.conversion(arguments);

        return WrapperManager.Wrap(value.method is ConstructorInfo constructor
            ? constructor.Invoke(arguments)
            : value.method.Invoke(instance, arguments));

        (MethodBase method, Func<object[], object[]>)? GetMethodByAliasing()
        {
            var aliasTypes = types.Select(type => type == typeof(double) || type == typeof(float) ? typeof(int) : type).ToArray();
            if (GetMethodFor(aliasTypes) is not { } func)
                return null;

            var doConversion = aliasTypes.Zip(types, (a, b) => a != b ? a : null).ToArray();

            return (func, (args) => [.. args.Zip(doConversion, (arg, type) => type is not null ? Convert.ChangeType(arg, type) : arg)]);
        }

        MethodBase GetMethodFor(Type[] types) => !IsConstructor
            ? BaseType.GetMethod(MethodName, types)
            : BaseType.GetConstructor(types);
    }

    private string ErrorMessage(Type[] types)
    {
        var sb = new StringBuilder()
            .Append("Could not match arguments of type [")
            .Append(string.Join(", ", types.Select(type => type.Name)))
            .AppendLine("]")
            .Append("Candidates include: ");
        foreach (var info in false ? BaseType.GetMethods().Where(info => info.Name == Name).Cast<MethodBase>() : BaseType.GetConstructors())
            sb.AppendLine().Append("\t" + CandidateString(info));
        return sb.ToString();
    }

    private static string CandidateString(MethodBase info)
    {
        var ParameterList = info.GetParameters().ToList();
        var RequiredLength = ParameterList.FindIndex(p => p.IsOptional);
        if (RequiredLength == -1) RequiredLength = ParameterList.Count;
        return RequiredLength + "[" + (info.IsStatic ? "" : "_, ") + string.Join(", ", ParameterList.Select((t, i) => (i < RequiredLength ? "" : "?") + t.ParameterType.Name)) + "]";
    }

    private class TypeMatcher : IEqualityComparer<Type[]>
    {
        public bool Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
        public int GetHashCode(Type[] obj) => obj.Aggregate(0, HashCode.Combine);
    }
}
