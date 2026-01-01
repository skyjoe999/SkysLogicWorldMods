using System;
using Lua;

namespace SkysLuaLib.Server.LuaWrapper.WrappedObjects;

public class Wrapped : IWrapped
{
    public virtual object value { get; }
    public virtual Type ObjType { get; }

    protected Wrapped()
    {
        Metatable = new LuaTable();
    }

    public Wrapped(object value)
    {
        this.value = value;
        ObjType = value?.GetType();
        Metatable = value is not null ? IWrapped.GenerateDefaultTable(value.GetType()) : new LuaTable();
    }

    public Wrapped(object value, Type type)
    {
        this.value = value;
        ObjType = type;
        Metatable = value is not null ? IWrapped.GenerateDefaultTable(type) : new LuaTable();
    }

    public LuaTable Metatable { get; set; }
    public Span<LuaValue> UserValues => new([LuaValue.FromObject(value)]);

    public override string ToString() => $"{GetType().Name}({value})";

    public static implicit operator LuaValue(Wrapped wrapped)
        => new LuaValue(wrapped);
}
