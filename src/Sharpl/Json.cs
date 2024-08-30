using System.Globalization;
using System.Text;
using Sharpl.Libs;

namespace Sharpl;

public static class Json
{
    public static Value? ReadArray(VM vm, TextReader source, ref Loc loc)
    {
        var c = source.Peek();
        if (c == -1 || c != '[') { return null; }
        loc.Column++;
        source.Read();
        var items = new List<Value>();

        while (true)
        {
            ReadWhitespace(source, ref loc);
            switch (source.Peek())
            {
                case -1: throw new ReadError(loc, "Unexpected end of array");

                case ']':
                    {
                        loc.Column++;
                        source.Read();
                        goto EXIT;
                    }

                case ',':
                    {
                        loc.Column++;
                        source.Read();
                        break;
                    }
            }

            if (ReadValue(vm, source, ref loc) is Value it) { items.Add(it); }
            else { throw new ReadError(loc, "Unexpected end of array"); }
        }

    EXIT:
        return Value.Make(Core.Array, items.ToArray());
    }

    public static Value? ReadDecimal(TextReader source, ref Loc loc, int value)
    {
        var startLoc = loc;
        var c = source.Peek();
        if (c == -1 || c != '.') { return null; }
        loc.Column++;
        source.Read();
        byte e = 0;

        while (true)
        {
            c = source.Peek();
            if (c == -1) { break; }
            var cc = Convert.ToChar(c);
            if (!char.IsAsciiDigit(cc)) { break; }
            source.Read();
            value = value * 10 + (int)CharUnicodeInfo.GetDecimalDigitValue(cc);
            e++;
            loc.Column++;
        }

        return (startLoc.Column == loc.Column) ? null : Value.Make(Core.Fix, Fix.Make(e, value));
    }

    public static Value? ReadId(TextReader source, ref Loc loc)
    {
        var c = source.Peek();
        if (c == -1) { return null; }
        var cc = Convert.ToChar(c);
        if (!char.IsAscii(cc)) { return null; }
        var buffer = new StringBuilder();

        while (true)
        {
            c = source.Peek();
            if (c == -1 || c == ',' || c == '[' || c == ']' || c == '{' || c == '}' || c == '"') { break; }
            cc = Convert.ToChar(c);
            if (!char.IsAscii(cc)) { break; }
            source.Read();
            buffer.Append(cc);
            loc.Column++;
        }

        return buffer.ToString() switch
        {
            "" => null,
            "null" => Value.Nil,
            "true" => Value.T,
            "false" => Value.F,
            var id => throw new ReadError(loc, $"Unknown id: {id}")
        };
    }

    public static Value? ReadMap(VM vm, TextReader source, ref Loc loc)
    {
        var c = source.Peek();
        if (c == -1 || c != '{') { return null; }
        loc.Column++;
        source.Read();
        var m = new OrderedMap<Value, Value>();

        while (true)
        {
            ReadWhitespace(source, ref loc);
            switch (source.Peek())
            {
                case -1: throw new ReadError(loc, "Unexpected end of map");

                case '}':
                    {
                        loc.Column++;
                        source.Read();
                        goto EXIT;
                    }

                case ',':
                    {
                        loc.Column++;
                        source.Read();
                        break;
                    }
            }

            if (ReadString(source, ref loc) is Value k)
            {
                ReadWhitespace(source, ref loc);
                c = source.Peek();
                if (c != ':') { throw new ReadError(loc, $"Invalid map: {c}"); }
                loc.Column++;
                source.Read();
                if (ReadValue(vm, source, ref loc) is Value v) { m[Value.Make(Core.Sym, vm.Intern(k.Cast(Core.String)))] = v; }
                else { throw new ReadError(loc, "Unexpected end of map"); }
            }
            else { throw new ReadError(loc, "Unexpected end of map"); }
        }

    EXIT:
        return Value.Make(Core.Map, m);
    }
    public static Value? ReadNumber(TextReader source, ref Loc loc)
    {
        var v = 0;
        var startLoc = loc;

        while (true)
        {
            var c = source.Peek();
            if (c == -1) { break; }
            if (c == '.') { return ReadDecimal(source, ref loc, v); }
            var cc = Convert.ToChar(c);
            if (!char.IsAsciiDigit(cc)) { break; }
            source.Read();
            v = v * 10 + CharUnicodeInfo.GetDecimalDigitValue(cc);
            loc.Column++;
        }

        return (startLoc.Column == loc.Column) ? null : Value.Make(Core.Int, v);
    }

    public static Value? ReadString(TextReader source, ref Loc loc)
    {
        var c = source.Peek();
        if (c == -1 || c != '"') { return null; }
        source.Read();
        var sb = new StringBuilder();

        while (true)
        {
            c = source.Peek();
            if (c == -1) { throw new ReadError(loc, "Invalid string"); }
            source.Read();
            if (c == '"') { break; }

            if (c == '\\')
            {
                loc.Column++;

                c = source.Read() switch
                {
                    'r' => '\r',
                    'n' => '\n',
                    '\\' => '\\',
                    '"' => '"',
                    var v => throw new ReadError(loc, $"Invalid escape: {Convert.ToChar(v)}")
                };
            }

            sb.Append(Convert.ToChar(c));
            loc.Column++;
        }

        var s = sb.ToString();
        return (s == "") ? null : Value.Make(Core.String, s);
    }

    public static Value? ReadValue(VM vm, TextReader source, ref Loc loc)
    {
    START:
        switch (source.Peek())
        {
            case -1: return null;
            case '[': return ReadArray(vm, source, ref loc);
            case '{': return ReadMap(vm, source, ref loc);
            case '"': return ReadString(source, ref loc);

            case var c:
                {
                    var cc = Convert.ToChar(c);

                    if (char.IsWhiteSpace(cc))
                    {
                        ReadWhitespace(source, ref loc);
                        goto START;
                    }

                    if (char.IsDigit(cc)) { return ReadNumber(source, ref loc); }
                    if (char.IsAscii(cc)) { return ReadId(source, ref loc); }
                    return null;
                }
        }
    }

    public static void ReadWhitespace(TextReader source, ref Loc loc)
    {
        while (true)
        {
            switch (source.Peek())
            {
                case ' ':
                case '\t':
                    loc.Column++;
                    source.Read();
                    break;
                case '\r':
                case '\n':
                    loc.NewLine();
                    source.Read();
                    break;
                default:
                    return;
            }
        }
    }
}