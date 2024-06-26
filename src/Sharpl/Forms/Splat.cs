namespace Sharpl.Forms;

public class Splat : Form
{
    public readonly Form Target;

    public Splat(Loc loc, Form target) : base(loc)
    {
        Target = target;
    }

    public override void Emit(VM vm, Form.Queue args)
    {
        Target.Emit(vm, args);
        vm.Emit(Ops.Splat.Make(Loc));
    }

    public override bool IsSplat => true;

    public override string ToString() {
        return $"{Target}*";
    }
}