using System.Text;

namespace Sharpl.Types.Term;

public class KeyType : Type<ConsoleKeyInfo>
{
    public KeyType(string name) : base(name) { }

    public override void Dump(Value value, VM vm, StringBuilder result)
    {
        result.Append("(term/Key ");
        Say(value, vm, result);
        result.Append(')');
    }

    public override void Say(Value value, VM vm, StringBuilder result)
    {
        var ki = value.CastUnbox(this);
        if ((ki.Modifiers & ConsoleModifiers.Alt) != 0) { result.Append("Alt+"); }
        if ((ki.Modifiers & ConsoleModifiers.Control) != 0) { result.Append("Ctrl+"); }
        if ((ki.Modifiers & ConsoleModifiers.Shift) != 0) { result.Append("Shift+"); }
        result.Append(ki.Key.ToString());
    }
}
