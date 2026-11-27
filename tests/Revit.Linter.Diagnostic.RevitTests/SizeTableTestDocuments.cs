using Autodesk.Revit.DB;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>Builds the family parameters and size lookup tables used by the size table tests.</summary>
internal static class SizeTableTestDocuments
{
    /// <summary>A table with one key column and two length columns.</summary>
    public const string ProbeTableCsv =
        ",Length##length##millimeters,Width##length##millimeters\nS,100,50\n";

    public static FamilySizeTableManager? GetManager(Document document) =>
        document.OwnerFamily is { } family
            ? FamilySizeTableManager.GetFamilySizeTableManager(document, family.Id)
            : null;

    public static void EnsureCurrentType(Document document)
    {
        FamilyManager manager = document.FamilyManager;
        if (manager.CurrentType is not null) return;
        AddType(document, "ProbeType");
    }

    public static FamilyType AddType(Document document, string name)
    {
        using Transaction transaction = new(document, "Create test type");
        transaction.Start();
        FamilyType type = document.FamilyManager.NewType(name);
        transaction.Commit();
        return type;
    }

    public static FamilyParameter AddLengthParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Add test parameter " + name);
        transaction.Start();
        FamilyParameter parameter = AddLengthParameter(document.FamilyManager, name);
        transaction.Commit();
        return parameter;
    }

    public static FamilyParameter AddTextParameter(Document document, string name)
    {
        using Transaction transaction = new(document, "Add test parameter " + name);
        transaction.Start();
        FamilyManager manager = document.FamilyManager;
#if IS2021
        FamilyParameter parameter = manager.AddParameter(
            name, BuiltInParameterGroup.PG_DATA, ParameterType.Text, false);
#else
        FamilyParameter parameter = manager.AddParameter(
            name, GroupTypeId.Data, SpecTypeId.String.Text, false);
#endif
        transaction.Commit();
        return parameter;
    }

    public static void SetText(Document document, FamilyParameter parameter, string value)
    {
        using Transaction transaction = new(document, "Set test parameter");
        transaction.Start();
        document.FamilyManager.Set(parameter, value);
        transaction.Commit();
    }

    public static void SetText(
        Document document, FamilyType type, FamilyParameter parameter, string value)
    {
        using Transaction transaction = new(document, "Set test type parameter");
        transaction.Start();
        document.FamilyManager.CurrentType = type;
        document.FamilyManager.Set(parameter, value);
        transaction.Commit();
    }

    public static void SetFormula(Document document, FamilyParameter parameter, string formula)
    {
        using Transaction transaction = new(document, "Set test formula");
        transaction.Start();
        document.FamilyManager.SetFormula(parameter, formula);
        transaction.Commit();
    }

    public static string ImportTable(Document document, string csv)
    {
        string tableName = $"Probe{Guid.NewGuid():N}";
        ImportTable(document, tableName, csv);
        return tableName;
    }

    public static void ImportTable(Document document, string tableName, string csv)
    {
        EnsureColumns(document);
        string tablePath = Path.Combine(Path.GetTempPath(), tableName + ".csv");
        File.WriteAllText(tablePath, csv);
        try
        {
            var errorInfo = new FamilySizeTableErrorInfo();
            using Transaction transaction = new(document, "Import test size table");
            transaction.Start();
            if (GetManager(document) is null)
                FamilySizeTableManager.CreateFamilySizeTableManager(document, document.OwnerFamily.Id);
            FamilySizeTableManager manager = GetManager(document)
                ?? throw new InvalidOperationException("The size table manager was not created.");
            bool imported = manager.ImportSizeTable(document, tablePath, errorInfo);
            if (!imported)
                throw new InvalidOperationException(
                    $"Size table import failed: {errorInfo.FamilySizeTableErrorType} " +
                    $"(file: '{errorInfo.FilePath}', column: {errorInfo.InvalidColumnIndex}, " +
                    $"header: '{errorInfo.InvalidHeaderText}', row: {errorInfo.InvalidRowIndex}).");
            transaction.Commit();
        }
        finally
        {
            if (File.Exists(tablePath)) File.Delete(tablePath);
        }
    }

    public static void RemoveTable(Document document, string tableName)
    {
        using Transaction transaction = new(document, "Remove test size table");
        transaction.Start();
        FamilySizeTableManager manager = GetManager(document)
            ?? throw new InvalidOperationException("The size table manager was not found.");
        if (!manager.RemoveSizeTable(tableName))
            throw new InvalidOperationException($"Size table '{tableName}' was not removed.");
        transaction.Commit();
    }

    private static FamilyParameter AddLengthParameter(FamilyManager manager, string name)
    {
#if IS2021
        return manager.AddParameter(name, BuiltInParameterGroup.PG_DATA, ParameterType.Length, false);
#else
        return manager.AddParameter(name, GroupTypeId.Data, SpecTypeId.Length, false);
#endif
    }

    private static void EnsureColumns(Document document)
    {
        FamilyManager manager = document.FamilyManager;
        if (manager.GetParameters().Any(parameter => parameter.Definition.Name == "Length")) return;

        using Transaction transaction = new(document, "Add test table columns");
        transaction.Start();
        AddLengthParameter(manager, "Length");
        AddLengthParameter(manager, "Width");
        transaction.Commit();
    }
}
