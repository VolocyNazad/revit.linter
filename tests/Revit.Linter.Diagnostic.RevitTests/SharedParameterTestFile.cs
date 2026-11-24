using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Creates a shared parameter through a temporary shared parameter file and binds it to a category.
/// </summary>
/// <remarks>
/// The Revit API creates a shared parameter only from a definition file, and the application keeps a
/// single current file, so the user file is replaced for the duration of the call and restored
/// afterwards. A test that needs two definitions with one name calls the helper twice: each call
/// writes its own file, which is how two parameter files produce a duplicate name.
/// </remarks>
internal static class SharedParameterTestFile
{
    private const string Header =
        "# This is a Revit shared parameter file.\n" +
        "*META\tVERSION\tMINVERSION\n" +
        "META\t2\t1\n" +
        "*GROUP\tID\tNAME\n";

    /// <summary>Creates a shared parameter and binds it to <paramref name="category"/>.</summary>
    /// <param name="document">The project document that receives the binding.</param>
    /// <param name="name">The definition name.</param>
    /// <param name="guid">The definition GUID; a new GUID makes a new definition.</param>
    /// <param name="category">The category to bind the parameter to.</param>
    /// <param name="isInstance">Whether to bind an instance parameter rather than a type parameter.</param>
    public static void Add(
        Document document, string name, Guid guid, Category category, bool isInstance = true)
    {
        Application application = document.Application;
        string path = Path.Combine(Path.GetTempPath(), $"revit-linter-shared-{guid:N}.txt");
        File.WriteAllText(path, Header);
        string originalFile = application.SharedParametersFilename;
        try
        {
            application.SharedParametersFilename = path;
            DefinitionFile file = application.OpenSharedParameterFile()
                ?? throw new InvalidOperationException(
                    $"Revit did not open the shared parameter file '{path}'.");
            DefinitionGroup group = file.Groups.Create("Linter");
            using ExternalDefinitionCreationOptions options = CreateOptions(name, guid);
            if (group.Definitions.Create(options) is not ExternalDefinition definition)
                throw new InvalidOperationException(
                    $"Revit did not create the shared parameter '{name}' in '{path}'.");

            CategorySet categorySet = application.Create.NewCategorySet();
            categorySet.Insert(category);
            Binding binding = isInstance
                ? application.Create.NewInstanceBinding(categorySet)
                : application.Create.NewTypeBinding(categorySet);
            if (!document.ParameterBindings.Insert(definition, binding))
                throw new InvalidOperationException(
                    $"Revit did not bind the shared parameter '{name}' to '{category.Name}'.");
        }
        finally
        {
            application.SharedParametersFilename = originalFile;
            TryDelete(path);
        }
    }

    /// <summary>Creates a shared parameter in a family document as a family parameter.</summary>
    /// <param name="familyDocument">The family document that receives the parameter.</param>
    /// <param name="name">The definition name.</param>
    /// <param name="guid">The definition GUID; a new GUID makes a new definition.</param>
    /// <param name="isInstance">Whether to add an instance parameter rather than a type parameter.</param>
    public static void AddToFamily(
        Document familyDocument, string name, Guid guid, bool isInstance = false)
    {
        Application application = familyDocument.Application;
        string path = Path.Combine(Path.GetTempPath(), $"revit-linter-shared-{guid:N}.txt");
        File.WriteAllText(path, Header);
        string originalFile = application.SharedParametersFilename;
        try
        {
            application.SharedParametersFilename = path;
            DefinitionFile file = application.OpenSharedParameterFile()
                ?? throw new InvalidOperationException(
                    $"Revit did not open the shared parameter file '{path}'.");
            DefinitionGroup group = file.Groups.Create("Linter");
            using ExternalDefinitionCreationOptions options = CreateOptions(name, guid);
            if (group.Definitions.Create(options) is not ExternalDefinition definition)
                throw new InvalidOperationException(
                    $"Revit did not create the shared parameter '{name}' in '{path}'.");

            FamilyManager manager = familyDocument.FamilyManager;
#if IS2021
            manager.AddParameter(definition, BuiltInParameterGroup.PG_DATA, isInstance);
#else
            manager.AddParameter(definition, GroupTypeId.Data, isInstance);
#endif
        }
        finally
        {
            application.SharedParametersFilename = originalFile;
            TryDelete(path);
        }
    }

    private static ExternalDefinitionCreationOptions CreateOptions(string name, Guid guid)
    {
#if IS2021
        ExternalDefinitionCreationOptions options = new(name, ParameterType.Text);
#else
        ExternalDefinitionCreationOptions options = new(name, SpecTypeId.String.Text);
#endif
        options.GUID = guid;
        return options;
    }

    // Revit may keep the definition file open after the current file is restored, so a file that
    // cannot be removed yet stays in the temporary folder instead of failing the test.
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The file is still held open: it waits in the temporary folder until the process ends.
        }
        catch (UnauthorizedAccessException)
        {
            // The file is still held open: it waits in the temporary folder until the process ends.
        }
    }
}
