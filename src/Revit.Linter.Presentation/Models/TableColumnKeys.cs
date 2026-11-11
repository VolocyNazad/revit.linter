namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Names the data columns that a table can hide.
/// </summary>
/// <remarks>
/// Only data columns carry a key: the action icon columns of a table have none and therefore always stay
/// visible. A key is written to <c>columns.yml</c>, so renaming one shows the column again.
/// </remarks>
public static class TableColumnKeys
{
    /// <summary>The checkbox that turns a diagnostic on or off.</summary>
    public const string IsActive = "is-active";

    /// <summary>The diagnostic or fix code.</summary>
    public const string Code = "code";

    /// <summary>The severity of a diagnostic.</summary>
    public const string Severity = "severity";

    /// <summary>The description of a diagnostic.</summary>
    public const string Description = "description";

    /// <summary>The message of a finding or fix.</summary>
    public const string Message = "message";

    /// <summary>The document a finding or fix belongs to.</summary>
    public const string Document = "document";

    /// <summary>The moment a finding or fix was created.</summary>
    public const string Created = "created";

    /// <summary>The tags of a diagnostic or finding.</summary>
    public const string Tags = "tags";

    /// <summary>The scope of an ignored target.</summary>
    public const string Scope = "scope";

    /// <summary>The Revit element identifier of an ignored target.</summary>
    public const string ElementId = "id";

    /// <summary>The ignored target itself.</summary>
    public const string Target = "target";
}
