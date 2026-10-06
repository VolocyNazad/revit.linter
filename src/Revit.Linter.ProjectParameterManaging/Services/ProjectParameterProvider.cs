using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;
using System.Reflection;
#if BEFORE2024
using Revit.Sugar;
#endif
#if BEFORE2024
using ParameterGroupId = Autodesk.Revit.DB.BuiltInParameterGroup;
#else
using ParameterGroupId = Autodesk.Revit.DB.ForgeTypeId;
#endif

namespace Revit.Linter.ProjectParameterManaging.Services;

internal sealed class ProjectParameterProvider : IProjectParameterProvider
{
    private const string SharedParameterFileName = "required-revit-project-parameters.txt";
    private static readonly string DirectoryPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!;

    public bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        ParameterGroupId parameterGroup, bool isInstance = true, bool allowVaryBetweenGroups = false)
    {
        if (document is not { IsValidObject: true } || document.IsFamilyDocument) return false;

        BindingMap bindingMap = document.ParameterBindings;
        if (FindBoundDefinition(document, bindingMap, targetParameterId) is { } boundDefinition)
        {
            bool reInserted =
                !IsBoundDifferently(bindingMap, boundDefinition, builtInCategories, parameterGroup, isInstance)
                || bindingMap.ReInsert(
                    boundDefinition, CreateParameterBinding(document, builtInCategories, isInstance), parameterGroup);
            boundDefinition.SetAllowVaryBetweenGroups(document, allowVaryBetweenGroups);
            return reInserted;
        }

        string sharedParameterFile = Path.Combine(DirectoryPath, SharedParameterFileName);
        if (!File.Exists(sharedParameterFile)) return false;

        // Revit reads shared parameters only from the file set on the application, so the user's file is
        // replaced for the duration of the lookup and restored afterwards.
        Application application = document.Application;
        string userSharedParameterFile = application.SharedParametersFilename;
        try
        {
            application.SharedParametersFilename = sharedParameterFile;
            foreach (DefinitionGroup group in application.OpenSharedParameterFile().Groups)
            {
                foreach (Definition definition in group.Definitions)
                {
                    if (definition is not ExternalDefinition externalDefinition ||
                        externalDefinition.GUID != targetParameterId) continue;

                    bool inserted = bindingMap.Insert(
                        externalDefinition, CreateParameterBinding(document, builtInCategories, isInstance),
                        parameterGroup);
                    SharedParameterElement.Lookup(document, targetParameterId)?.GetDefinition()
                        ?.SetAllowVaryBetweenGroups(document, allowVaryBetweenGroups);
                    return inserted;
                }
            }
        }
        finally
        {
            application.SharedParametersFilename = userSharedParameterFile;
        }

        return false;
    }

    public bool IsConfigured(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        ParameterGroupId parameterGroup, bool isInstance = true, bool allowVaryBetweenGroups = false)
    {
        if (document is not { IsValidObject: true } || document.IsFamilyDocument) return false;

        BindingMap bindingMap = document.ParameterBindings;
        if (FindBoundDefinition(document, bindingMap, targetParameterId) is not InternalDefinition boundDefinition)
            return false;
        if (bindingMap.get_Item(boundDefinition) is not ElementBinding) return false;
        if (boundDefinition.VariesAcrossGroups != allowVaryBetweenGroups) return false;

        return !IsBoundDifferently(bindingMap, boundDefinition, builtInCategories, parameterGroup, isInstance);
    }

    private static InternalDefinition? FindBoundDefinition(Document document, BindingMap bindingMap, Guid parameterId)
    {
        if (SharedParameterElement.Lookup(document, parameterId) is not { } parameter) return null;

        var iterator = bindingMap.ForwardIterator();
        iterator.Reset();
        while (iterator.MoveNext())
            if (iterator.Key is InternalDefinition definition && definition.Id == parameter.Id)
                return definition;

        return null;
    }

    private static bool IsBoundDifferently(
        BindingMap bindingMap, InternalDefinition definition, IEnumerable<BuiltInCategory> builtInCategories,
        ParameterGroupId parameterGroup, bool isInstance)
    {
        ElementBinding binding = (ElementBinding)bindingMap.get_Item(definition);
        return (binding is InstanceBinding && !isInstance)
            || (binding is TypeBinding && isInstance)
            || GetGroup(definition) != parameterGroup
            || !binding.Categories.Cast<Category>().Select(i => i.BuiltInCategory).SetEquals(builtInCategories);
    }

#if BEFORE2024
    private static ParameterGroupId GetGroup(InternalDefinition definition) => definition.ParameterGroup;
#else
    private static ParameterGroupId GetGroup(InternalDefinition definition) => definition.GetGroupTypeId();
#endif

    private static Binding CreateParameterBinding(
        Document document, IEnumerable<BuiltInCategory> builtInCategories, bool isInstance)
    {
        if (!builtInCategories.Any())
            throw new InvalidOperationException(
                "Unable to create parameter binding to categories because category list is empty");

        Application application = document.Application;
        CategorySet categorySet = application.Create.NewCategorySet();
        foreach (BuiltInCategory builtInCategory in builtInCategories)
            if (Category.GetCategory(document, builtInCategory) is { } category)
                categorySet.Insert(category);

        return isInstance
            ? application.Create.NewInstanceBinding(categorySet)
            : application.Create.NewTypeBinding(categorySet);
    }
}
