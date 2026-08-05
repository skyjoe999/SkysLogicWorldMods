using System;
using System.Collections;
using System.Collections.Generic;
using Lua;

namespace SkysLuaLib.Shared;

public class Wrapper
{
    public Type WrappedType;
    public readonly Wrapper ParentWrapper;
    protected readonly Dictionary<string, ICachedLookup> Cache = [];
    public readonly LuaFunction IndexFunc;
    public readonly LuaFunction NewindexFunc;

    protected internal Wrapper(Type type, Wrapper parent = null)
    {
        WrappedType = type;
        WrapperManager.RegisterWrapper(type, this);
        ParentWrapper = parent ?? WrapperManager.GetWrapper(type.BaseType);

        IndexFunc = new LuaFunction(type.Name + ":__index", (context, _)
            => context.ReturnTask(
                Index(
                    context.GetArgument<IWrapped>(0),
                    Callable.UnpackArgument(context.Arguments[1])
                )
            ));
        NewindexFunc = new LuaFunction(type.Name + ":__newindex", (context, _)
            =>
        {
            NewIndex(
                context.Arguments[0].Read<IWrapped>(),
                Callable.UnpackArgument(context.Arguments[1]),
                Callable.UnpackArgument(context.Arguments[2])
            );
            return context.ReturnTask();
        });
    }

    protected virtual LuaValue Index(IWrapped obj, object key)
    {
        // Look for a Get() function
        if (TryInstanceGetter(obj, key, out var ret)) return ret;

        if (key is not string _key)
            throw IndexError(obj, key);

        // TODO: Add hierarchy search
        if (!Cache.TryGetValue(_key, out var lookup))
        {
            // key has not been cached (or doesn't exist)
            if (!TryCacheNewLookup(_key, out lookup)) throw IndexError(obj, key);
            Cache[_key] = lookup;
        }

        return lookup.Get(obj.Value);
    }


    protected virtual void NewIndex(IWrapped obj, object key, object value)
    {
        // Look for a Set() function
        if (TryInstanceSetter(obj, key, value, out var exception)) return;
        if (exception is not null) throw exception;

        if (key is not string _key)
            throw NewIndexError(obj, key, value);

        // TODO: Add hierarchy search
        if (!Cache.TryGetValue(_key, out var lookup))
        {
            // key has not been cached (or doesn't exist)
            if (!TryCacheNewLookup(_key, out lookup)) throw NewIndexError(obj, key, value);
            Cache[_key] = lookup;
        }

        lookup.Set(obj.Value, value);
    }

    protected virtual bool TryCacheNewLookup(string key, out ICachedLookup newLookup)
    {
        if ((newLookup = MethodLookup.Cache(key, WrappedType)) is not null) return true;
        if ((newLookup = PropertyLookup.Cache(key, WrappedType)) is not null) return true;
        if ((newLookup = FieldLookup.Cache(key, WrappedType)) is not null) return true;
        return false;
    }

    protected bool? HasGetter;
    protected bool? HasSetter;

    protected virtual bool TryInstanceGetter(IWrapped obj, object key, out LuaValue ret)
    {
        if (!HasGetter.HasValue)
        {
            HasGetter = TryCacheNewLookup("Get", out var lookup);
            if (HasGetter.Value)
                Cache["Get"] = lookup;
        }

        if (HasGetter.Value && ((MethodLookup)Cache["Get"]).Method.TryCall(obj.Value, [key], out ret, out _))
            return true;

        ret = LuaValue.Nil;
        return false;
    }

    protected virtual bool TryInstanceSetter(IWrapped obj, object key, object value, out Exception exception)
    {
        if (!HasSetter.HasValue)
        {
            HasSetter = TryCacheNewLookup("Set", out var lookup);
            if (HasSetter.Value)
                Cache["Set"] = lookup;
        }

        exception = null;
        if (HasSetter.Value &&
            ((MethodLookup)Cache["Set"]).Method.TryCall(obj.Value, [key, value], out _, out exception))
            return true;
        if (exception is KeyNotFoundException) exception = null;
        return false;
    }

    public virtual LuaValue Wrap(object obj) => new Wrapped(obj, WrappedType);

    public virtual Wrapper CreateSubWrapper(Type new_type)
    {
        // TODO: Add enumerable support
        if (typeof(IList).IsAssignableFrom(new_type))
            return new ListWrapper(new_type, this);
        if (typeof(IReadOnlyList<object>).IsAssignableFrom(new_type))
            return new ReadableListWrapper(new_type, this);

        return new Wrapper(new_type, this);
    }

    protected virtual Exception IndexError(IWrapped obj, object key)
        => new KeyNotFoundException($"Key '{key}' not found on type '{WrappedType}'");

    protected virtual Exception NewIndexError(IWrapped obj, object key, object value) => IndexError(obj, key);
}
