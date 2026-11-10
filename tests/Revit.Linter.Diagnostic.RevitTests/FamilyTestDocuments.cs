using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace Revit.Linter.Diagnostic.RevitTests;

internal static class FamilyTestDocuments
{
    // Resolving a template means enumerating the whole family-template library and opening
    // candidate family documents until one fits. The result depends only on the installation, so
    // it is remembered for the lifetime of the test assembly; every caller runs its Revit API calls
    // on the single Revit thread of that assembly, one test at a time.
    private static string[]? _templateFiles;
    private static string? _templateRoot;
    private static string? _firstTemplatePath;
    private static string? _modelFamilyTemplatePath;
    private static string? _titleBlockTemplatePath;

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

    // Type-table tests need a model (non-annotation) family: the first .rft on disk is often
    // an annotation template, where type creation does not behave like in model families.
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

    public static void AddTextTypeParameter(Document familyDocument, string name)
    {
        FamilyManager manager = familyDocument.FamilyManager;
#if IS2021
        manager.AddParameter(name, BuiltInParameterGroup.PG_DATA, ParameterType.Text, false);
#else
        manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.String.Text, false);
#endif
    }

    public static Family LoadFamilyWithType(
        Autodesk.Revit.ApplicationServices.Application application,
        Document projectDocument,
        string typeName)
    {
        Document familyDocument = OpenModelFamilyTemplate(application);
        try
        {
            using (Transaction transaction = new(familyDocument, "Create test family type"))
            {
                transaction.Start();
                familyDocument.FamilyManager.NewType(typeName);
                transaction.Commit();
            }
            return familyDocument.LoadFamily(projectDocument);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    public static FamilyInstance PlaceInstance(
        Document projectDocument, FamilySymbol symbol, Level level, XYZ point)
    {
        if (!symbol.IsActive) symbol.Activate();
        return projectDocument.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural);
    }

    public static FamilySymbol FindSymbolByTypeName(Document document, string typeName)
    {
        IReadOnlyList<FamilySymbol> symbols = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .ToArray();
        return symbols.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, typeName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No family type '{typeName}' was found. " +
                $"Available types: {string.Join(", ", symbols.Select(candidate => $"'{candidate.Family.Name}:{candidate.Name}'"))}.");
    }

    public static void PlaceTitleBlocks(Document document, ViewSheet sheet, int count)
    {
        FamilySymbol symbol = LoadTitleblockSymbol(document);
        using Transaction transaction = new(document, "Place test title blocks");
        transaction.Start();
        if (!symbol.IsActive) symbol.Activate();
        for (int index = 0; index < count; index++)
            document.Create.NewFamilyInstance(new XYZ(index * 2, 0, 0), symbol, sheet);
        transaction.Commit();
    }

    // Title block family templates do not ship with Revit: an annotation template is
    // reassigned to the title block category, which makes its instances view-based.
    // Templates that reject the category are skipped like OpenModelFamilyTemplate does.
    public static FamilySymbol LoadTitleblockSymbol(Document document)
    {
        Autodesk.Revit.ApplicationServices.Application application = document.Application
            ?? throw new InvalidOperationException("The test document has no application.");

        string? cachedPath = _titleBlockTemplatePath;
        if (cachedPath is not null && File.Exists(cachedPath))
        {
            FamilySymbol? cachedSymbol = TryCreateTitleblockSymbol(application, document, cachedPath);
            if (cachedSymbol is not null)
                return cachedSymbol;
        }

        string[] candidates = GetTemplateFiles(application)
            .OrderByDescending(path => IsLikelyAnnotation(Path.GetFileNameWithoutExtension(path)))
            .Take(40)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException(
                $"No family template (*.rft) was found below '{application.FamilyTemplatePath}'.");
        List<string> tried = [];
#pragma warning disable S3267 // The loop opens and closes candidate documents: not a pure filter.
        foreach (string path in candidates)
        {
            tried.Add(Path.GetFileName(path));
            FamilySymbol? symbol = TryCreateTitleblockSymbol(application, document, path);
            if (symbol is null)
                continue;
            _titleBlockTemplatePath = path;
            return symbol;
        }
#pragma warning restore S3267
        throw new InvalidOperationException(
            $"No title block family template was found below '{application.FamilyTemplatePath}'. " +
            $"Tried: {string.Join(", ", tried)}.");
    }

    /// <summary>
    /// Opens one family template, reassigns its category to title blocks and loads it into
    /// <paramref name="projectDocument"/>.
    /// </summary>
    /// <returns>The loaded symbol, or <see langword="null"/> when the template rejects the category or cannot be loaded.</returns>
    private static FamilySymbol? TryCreateTitleblockSymbol(
        Autodesk.Revit.ApplicationServices.Application application,
        Document projectDocument,
        string templatePath)
    {
        Document familyDocument = application.NewFamilyDocument(templatePath);
        try
        {
            bool committed = false;
            using (Transaction transaction = new(familyDocument, "Create test title block type"))
            {
                transaction.Start();
                try
                {
                    familyDocument.OwnerFamily.FamilyCategory = familyDocument.Settings.Categories
                        .get_Item(BuiltInCategory.OST_TitleBlocks);
                    familyDocument.FamilyManager.NewType("LinterTitleblock");
                    transaction.Commit();
                    committed = true;
                }
                catch (ArgumentException)
                {
                    transaction.RollBack();
                }
            }
            if (!committed)
                return null;
            try
            {
                Family family = familyDocument.LoadFamily(projectDocument);
                return new FilteredElementCollector(projectDocument)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .First(candidate => candidate.Family.Id == family.Id);
            }
            catch
            {
                // Not loadable: the caller tries the next template.
                return null;
            }
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

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

    private static bool IsLikelyAnnotation(string templateName) =>
        templateName.Contains("annot", StringComparison.OrdinalIgnoreCase)
        || templateName.Contains("аннот", StringComparison.OrdinalIgnoreCase);
}
