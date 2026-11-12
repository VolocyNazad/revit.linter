using System.Text;

namespace Revit.Linter.Sandbox.TemplateScan;

/// <summary>Minimal indented JSON writer used only by the one-off template scan.</summary>
internal sealed class JsonBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly Stack<bool> _first = new();
    private int _depth;
    private bool _afterName;

    public override string ToString() => _builder.ToString();

    public JsonBuilder BeginObject()
    {
        Separate();
        _builder.Append('{');
        _depth++;
        _first.Push(true);
        return this;
    }

    public JsonBuilder EndObject()
    {
        _depth--;
        _first.Pop();
        NewLine();
        _builder.Append('}');
        return this;
    }

    public JsonBuilder BeginArray()
    {
        Separate();
        _builder.Append('[');
        _depth++;
        _first.Push(true);
        return this;
    }

    public JsonBuilder EndArray()
    {
        _depth--;
        _first.Pop();
        NewLine();
        _builder.Append(']');
        return this;
    }

    public JsonBuilder Name(string name)
    {
        Separate();
        AppendQuoted(name);
        _builder.Append(": ");
        _afterName = true;
        return this;
    }

    public JsonBuilder String(string? value)
    {
        Separate();
        if (value is null)
            _builder.Append("null");
        else
            AppendQuoted(value);
        return this;
    }

    public JsonBuilder Number(long value)
    {
        Separate();
        _builder.Append(value);
        return this;
    }

    public JsonBuilder Bool(bool value)
    {
        Separate();
        _builder.Append(value ? "true" : "false");
        return this;
    }

    private void Separate()
    {
        if (_afterName)
        {
            _afterName = false;
            return;
        }

        if (_first.Count == 0)
            return;

        if (_first.Peek())
        {
            _first.Pop();
            _first.Push(false);
        }
        else
        {
            _builder.Append(", ");
        }

        NewLine();
    }

    private void NewLine()
    {
        _builder.AppendLine();
        _builder.Append(' ', _depth * 2);
    }

    private void AppendQuoted(string value)
    {
        _builder.Append('"');
        foreach (char symbol in value)
        {
            switch (symbol)
            {
                case '"':
                    _builder.Append("\\\"");
                    break;
                case '\\':
                    _builder.Append("\\\\");
                    break;
                case '\n':
                    _builder.Append("\\n");
                    break;
                case '\r':
                    _builder.Append("\\r");
                    break;
                case '\t':
                    _builder.Append("\\t");
                    break;
                default:
                    if (symbol < ' ')
                        _builder.Append("\\u").Append(((int)symbol).ToString("x4"));
                    else
                        _builder.Append(symbol);
                    break;
            }
        }

        _builder.Append('"');
    }
}
