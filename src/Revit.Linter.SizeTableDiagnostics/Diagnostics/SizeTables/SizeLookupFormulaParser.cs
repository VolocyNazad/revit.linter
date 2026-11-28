using System.Text;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

/// <summary>Finds the size lookup calls in a family formula.</summary>
internal static class SizeLookupFormulaParser
{
    private const string FunctionName = "size_lookup";

    /// <summary>Parses every <c>size_lookup</c> call in a formula, including nested calls.</summary>
    /// <param name="formula">The formula text, or <see langword="null"/> when the parameter has none.</param>
    /// <param name="formulaParameterName">The parameter that owns the formula.</param>
    /// <returns>The calls in the order they appear.</returns>
    public static IReadOnlyList<SizeLookupCall> Parse(string? formula, string formulaParameterName)
    {
        // Explicit null check: string.IsNullOrWhiteSpace carries no NotNullWhen contract on net48.
        if (formula is null || string.IsNullOrWhiteSpace(formula)) return [];

        List<SizeLookupCall> calls = [];
        int searchFrom = 0;
        while (TryFindFunction(formula, searchFrom, out int openParenthesis))
        {
            if (TryFindClosingParenthesis(formula, openParenthesis, out int closeParenthesis))
            {
                string arguments = formula.Substring(
                    openParenthesis + 1, closeParenthesis - openParenthesis - 1);
                AddCall(calls, arguments, formulaParameterName);
            }
            searchFrom = openParenthesis + 1;
        }
        return calls;
    }

    private static void AddCall(List<SizeLookupCall> calls, string arguments, string formulaParameterName)
    {
        IReadOnlyList<string> parts = SplitTopLevel(arguments);
        if (parts.Count < 2) return;

        SizeLookupArgument table = ParseArgument(parts[0]);
        SizeLookupArgument column = ParseArgument(parts[1]);
        // Both arguments can be expressions; the call is kept so DOC007 can report that
        // neither the table nor the column name can be determined.
        calls.Add(new SizeLookupCall(formulaParameterName, table, column));
    }

    private static bool TryFindFunction(string text, int searchFrom, out int openParenthesis)
    {
        openParenthesis = -1;
        bool inString = false;
        bool inBracket = false;
        int index = searchFrom;
        while (index < text.Length)
        {
            char current = text[index];
            if (inString)
            {
                if (current != '"')
                {
                    index++;
                    continue;
                }
                if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }
                inString = false;
                index++;
                continue;
            }
            if (inBracket)
            {
                if (current == ']') inBracket = false;
                index++;
                continue;
            }
            if (current == '"')
            {
                inString = true;
                index++;
                continue;
            }
            if (current == '[')
            {
                inBracket = true;
                index++;
                continue;
            }
            if (!MatchesFunctionName(text, index))
            {
                index++;
                continue;
            }

            int next = index + FunctionName.Length;
            while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
            if (next >= text.Length || text[next] != '(')
            {
                index++;
                continue;
            }

            openParenthesis = next;
            return true;
        }
        return false;
    }

    private static bool MatchesFunctionName(string text, int index)
    {
        if (index + FunctionName.Length > text.Length) return false;
        if (index > 0 && IsIdentifierPart(text[index - 1])) return false;
        return string.Compare(
            text, index, FunctionName, 0, FunctionName.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static bool TryFindClosingParenthesis(string text, int openParenthesis, out int closeParenthesis)
    {
        closeParenthesis = -1;
        int depth = 1;
        bool inString = false;
        bool inBracket = false;
        int index = openParenthesis + 1;
        while (index < text.Length)
        {
            char current = text[index];
            if (inString)
            {
                if (current != '"')
                {
                    index++;
                    continue;
                }
                if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }
                inString = false;
                index++;
                continue;
            }
            if (inBracket)
            {
                if (current == ']') inBracket = false;
                index++;
                continue;
            }
            if (current == '"')
            {
                inString = true;
                index++;
                continue;
            }
            if (current == '[')
            {
                inBracket = true;
                index++;
                continue;
            }
            if (current == '(')
            {
                depth++;
                index++;
                continue;
            }
            if (current == ')')
            {
                depth--;
                if (depth == 0)
                {
                    closeParenthesis = index;
                    return true;
                }
            }
            index++;
        }
        return false;
    }

    private static IReadOnlyList<string> SplitTopLevel(string text)
    {
        List<string> parts = [];
        int depth = 0;
        int start = 0;
        bool inString = false;
        bool inBracket = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (inString)
            {
                if (current != '"')
                {
                    index++;
                    continue;
                }
                if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }
                inString = false;
                index++;
                continue;
            }
            if (inBracket)
            {
                if (current == ']') inBracket = false;
                index++;
                continue;
            }
            if (current == '"')
            {
                inString = true;
                index++;
                continue;
            }
            if (current == '[')
            {
                inBracket = true;
                index++;
                continue;
            }
            if (current == '(')
            {
                depth++;
                index++;
                continue;
            }
            if (current == ')')
            {
                if (depth > 0) depth--;
                index++;
                continue;
            }
            if (current == ',' && depth == 0)
            {
                parts.Add(text.Substring(start, index - start));
                start = index + 1;
            }
            index++;
        }
        parts.Add(text.Substring(start));
        return parts;
    }

    private static SizeLookupArgument ParseArgument(string text)
    {
        // Only a quoted value or a single parameter is resolved statically. An expression such
        // as IF(...) or a text concatenation stays unresolved; DOC007 reports it, and the table
        // checks leave it to manual review.
        string value = text.Trim();
        if (value.Length == 0) return SizeLookupArgument.Unresolved;

        if (value[0] == '"')
        {
            return TryParseStringLiteral(value, out string literal)
                ? new SizeLookupArgument(literal, null)
                : SizeLookupArgument.Unresolved;
        }

        if (value[0] == '[' && value[value.Length - 1] == ']' && value.Length > 2)
        {
            string parameterName = value.Substring(1, value.Length - 2);
            return parameterName.IndexOf(']') >= 0
                ? SizeLookupArgument.Unresolved
                : new SizeLookupArgument(null, parameterName);
        }

        return IsIdentifier(value)
            ? new SizeLookupArgument(null, value)
            : SizeLookupArgument.Unresolved;
    }

    private static bool IsIdentifier(string value)
    {
        if (value.Length == 0) return false;
        if (!char.IsLetter(value[0]) && value[0] != '_') return false;
        for (int index = 1; index < value.Length; index++)
        {
            if (!IsIdentifierPart(value[index])) return false;
        }
        return true;
    }

    private static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_';

    private static bool TryParseStringLiteral(string value, out string literal)
    {
        literal = string.Empty;
        if (value.Length < 2 || value[0] != '"') return false;

        StringBuilder builder = new(value.Length);
        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != '"')
            {
                builder.Append(current);
                index++;
                continue;
            }
            if (index + 1 < value.Length && value[index + 1] == '"')
            {
                builder.Append('"');
                index += 2;
                continue;
            }
            if (index != value.Length - 1) return false;

            literal = builder.ToString();
            return true;
        }
        return false;
    }
}
