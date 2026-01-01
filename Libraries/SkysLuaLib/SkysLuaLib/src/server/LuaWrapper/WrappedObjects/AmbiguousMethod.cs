using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

// TODO: Refactor out non ambiguous methods
// If anyone has a nicer solution im all ears
public class AmbiguousMethod : Callable
{
    private readonly (TypeMatcher, MethodInfo)[] InfosByTypes;

    public AmbiguousMethod(MethodInfo[] infos) : base(null, infos[0].Name + ":__call")
    {
        InfosByTypes = infos.Select(i => (new TypeMatcher(i), i)).ToArray();
    }


    public override LuaValue call(object instance, object[] arguments)
    {
        // TODO: Add out parameters
        // TODO: Add generic parameters
        // Maybe this was a mistake?
        var types = arguments.Select(a => a?.GetType()).ToArray();
        foreach (var (matcher, info) in InfosByTypes)
            if (matcher.match(types))
                return WrapperManager.Wrap(info.Invoke(instance, arguments));
        throw new ArgumentException(errorMessage(types));
    }

    private string errorMessage(Type[] types)
    {
        var sb = new StringBuilder()
            .Append("Could not match arguments of type ")
            .Append(types.AsListString(a => a.Name))
            .AppendLine()
            .Append("Candidates include: ");
        foreach (var (matcher, _) in InfosByTypes) sb.AppendLine().Append("\t" + matcher.ToCandidateString());
        return sb.ToString();
    }

    private readonly record struct TypeMatcher
    {
        private readonly int requiredLength;
        private readonly List<ParameterInfo> ParameterList;

        public TypeMatcher(MethodInfo info)
        {
            ParameterList = info.GetParameters().ToList();
            requiredLength = ParameterList.FindIndex(p => p.IsOptional);
            if (requiredLength == -1) requiredLength = ParameterList.Count;
        }


        public bool match(Type[] types)
        {
            if (requiredLength > types.Length) return false;
            if (ParameterList.Count < types.Length) return false;
            foreach (var (i, t) in ParameterList.Zip(types, (a, b) => (a, b)))
                if (!AreCompatible(t, i))
                    return false;
            return true;
        }
        // public bool convert(object[] args)
        // {
        //    foreach (var (i, t) in ParameterList.Zip(types, (a, b) => (a, b)))
        //         if (!AreCompatible(t, i))
        //             return false;
        //     return true;
        // }


        private static bool AreCompatible(Type t, ParameterInfo i)
        {
            return AreCompatible(t, i.ParameterType.IsEnum ? i.ParameterType.GetEnumUnderlyingType() : i.ParameterType);
        }

        private static bool AreCompatible(Type t, Type i)
        {
            // return t == i || t.IsSubclassOf(i);
            // return t.IsAssignableTo(i) || (isNumeric(t) && isNumeric(i));
            return t.IsAssignableTo(i);
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
            var rl = requiredLength;
            return rl + ParameterList.Select((t, i) => (i < rl ? "" : "?") + t.ParameterType.Name).AsListString();
        }
    }
}
