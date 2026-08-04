using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lua;

namespace SkysLuaLib.Shared;

// TODO: Refactor out non ambiguous methods
// If anyone has a nicer solution im all ears
public class AmbiguousMethod : Callable
{
    private readonly (TypeMatcher, MethodInfo)[] InfosByTypes;
    private readonly (Type type, string name) Method;

    public AmbiguousMethod(MethodInfo[] infos, Type type) : base(null, infos[0].Name + ":__call")
    {
        Method = (type, infos[0].Name);
        InfosByTypes = infos.Select(i => (new TypeMatcher(i), i)).ToArray();
    }


    public override LuaValue call(object instance, object[] arguments)
    {
        // TODO: Add out parameters
        // TODO: Add generic parameters
        // Maybe this was a mistake?
        var types = arguments.Select(a => a?.GetType()).Prepend(instance.GetType()).ToArray();
        foreach (var (matcher, info) in InfosByTypes)
            if (matcher.Match(types))
                return WrapperManager.Wrap(matcher.IsStatic ? info.Invoke(null, [instance, .. arguments]) : info.Invoke(instance, arguments));

        throw new ArgumentException(ErrorMessage(types));
    }

    private string ErrorMessage(Type[] types)
    {
        var sb = new StringBuilder()
            .Append("Could not match arguments of type [")
            .Append(string.Join(", ", types.Select(type => type.Name)))
            .AppendLine("]")
            .Append("Candidates include: ");
        foreach (var (matcher, _) in InfosByTypes)
            sb.AppendLine().Append("\t" + matcher.ToCandidateString());
        return sb.ToString();
    }

    private readonly record struct TypeMatcher
    {
        public readonly bool IsStatic;
        private readonly int RequiredLength;
        private readonly List<ParameterInfo> ParameterList;

        public TypeMatcher(MethodInfo info)
        {
            IsStatic = info.IsStatic;
            ParameterList = [.. info.GetParameters()];
            RequiredLength = ParameterList.FindIndex(p => p.IsOptional);
            if (RequiredLength == -1) RequiredLength = ParameterList.Count;
        }


        public bool Match(Type[] types)
        {
            var count = types.Length;
            var _types = types.AsEnumerable();
            if (!IsStatic)
            {
                count -= 1;
                _types = _types.Skip(1);
            }

            if (RequiredLength > count) return false;
            if (ParameterList.Count < count) return false;

            return ParameterList.Zip(_types, (a, b) => (b, a)).All(AreCompatible);
        }
        // public bool convert(object[] args)
        // {
        //    foreach (var (i, t) in ParameterList.Zip(types, (a, b) => (a, b)))
        //         if (!AreCompatible(t, i))
        //             return false;
        //     return true;
        // }


        private static bool AreCompatible((Type t, ParameterInfo i) val)
        {
            return AreCompatible(val.t, val.i.ParameterType.IsEnum ? val.i.ParameterType.GetEnumUnderlyingType() : val.i.ParameterType);
        }

        private static bool AreCompatible(Type t, Type i)
        {
            // return t == i || t.IsSubclassOf(i);
            // return i.IsAssignableFrom(t) || (isNumeric(t) && isNumeric(i));
            return i.IsAssignableFrom(t);
        }

        // private static bool isNumeric(Type t)
        // {
        //     return t == typeof(int) ||
        //            t == typeof(float) ||
        //            t == typeof(bool) ||
        //            t == typeof(double);
        // }

        public string ToCandidateString()
        {
            var rl = RequiredLength;
            return rl + "[" + (IsStatic ? "" : "_, ") + string.Join(", ", ParameterList.Select((t, i) => (i < rl ? "" : "?") + t.ParameterType.Name)) + "]";
        }
    }
}
