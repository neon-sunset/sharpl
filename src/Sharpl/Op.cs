namespace Sharpl;

public readonly struct Op
{
    public enum T
    {
        And,
        BeginFrame, Benchmark, Branch,
        CallDirect, CallStack, CallMethod, CallTail, CallUserMethod, CallRegister, 
        Check, CopyRegister, 
        CreateArray, CreateIter, CreateList, CreateMap, CreatePair,
        Decrement, Drop,
        EndFrame, ExitMethod,
        GetRegister, Goto,
        Increment, IterNext,
        OpenInputStream, Or, 
        PopItem, PrepareClosure, Push, PushItem, PushSplat,
        Repush,
        SetArrayItem, SetLoadPath, SetMapItem, SetRegister, 
        Splat, Stop, Swap,
        UnquoteRegister,
        Unzip
    };

    public readonly object Data;
    public T Type { get; }

    public Op(T type, object data)
    {
        Type = type;
        Data = data;
    }

    public override string ToString() {
        if (Data is null) { return $"{Type}";  }
#pragma warning disable CS8603 // Possible null reference return.
        return Data.ToString();
#pragma warning restore CS8603 // Possible null reference return.
    }
}