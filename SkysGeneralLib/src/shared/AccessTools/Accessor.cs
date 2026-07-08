using System;
using System.Linq.Expressions;
using System.Reflection;
using EccsLogicWorldAPI.Shared.AccessHelper;

namespace SkysGeneralLib.Shared.AccessTools;

public class Accessor<T, R>
{
    private readonly Func<T, R> Getter;
    private readonly Action<T, R> Setter;
    public Accessor(MemberInfo info)
    {
        // TODO: check if R type can be assigned to member type for error prevention
        //       (possibly even add a casting conversion when possible! (or just when typeof(R) == typeof(object)))
        //       or just encase the setter in a try
        (Getter, Setter) = info switch
        {
            PropertyInfo i => (Delegator.createPropertyGetter<T, R>(i), i.CanWrite ? Delegator.createPropertySetter<T, R>(i) : DummyWrite(i)),
            FieldInfo i => (Delegator.createFieldGetter<T, R>(i), !i.IsInitOnly ? Delegator.createFieldSetter<T, R>(i) : DummyWrite(i)),
            _ => throw new ArgumentException($"AccessHelper.Accessor can only handle infos that inherit {nameof(PropertyInfo)} or {nameof(FieldInfo)}, not {info?.GetType().Name ?? "null"}")
        };
    }
    private static Action<T,R> DummyWrite(MemberInfo info) => (_, _) => throw new Exception($"Member {info.Name} in type {info.DeclaringType.Name} does not support writing");

    public Accessor(Type type, string name) : this((MemberInfo)
        type.GetField(name, Bindings.any) ??
        type.GetProperty(name, Bindings.any) ??
        throw new AccessHelperException($"Could not find member '{name}' in {type.Name}")
    )
    { }

    public Accessor(string name) : this(typeof(T), name) { }
    public Accessor(T obj, string name) : this(obj.GetType(), name) { }

    public R Get(T obj) => Getter.Invoke(obj);
    public void Set(T obj, R val) => Setter.Invoke(obj, val);
    
    public static implicit operator Func<T, R>(Accessor<T, R> access) => access.Getter;
    public static implicit operator Action<T, R>(Accessor<T, R> access) => access.Setter;
}

public class StaticAccessor<R>
{
    private readonly Func<R> Getter;
    private readonly Action<R> Setter;
    public StaticAccessor(MemberInfo info)
    {
        (Getter, Setter) = info switch
        {
            PropertyInfo i => (Delegator.createStaticPropertyGetter<R>(i), _createStaticPropertySetter<R>(i)),
            FieldInfo i => (Delegator.createStaticFieldGetter<R>(i), Delegator.createStaticFieldSetter<R>(i)),
            _ => throw new ArgumentException($"AccessHelper.StaticAccessor can only handle infos that inherit {nameof(PropertyInfo)} or {nameof(FieldInfo)}, not {info?.GetType().Name ?? "null"}")
        };
    }
    public StaticAccessor(Type type, string name) : this((MemberInfo)
        type.GetField(name, Bindings.ppStatic) ??
        type.GetProperty(name, Bindings.ppStatic) ??
        throw new AccessHelperException($"Could not find static member '{name}' in {type.Name}")
    )
    { }
    public StaticAccessor(object obj, string name) : this(obj.GetType(), name) { }

    public R Get() => Getter.Invoke();
    public void Set(R val) => Setter.Invoke(val);

    public static implicit operator Func<R>(StaticAccessor<R> access) => access.Getter;
    public static implicit operator Action<R>(StaticAccessor<R> access) => access.Setter;

    // I think Ecconia just kinda forgot this one... hope this is correct!
    private static Action<VALUE> _createStaticPropertySetter<VALUE>(PropertyInfo property)
    {
        var setter = property.SetMethod;
        var valueExpression = Expression.Parameter(property.PropertyType, "value");
        var callExpression = Expression.Call(null, setter, valueExpression);
        return Expression.Lambda<Action<VALUE>>(callExpression, valueExpression).Compile();
    }
}
public class StaticAccessor<T, R>(string name) : StaticAccessor<R>(typeof(T), name) { }
