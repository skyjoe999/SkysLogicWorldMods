using System;
using Lua;

namespace SkysLuaLib.Shared;

public class Wrapped : IWrapped
{
    public virtual object Value { get; }
    public virtual Type ObjType { get; }

    protected Wrapped()
    {
        Metatable = new LuaTable();
    }

    public Wrapped(object value)
    {
        this.Value = value;
        ObjType = value?.GetType();
        Metatable = value is not null ? IWrapped.GenerateDefaultTable(value.GetType()) : new LuaTable();
    }

    public Wrapped(object value, Type type)
    {
        this.Value = value;
        ObjType = type;
        Metatable = value is not null ? IWrapped.GenerateDefaultTable(type) : new LuaTable();
    }

    public LuaTable Metatable { get; set; }
    public Span<LuaValue> UserValues => new([LuaValue.FromObject(Value)]);

    public override string ToString() => $"{GetType().Name}({Value})";

    public static implicit operator LuaValue(Wrapped wrapped) => new(wrapped);
}
