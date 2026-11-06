namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Normalizes diagnostic tags and matches them against search text.
/// </summary>
/// <remarks>
/// A tag is a plain string. Two kinds exist: system tags, fixed English literals the product sets
/// itself, and user tags, which keep the spelling from the configuration file. System tags look the
/// same in every interface language. A user tag may not reuse a reserved system tag name; <see cref="Merge"/>
/// drops such a name. The reserved system tags name the source of a diagnostic — the built-in checks
/// (<see cref="BuiltIn"/>), the module that loaded a configured rule (<see cref="Custom"/>,
/// <see cref="Collision"/>, <see cref="ParameterProject"/>) — and the configuration provenance
/// (<see cref="Example"/>, <see cref="Tour"/>, <see cref="Debug"/>).
/// <see cref="Project"/> and <see cref="Family"/> are the canonical spellings of the document-kind
/// tags the product sets on its built-in diagnostics. <see cref="Shared"/>, <see cref="Systems"/> and
/// <see cref="Architecture"/> are the canonical spellings of the discipline tags the product sets on
/// a built-in diagnostic from the prefix of its code. A configured rule declares both kinds itself in
/// its <c>tags</c> list, so they are ordinary tags a user may also write; a localized configuration
/// uses its own spelling of a discipline tag.
/// </remarks>
public static class DiagnosticTags
{
    /// <summary>Gets the canonical document-kind tag of diagnostics that run in project documents.</summary>
    public const string Project = "Project";
    /// <summary>Gets the canonical document-kind tag of diagnostics that run in family documents.</summary>
    public const string Family = "Family";
    /// <summary>Gets the canonical discipline tag of the shared element checks (<c>SHRD</c> codes).</summary>
    public const string Shared = "Shared";
    /// <summary>Gets the canonical discipline tag of the engineering-system checks (<c>SYST</c> codes).</summary>
    public const string Systems = "Systems";
    /// <summary>Gets the canonical discipline tag of the architectural checks (<c>ARCH</c> codes).</summary>
    public const string Architecture = "Architecture";
    /// <summary>Gets the system tag of the built-in checks that ship with the product.</summary>
    public const string BuiltIn = "BuiltIn";
    /// <summary>Gets the system tag of user-defined checks loaded from a configuration file.</summary>
    public const string Custom = "Custom";
    /// <summary>Gets the system tag of collision checks loaded from a configuration file.</summary>
    public const string Collision = "Collision";
    /// <summary>Gets the system tag of project-parameter checks loaded from a configuration file.</summary>
    public const string ParameterProject = "ParameterProject";
    /// <summary>Gets the system tag of diagnostics that came from an installed example configuration.</summary>
    public const string Example = "Example";
    /// <summary>Gets the system tag of diagnostics that belong to the managed practical-tour configuration.</summary>
    public const string Tour = "Tour";
    /// <summary>Gets the system tag of diagnostics that came from a debug configuration.</summary>
    public const string Debug = "Debug";

    private static readonly string[] _reserved = [BuiltIn, Custom, Collision, ParameterProject, Example, Tour, Debug];

    // The single place that decides the discipline tag of a built-in diagnostic code. A new built-in
    // prefix is added here together with its tag, or with null when the prefix deliberately has no
    // discipline; a guard test fails while a collector uses a prefix that has no entry.
    private static readonly IReadOnlyDictionary<string, string?> _disciplineTags =
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["SHRD"] = Shared,
            ["SYST"] = Systems,
            ["ARCH"] = Architecture,
            ["DOC"] = null,
            ["RVT"] = null,
        };

    /// <summary>Gets the empty tag list.</summary>
    public static IReadOnlyList<string> None { get; } = Array.Empty<string>();

    /// <summary>
    /// Tries to resolve the discipline tag of a built-in diagnostic code.
    /// </summary>
    /// <param name="code">The diagnostic code, for example <c>SHRD003</c>.</param>
    /// <param name="tag">The discipline tag, or <see langword="null"/> when the known prefix has none.</param>
    /// <returns>
    /// <see langword="true"/> when the prefix of the code has an entry in the discipline table;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The table decides the discipline of every built-in prefix, so a new prefix needs one explicit
    /// entry: its tag or a deliberate "no discipline". An unregistered prefix is reported by the guard
    /// test instead of silently losing its tag.
    /// </remarks>
    public static bool TryGetDisciplineTag(string? code, out string? tag)
    {
        tag = null;
        if (!TryGetCodePrefix(code, out string prefix)) return false;

        return _disciplineTags.TryGetValue(prefix, out tag);
    }

    /// <summary>
    /// Returns the canonical discipline tag that the prefix of a built-in diagnostic code implies.
    /// </summary>
    /// <param name="code">The diagnostic code, for example <c>SHRD003</c>.</param>
    /// <returns>
    /// <see cref="Shared"/> for an <c>SHRD</c> code, <see cref="Systems"/> for a <c>SYST</c> code,
    /// <see cref="Architecture"/> for an <c>ARCH</c> code; otherwise, <see langword="null"/>. A known
    /// prefix without a discipline also returns <see langword="null"/>; use
    /// <see cref="TryGetDisciplineTag"/> to tell that prefix from an unregistered one.
    /// </returns>
    public static string? DisciplineForCode(string? code) =>
        TryGetDisciplineTag(code, out string? tag) ? tag : null;

    private static bool TryGetCodePrefix(string? code, out string prefix)
    {
        prefix = string.Empty;
        if (code is null) return false;

        int length = 0;
        while (length < code.Length && char.IsLetter(code[length])) length++;
        if (length == 0) return false;

        prefix = code.Substring(0, length);
        return true;
    }

    /// <summary>
    /// Returns whether the tag is a system tag name that a user tag may not reuse.
    /// </summary>
    /// <param name="tag">The tag to test, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the tag is reserved; otherwise, <see langword="false"/>.</returns>
    public static bool IsReserved(string? tag)
    {
        if (tag is null || string.IsNullOrWhiteSpace(tag)) return false;

        string trimmed = tag.Trim();
        return _reserved.Any(reserved => string.Equals(reserved, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Trims the tags, drops the empty ones and removes duplicates, keeping the declaration order.
    /// </summary>
    /// <param name="tags">The tags to normalize, or <see langword="null"/>.</param>
    /// <returns>The normalized tags; the empty list when none remain.</returns>
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? tags)
    {
        if (tags is null) return None;

        List<string> normalized = [];
        foreach (string tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;

            string trimmed = tag.Trim();
            if (normalized.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
                continue;

            normalized.Add(trimmed);
        }

        return normalized.Count == 0 ? None : normalized.AsReadOnly();
    }

    /// <summary>
    /// Builds the system tags of a rule loaded from a configuration file.
    /// </summary>
    /// <param name="sourceTag">The tag of the module that loaded the rule: <see cref="Custom"/>, <see cref="Collision"/> or <see cref="ParameterProject"/>.</param>
    /// <param name="isExample">Whether the rule came from an installed example configuration.</param>
    /// <param name="isTour">Whether the rule belongs to the managed practical-tour configuration.</param>
    /// <param name="isDebug">Whether the rule came from a debug configuration.</param>
    /// <returns>
    /// The system tags in declaration order: the source tag, then the provenance tags
    /// (<see cref="Example"/>, <see cref="Tour"/>, <see cref="Debug"/>) that are set. The document-kind
    /// and discipline tags are not included, because a configured rule states them itself through its
    /// <c>tags</c> list.
    /// </returns>
    public static IReadOnlyList<string> ForConfiguredRule(string sourceTag, bool isExample, bool isTour, bool isDebug)
    {
        List<string> systemTags = [sourceTag];
        if (isExample) systemTags.Add(Example);
        if (isTour) systemTags.Add(Tour);
        if (isDebug) systemTags.Add(Debug);

        return systemTags.AsReadOnly();
    }

    /// <summary>
    /// Combines the system tags with the user tags into the list shown to the user.
    /// </summary>
    /// <param name="systemTags">The system tags the product sets, or <see langword="null"/>.</param>
    /// <param name="tags">The user tags from the configuration, or <see langword="null"/>.</param>
    /// <returns>The system tags first, then the user tags in declaration order.</returns>
    /// <remarks>
    /// A user tag whose name is a reserved system tag name (<see cref="IsReserved"/>) is dropped, so a
    /// configuration file cannot fake a system tag; the tags are also deduplicated ignoring case.
    /// </remarks>
    public static IReadOnlyList<string> Merge(IEnumerable<string>? systemTags, IEnumerable<string>? tags)
    {
        List<string> merged = [];
        foreach (string tag in Normalize(systemTags)) merged.Add(tag);

        foreach (string tag in Normalize(tags))
        {
            if (IsReserved(tag)) continue;
            if (merged.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase))) continue;

            merged.Add(tag);
        }

        return merged.Count == 0 ? None : merged.AsReadOnly();
    }

    /// <summary>
    /// Returns whether any tag contains the search text, ignoring case.
    /// </summary>
    /// <param name="tags">The tags of one diagnostic.</param>
    /// <param name="searchText">The text typed in the search box.</param>
    /// <returns><see langword="true"/> when a tag matches; otherwise, <see langword="false"/>.</returns>
    public static bool Matches(IReadOnlyList<string> tags, string? searchText)
    {
        if (tags.Count == 0 || string.IsNullOrEmpty(searchText)) return false;

        return tags.Any(tag => tag.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0);
    }
}
