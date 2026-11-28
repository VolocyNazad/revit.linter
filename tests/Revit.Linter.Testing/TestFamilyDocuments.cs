using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace Revit.Linter.Testing;

/// <summary>
/// Opens family documents built from the installed Revit family templates.
/// </summary>
public static class TestFamilyDocuments
{
    // Resolving a template means enumerating the whole family-template library and opening
    // candidate family documents until one fits. The result depends only on the installation, so
    // it is remembered for the lifetime of the test assembly.
    private static string[]? _templateFiles;
    private static string? _templateRoot;
    private static string? _firstTemplatePath;
    private static string? _modelFamilyTemplatePath;

    /// <summary>
    /// Opens the first family template the installation lists.
    /// </summary>
    /// <param name="application">The Revit application that opens the template.</param>
    /// <returns>The opened family document; the caller closes it.</returns>
    public static Document OpenTemplateFamily(Application application)
    {
        string? cachedPath = _firstTemplatePath;
        if (cachedPath is not null && File.Exists(cachedPath))
            return application.NewFamilyDocument(cachedPath);
        // Family templates live in locale subfolders below FamilyTemplatePath on some installations.
        string templatePath = GetTemplateFiles(application).FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No family template (*.rft) was found below '{application.FamilyTemplatePath}'.");
        _firstTemplatePath = templatePath;
        return application.NewFamilyDocument(templatePath);
    }

    /// <summary>
    /// Opens a model (non-annotation) family template, choosing one whose type table behaves like
    /// a model family.
    /// </summary>
    /// <param name="application">The Revit application that opens the template.</param>
    /// <returns>The opened family document; the caller closes it.</returns>
    public static Document OpenModelFamilyTemplate(Application application)
    {
        string? cachedPath = _modelFamilyTemplatePath;
        if (cachedPath is not null && File.Exists(cachedPath))
            return application.NewFamilyDocument(cachedPath);

        string[] candidates = GetTemplateFiles(application);
        if (candidates.Length == 0)
            throw new InvalidOperationException(
                $"No family template (*.rft) was found below '{application.FamilyTemplatePath}'.");
        string? preferredPath = candidates.FirstOrDefault(path =>
            IsLikelyGenericModel(Path.GetFileNameWithoutExtension(path)));
        if (preferredPath is not null)
        {
            _modelFamilyTemplatePath = preferredPath;
            return application.NewFamilyDocument(preferredPath);
        }

        List<string> tried = [];
#pragma warning disable S3267 // The loop opens and closes candidate documents: not a pure filter.
        foreach (string path in candidates.Take(40))
        {
            tried.Add(Path.GetFileName(path));
            Document candidate = application.NewFamilyDocument(path);
            try
            {
                if (candidate.OwnerFamily?.FamilyCategory?.CategoryType == CategoryType.Model)
                {
                    _modelFamilyTemplatePath = path;
                    return candidate;
                }
            }
            catch
            {
                // Not inspectable: close and try the next template.
            }
            candidate.Close(false);
        }
#pragma warning restore S3267
        throw new InvalidOperationException(
            $"No model family template was found below '{application.FamilyTemplatePath}'. " +
            $"Tried: {string.Join(", ", tried)}.");
    }

    // Plain generic models across locales; hosted ("based") variants are useless for type tests.
    private static bool IsLikelyGenericModel(string templateName) =>
        (templateName.Contains("generic model", StringComparison.OrdinalIgnoreCase)
            || templateName.Contains("常规模型", StringComparison.Ordinal)
            || templateName.Contains("обычная модель", StringComparison.OrdinalIgnoreCase))
        && !templateName.Contains("based", StringComparison.OrdinalIgnoreCase)
        && !templateName.Contains("основан", StringComparison.OrdinalIgnoreCase)
        && !templateName.Contains("基于", StringComparison.Ordinal);

    private static string[] GetTemplateFiles(Application application)
    {
        string templateRoot = application.FamilyTemplatePath;
        string[]? cachedFiles = _templateFiles;
        if (cachedFiles is not null
            && string.Equals(_templateRoot, templateRoot, StringComparison.OrdinalIgnoreCase))
            return cachedFiles;

        string[] files = Directory
            .EnumerateFiles(templateRoot, "*.rft", SearchOption.AllDirectories)
            .ToArray();
        _templateRoot = templateRoot;
        _templateFiles = files;
        return files;
    }
}
