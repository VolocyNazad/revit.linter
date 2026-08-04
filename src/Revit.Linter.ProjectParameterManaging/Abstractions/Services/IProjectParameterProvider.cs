using Autodesk.Revit.DB;

namespace Revit.Linter.ProjectParameterManaging.Abstractions.Services;

public interface IProjectParameterProvider
{
#if BEFORE2024
    bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        BuiltInParameterGroup builtInParameterGroup, bool isInstance = true, bool allowVaryBetweenGroups = false);
#else
    bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        ForgeTypeId groupTypeId, bool isInstance = true, bool allowVaryBetweenGroups = false);
#endif
}
