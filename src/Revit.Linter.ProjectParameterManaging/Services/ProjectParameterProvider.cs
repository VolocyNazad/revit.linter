#if BEFORE2024

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;
using System.Reflection;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ProjectParameterManaging.Services;

internal sealed class ProjectParameterProvider : IProjectParameterProvider
{
    private const string SharedParameterFileExtension = ".txt";
    private const string SharedParameterFileMask = $"*{SharedParameterFileExtension}";
    private static readonly string DirectoryPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

    public bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        BuiltInParameterGroup builtInParameterGroup, bool isInstance = true)
    {
        if (document is not { IsValidObject: true }) return false;

        Application application = document.Application;

        BindingMap bindingMap = document.ParameterBindings;
        var iterator = bindingMap.ForwardIterator();

        iterator.Reset();
        while (iterator.MoveNext())
        {
            if (iterator.Key is not InternalDefinition definition) continue;
            var parameterId = new FilteredElementCollector(document).OfClass(typeof(SharedParameterElement))
                .ToElementIds().FirstOrDefault(i => i.Equals(definition.Id));
            if (parameterId is null) continue;
            var sharedParameterElement = (SharedParameterElement)document.GetElement(parameterId);
            if (sharedParameterElement.GuidValue != targetParameterId) continue;
            ElementBinding binging = (ElementBinding)bindingMap.get_Item(definition);
            bool hasDifference = false;
            if (binging is InstanceBinding && !isInstance) hasDifference = true;
            if (binging is TypeBinding && isInstance) hasDifference = true;
            if (definition.ParameterGroup != builtInParameterGroup) hasDifference = true;
            if (!binging.Categories
                .Cast<Category>()
                .Select(i => i.BuiltInCategory)
                .SetEquals(builtInCategories)) hasDifference = true;
            if (hasDifference)
                return bindingMap.ReInsert(definition, CreateParameterBinding(document, builtInCategories, isInstance));
        }

        string sharedParametersFilenameCache = application.SharedParametersFilename;
        foreach (string sharedParameterFile in GetSharedParameterFiles())
        {
            //if (!File.Exists("sharedParameterFile")) continue;
            application.SharedParametersFilename = sharedParameterFile;
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            foreach (DefinitionGroup group in definitionFile.Groups)
            {
                foreach (ExternalDefinition definition in group.Definitions)
                {
                    if (definition.GUID != targetParameterId) continue;
                    return AddSharedParameterToDocument(
                        document, definition, builtInCategories, builtInParameterGroup, isInstance);
                }
            }
        }

        application.SharedParametersFilename = sharedParametersFilenameCache;

        return false;
    }

    private static bool AddSharedParameterToDocument(
        Document document, ExternalDefinition parameterDefinition, IEnumerable<BuiltInCategory> builtInCategories,
        BuiltInParameterGroup builtInParameterGroup, bool isInstance)
    {
        Binding binding = CreateParameterBinding(document, builtInCategories, isInstance);
        bool result = document.ParameterBindings.Insert(parameterDefinition, binding, builtInParameterGroup);
        return result;
    }

    private static Binding CreateParameterBinding(
        Document document, IEnumerable<BuiltInCategory> builtInCategories, bool isInstance)
    {
        Application application = document.Application;
        CategorySet categorySet = CreateCategorySet(document, builtInCategories);
        return isInstance
            ? application.Create.NewInstanceBinding(categorySet)
            : application.Create.NewTypeBinding(categorySet);
    }

    private static CategorySet CreateCategorySet(Document document, IEnumerable<BuiltInCategory> builtInCategories)
    {
        Application application = document.Application;
        CategorySet categorySet = application.Create.NewCategorySet();

        if (builtInCategories.Any())
        {
            foreach (BuiltInCategory builtInCategory in builtInCategories)
            {
                Category category = Category.GetCategory(document, builtInCategory);
                if (category != null)
                {
                    categorySet.Insert(category);
                }
            }
        }
        else throw new InvalidOperationException("Unable to create parameter binding to categories because category list is empty");
        return categorySet;
    }
    private static IEnumerable<string> GetSharedParameterFiles()
    {
        IEnumerable<string> sharedParameterFiles = Directory
            .GetFiles(DirectoryPath, SharedParameterFileMask, SearchOption.AllDirectories);
        foreach (string sharedParameterFile in sharedParameterFiles)
            yield return sharedParameterFile;
    }
}

#else

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;
using System.Reflection;

namespace Revit.Linter.ProjectParameterManaging.Services;

internal sealed class ProjectParameterProvider : IProjectParameterProvider
{
    private const string SharedParameterFileExtension = ".txt";
    private const string SharedParameterFileMask = $"*{SharedParameterFileExtension}";
    private static readonly string DirectoryPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

    public bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        ForgeTypeId groupTypeId, bool isInstance = true)
    {
        if (document is not { IsValidObject: true }) return false;
        Application application = document.Application;

        BindingMap bindingMap = document.ParameterBindings;
        var iterator = bindingMap.ForwardIterator();
        iterator.Reset();
        while (iterator.MoveNext()) {
            if (iterator.Key is not InternalDefinition definition) continue;
            var parameterId = new FilteredElementCollector(document).OfClass(typeof(SharedParameterElement))
                .ToElementIds().FirstOrDefault(i => i.Equals(definition.Id));
            if (parameterId is null) continue;
            var sharedParameterElement = (SharedParameterElement)document.GetElement(parameterId);
            if (sharedParameterElement.GuidValue != targetParameterId) continue;
            ElementBinding binging = (ElementBinding)bindingMap.get_Item(definition);
            bool hasDifference = false;
            if (binging is InstanceBinding && !isInstance) hasDifference = true;
            if (binging is TypeBinding && isInstance) hasDifference = true;
            if (definition.GetGroupTypeId() != groupTypeId) hasDifference = true;
            if (!binging.Categories
                .Cast<Category>()
                .Select(i => i.BuiltInCategory)
                .SetEquals(builtInCategories)) hasDifference = true;
            if (hasDifference)
                return bindingMap.ReInsert(definition, CreateParameterBinding(document, builtInCategories, isInstance));
        }

        string sharedParametersFilenameCache = application.SharedParametersFilename;
        foreach (string sharedParameterFile in GetSharedParameterFiles()) {
            //if (!File.Exists("sharedParameterFile")) continue;
            application.SharedParametersFilename = sharedParameterFile;
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            foreach (DefinitionGroup group in definitionFile.Groups) {
                foreach (ExternalDefinition definition in group.Definitions) {
                    if (definition.GUID != targetParameterId) continue;
                    return AddSharedParameterToDocument(
                        document, definition, builtInCategories, groupTypeId, isInstance);
                }
            }
        }
        application.SharedParametersFilename = sharedParametersFilenameCache;
        return false;
    }

    private static bool AddSharedParameterToDocument(
        Document document, ExternalDefinition parameterDefinition, IEnumerable<BuiltInCategory> builtInCategories,
        ForgeTypeId groupTypeId, bool isInstance)
    {
        Binding binding = CreateParameterBinding(document, builtInCategories, isInstance);
        bool result = document.ParameterBindings.Insert(parameterDefinition, binding, groupTypeId);
        return result;
    }
    private static Binding CreateParameterBinding(
        Document document, IEnumerable<BuiltInCategory> builtInCategories, bool isInstance)
    {
        Application application = document.Application;
        CategorySet categorySet = CreateCategorySet(document, builtInCategories);
        return isInstance 
            ? application.Create.NewInstanceBinding(categorySet) 
            : application.Create.NewTypeBinding(categorySet);
    }
    private static CategorySet CreateCategorySet(Document document, IEnumerable<BuiltInCategory> builtInCategories)
    {
        Application application = document.Application;
        CategorySet categorySet = application.Create.NewCategorySet();

        if (builtInCategories.Any()) {
            foreach (BuiltInCategory builtInCategory in builtInCategories) {
                Category category = Category.GetCategory(document, builtInCategory);
                if (category != null) {
                    categorySet.Insert(category);
                }
            }
        }
        else throw new InvalidOperationException("Unable to create parameter binding to categories because category list is empty");
        return categorySet;
    }
    private static IEnumerable<string> GetSharedParameterFiles()
    {
        IEnumerable<string> sharedParameterFiles = Directory
            .GetFiles(DirectoryPath, SharedParameterFileMask, SearchOption.AllDirectories);
        foreach (string sharedParameterFile in sharedParameterFiles)
            yield return sharedParameterFile;
    }
}
#endif