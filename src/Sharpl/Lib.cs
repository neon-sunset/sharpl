namespace Sharpl;

using Libs;

public class Lib
{
    private Dictionary<string, Value> bindings = new Dictionary<string, Value>();

    public Lib(string name, Lib? parentLib)
    {
        Name = name;
        ParentLib = parentLib;
    }

    public Value? this[string id]
    {
        get => Find(id);
        set
        {
            if (value == null)
            {
                Unbind(id);
            }
            else
            {
                Bind(id, (Value)value);
            }
        }
    }

    public void Bind(string id, Value value)
    {
        bindings[id] = value;
    }

    public void BindLib(Lib lib) {
            Bind(lib.Name, Value.Make(Core.Lib, lib));
    }
    
    public Macro BindMacro(string name, string[] args, Macro.BodyType body)
    {
        var m = new Macro(name, args, body);
        Bind(m.Name, Value.Make(Core.Macro, m));
        return m;
    }

    public Method BindMethod(string name, string[] args, Method.BodyType body)
    {
        var m = new Method(name, args, body);
        Bind(m.Name, Value.Make(Core.Method, m));
        return m;
    }

    public void BindType(AnyType t)
    {
        Bind(t.Name, Value.Make(Core.Meta, t));
    }

    public Value? Find(string id)
    {
        return bindings.ContainsKey(id) ? bindings[id] : ParentLib?.Find(id);
    }

    public void Import(Lib source) {
        foreach (var (id, v) in source.bindings) {
            Bind(id, v);
        }
    }

    public string Name { get; }
    public Lib? ParentLib { get; }

    public override string ToString() {
        return $"(Lib {Name})";
    }

    public bool Unbind(string id)
    {
        return bindings.Remove(id);
    }
}