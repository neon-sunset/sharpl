namespace Sharpl.Types.Core;

public interface IterTrait
{
    Iter CreateIter(Value target, VM vm, Loc loc);
};