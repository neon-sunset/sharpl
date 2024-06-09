namespace Sharpl;

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
    private String loadPath = "";
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
                case Op.T.Check:
                    {
                        var checkOp = (Ops.Check)op.Data;

                        if (stack.Pop() is Value ev)
                        {
                            if (stack.Pop() is Value av)
                            {
                                if (!av.Equals(ev))
                                {
                                    throw new EvalError(checkOp.Loc, $"Check failed: expected {checkOp.Expected}, actual {av}!");
                                }
                            }
                            else
                            {
                                throw new EvalError(checkOp.Loc, "Missing actual value");
                            }
                        }
                        else
                        {
                            throw new EvalError(checkOp.Loc, "Missing expected value");
                        }

                        PC++;
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
                        #pragma warning disable CS8629               
                        PC = (PC)gotoOp.Target.PC;
                        #pragma warning restore CS8629               
                        break;
                    }
                case Op.T.Push:
                    {
                        var pushOp = (Ops.Push)op.Data;
                        stack.Push(pushOp.Value.Copy());
                        PC++;
                        break;
                    }
                case Op.T.SetLoadPath:
                    {
                        var setOp = (Ops.SetLoadPath)op.Data;
                        loadPath = setOp.Path;
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

    public void Eval(Form form, Env env, Form.Queue args, Stack stack)
    {
        var skipLabel = new Label();
        Emit(Ops.Goto.Make(skipLabel));
        var startPC = EmitPC;
        form.Emit(this, env, args);
        Emit(Ops.Stop.Make());
        skipLabel.PC = EmitPC;
        Eval(startPC, stack);
    }

    public Value? Eval(Form form, Env env, Form.Queue args)
    {
        var stack = new Stack(STACK_SIZE);
        Eval(form, env, args, stack);
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


    public void Load(String path, Env env)
    {
        var prevLoadPath = loadPath;
        var p = Path.Combine(loadPath, path);

        try
        {
            if (Path.GetDirectoryName(p) is String d)
            {
                loadPath = d;
            }

            var loc = new Loc(path);

            using (StreamReader source = new StreamReader(path, Encoding.UTF8))
            {
                var c = source.Peek();

                if (c == '#')
                {
                    source.ReadLine();
                    loc.NewLine();
                }

                var forms = ReadForms(source, ref loc);
                Emit(Ops.SetLoadPath.Make(loadPath));
                forms.Emit(this, env);
                Emit(Ops.SetLoadPath.Make(prevLoadPath));
            }
        }
        finally
        {
            loadPath = prevLoadPath;
        }

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
        var loc = new Loc("repl");
        var bufferLines = 0;

        while (true)
        {
            Console.Write($"{(loc.Line + bufferLines),4} ");
            var line = Console.In.ReadLine();

            if (line is null)
            {
                break;
            }

            if (line == "")
            {
                var startPC = EmitPC;

                try
                {
                    ReadForms(new StringReader(buffer.ToString()), ref loc).Emit(this, UserLib);
                    Emit(Ops.Stop.Make());
                    Eval(startPC, stack);
                    Console.WriteLine(stack.Empty ? Value.Nil : stack.Pop());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    buffer.Clear();
                    bufferLines = 0;
                }

                Console.WriteLine("");
            }
            else
            {
                buffer.Append(line);
                buffer.AppendLine();
                bufferLines++;
            }
        }
    }

    public void SetRegister(int frameOffset, int index, Value value)
    {
        registers[frames.Peek(frameOffset) + index] = value;
    }
}