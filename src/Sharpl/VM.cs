namespace Sharpl;

using System.Reflection.Metadata;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text;

using PC = int;

public class VM
{
    public static readonly int STACK_SIZE = 1024;
    public static readonly int VERSION = 1;

    public readonly Libs.Core CoreLib = new Libs.Core();
    public readonly Libs.String StringLib = new Libs.String();
    public readonly Libs.Term TermLib;
    public readonly Lib UserLib = new Lib("user", null);

    public PC PC = 0;

    private ArrayStack<Call> calls = new ArrayStack<Call>(1024);
    private ArrayStack<Op> code = new ArrayStack<Op>(1024);
    private ArrayStack<int> frames = new ArrayStack<int>(1024);
    private List<Label> labels = new List<Label>();
    private ArrayStack<Value> registers = new ArrayStack<Value>(1024);

    private Reader[] readers = [
        Readers.WhiteSpace.Instance,

        Readers.Call.Instance,
        Readers.Int.Instance,
        Readers.String.Instance,

        Readers.Id.Instance
    ];

    public VM()
    {
        TermLib = new Libs.Term(this);
        UserLib.BindLib(CoreLib);
        UserLib.BindLib(StringLib);
        UserLib.BindLib(TermLib);
        UserLib.BindLib(UserLib);
    }

    public int AllocRegister()
    {
        var result = registers.Count;
        registers.Push(Value.Nil);
        return result;
    }


    public void BeginFrame()
    {
        frames.Push(registers.Count);
    }

    public PC Emit(Op op)
    {
        var result = code.Count;
        code.Push(op);
        return result;
    }

    public PC EmitPC
    {
        get { return code.Count; }
    }

    public void EndFrame()
    {
        frames.Pop();
    }

    public void Eval(PC startPC, Stack stack)
    {
        PC = startPC;

        while (true)
        {
            var op = code[PC];

            Console.WriteLine(op);

            switch (op.Type)
            {
                case Op.T.CallDirect:
                    {
                        var callOp = (Ops.CallDirect)op.Data;
                        var recursive = !calls.Empty && calls.Peek().Target.Equals(callOp.Target);
                        PC++;
                        callOp.Target.Call(callOp.Loc, this, stack, callOp.Arity, recursive);
                        break;
                    }
                case Op.T.CallIndirect:
                    {
                        var target = stack.Pop();
                        var callOp = (Ops.CallIndirect)op.Data;
                        var recursive = !calls.Empty && calls.Peek().Target.Equals(target);
                        PC++;
                        target.Call(callOp.Loc, this, stack, callOp.Arity, recursive);
                        break;
                    }
                case Op.T.CallMethod:
                    {
                        var callOp = (Ops.CallMethod)op.Data;
                        PC++;
                        var recursive = !calls.Empty && calls.Peek().Target.Equals(callOp.Target);
                        callOp.Target.Call(callOp.Loc, this, stack, callOp.Arity, recursive);
                        break;
                    }
                case Op.T.CallPrim:
                    {
                        var callOp = (Ops.CallPrim)op.Data;
                        PC++;
                        callOp.Target.Call(callOp.Loc, this, stack, callOp.Arity, false);
                        break;
                    }
                case Op.T.GetRegister:
                    {
                        var getOp = (Ops.GetRegister)op.Data;
                        stack.Push(GetRegister(getOp.FrameOffset, getOp.Index));
                        break;
                    }
                case Op.T.Goto:
                    {
                        var gotoOp = (Ops.Goto)op.Data;
                        PC = gotoOp.Target.PC;
                        break;
                    }
                case Op.T.Push:
                    {
                        var pushOp = (Ops.Push)op.Data;
                        stack.Push(pushOp.Value.Copy());
                        PC++;
                        break;
                    }
                case Op.T.Stop:
                    {
                        PC++;
                        return;
                    }
            }
        }
    }

    public void Eval(Form form, Lib lib, Form.Queue args, Stack stack)
    {
        var skipLabel = new Label();
        Emit(Ops.Goto.Make(skipLabel));
        var startPC = EmitPC;
        form.Emit(this, lib, args);
        skipLabel.PC = EmitPC;
        Eval(startPC, stack);
    }

    public Value? Eval(Form form, Lib lib, Form.Queue args) {
        var stack = new Stack(STACK_SIZE);
        Eval(form, lib, args, stack);
        return stack.Pop();
    }

    public Value GetRegister(int frameOffset, int index)
    {
        return registers[frames.Peek(frameOffset) + index];
    }

    public Label Label(PC pc = -1)
    {
        var l = new Label(pc);
        labels.Append(l);
        return l;
    }


    public bool ReadForm(TextReader source, ref Loc loc, Form.Queue forms)
    {
        foreach (var r in readers)
        {
            if (r.Read(source, this, ref loc, forms))
            {
                return true;
            }
        }

        return false;
    }

    public Form? ReadForm(TextReader source, ref Loc loc)
    {
        var forms = new Form.Queue();
        ReadForm(source, ref loc, forms);
        return forms.Pop();
    }

    public void ReadForms(TextReader source, ref Loc loc, Form.Queue forms)
    {
        while (ReadForm(source, ref loc, forms)) { }
    }

    public Form.Queue ReadForms(TextReader source, ref Loc loc)
    {
        var forms = new Form.Queue();
        ReadForms(source, ref loc, forms);
        return forms;
    }

    public void REPL()
    {
        Console.Write($"Sharpl v{VERSION} — may the src be with you\n\n");
        var buffer = new StringBuilder();
        var stack = new Stack(32);

        while (true)
        {
            Console.Write("  ");
            var line = Console.In.ReadLine();

            if (line is null)
            {
                break;
            }

            if (line == "")
            {
                var startPC = EmitPC;
                var loc = new Loc("repl");

                try
                {
                    ReadForms(new StringReader(buffer.ToString()), ref loc).Emit(this, UserLib);
                    buffer.Clear();
                    Emit(Ops.Stop.Make());
                    Eval(startPC, stack);
                    Console.WriteLine(stack.Empty ? Value.Nil : stack.Pop());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine("");
            }

            buffer.Append(line);
            buffer.AppendLine();
        }
    }

    public void SetRegister(int frameOffset, int index, Value value)
    {
        registers[frames.Peek(frameOffset) + index] = value;
    }
}