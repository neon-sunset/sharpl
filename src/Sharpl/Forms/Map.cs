namespace Sharpl.Forms;

using System.Text;

public class Map : Form
{
    public readonly Form[] Items;

    public Map(Loc loc, Form[] items) : base(loc)
    {
        Items = items;
    }

    public override void CollectIds(HashSet<string> result)
    {
        foreach (var f in Items) { f.CollectIds(result); }
    }

    public override void Emit(VM vm, Queue args)
    {
        var callConstructor = false;

        foreach (var f in Items)
        {
            if (!(f is Pair))
            {
                callConstructor = true;
                break;
            }
        }

        if (callConstructor)
        {
            args.PushFirst(new Call(Loc, new Id(Loc, "Map"), Items));
        }
        else
        {
            vm.Emit(Ops.CreateMap.Make(Items.Length));
            var i = 0;

            foreach (var f in Items)
            {
                if (f is Pair pf)
                {
                    vm.Emit(pf.Left);
                    vm.Emit(pf.Right);
                }
                else
                {
                    vm.Emit(f);
                }

                vm.Emit(Ops.SetMapItem.Make());
                i++;
            }
        }
    }

    public override bool Equals(Form other)
    {
        if (other is Map f)
        {
            if (Items.Length != f.Items.Length) { return false; }

            for (var i = 0; i < Math.Min(Items.Length, f.Items.Length); i++)
            {
                if (!Items[i].Equals(f.Items[i])) { return false; }
            }

            return true;
        }

        return false;
    }

    public override Form Quote(Loc loc, VM vm) =>
        new Map(loc, Items.Select(it => it.Quote(loc, vm)).ToArray());

    public override string ToString()
    {
        var b = new StringBuilder();
        b.Append('{');
        var i = 0;

        foreach (var f in Items)
        {
            if (i > 0)
            {
                b.Append(' ');
            }

            b.Append(f);
            i++;
        }

        b.Append('}');
        return b.ToString();
    }

    public override Form Unquote(Loc loc, VM vm) =>
        new Map(loc, Items.Select(it => it.Unquote(loc, vm)).ToArray());
}