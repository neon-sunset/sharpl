namespace Sharpl;

public class Stack : SList<Value>
{
    public void Push<T>(Type<T> type, T data) where T: notnull
    {
        Push(Value.Make(type, data));
    }

    public void Reverse(int n)
    {
        Reverse(Count - n, n);
    }    
}