namespace Sharpl.Types.Core;

public class BindingType : Type<Register>
{
    public BindingType(string name) : base(name) { }

    public override void EmitCall(Loc loc, VM vm, Value target, Form.Queue args)
    {
        var arity = args.Count;
        var splat = args.IsSplat;
        args.Emit(vm, new Form.Queue());
        var v = target.CastUnbox(this);
        vm.Emit(Ops.CallRegister.Make(loc, v, arity, splat, vm.NextRegisterIndex));
    }

    public override void Emit(Loc loc, VM vm, Value target, Form.Queue args)
    {
        var v = target.CastUnbox(this);
        vm.Emit(Ops.GetRegister.Make(v.FrameOffset, v.Index));
    }

    public override Form Unquote(Loc loc, VM vm, Value value) => 
        new Forms.Binding(loc, value.CastUnbox(this));
}
