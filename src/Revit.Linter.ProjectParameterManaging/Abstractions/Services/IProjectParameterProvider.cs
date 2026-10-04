using Autodesk.Revit.DB;
#if BEFORE2024
using ParameterGroupId = Autodesk.Revit.DB.BuiltInParameterGroup;
#else
using ParameterGroupId = Autodesk.Revit.DB.ForgeTypeId;
#endif

namespace Revit.Linter.ProjectParameterManaging.Abstractions.Services;

/// <summary>
/// Adds or updates shared project-parameter bindings in a Revit project document.
/// </summary>
public interface IProjectParameterProvider
{
    /// <summary>
    /// Adds the shared parameter identified by <paramref name="targetParameterId"/> or updates its existing binding.
    /// </summary>
    /// <param name="document">The project document whose parameter binding is modified.</param>
    /// <param name="targetParameterId">The shared parameter GUID defined in the module's shared-parameter file.</param>
    /// <param name="builtInCategories">The categories to which the parameter is bound.</param>
    /// <param name="parameterGroup">
    /// The parameter group in which the parameter is displayed: a <c>BuiltInParameterGroup</c> before Revit 2024
    /// and a group <c>ForgeTypeId</c> from Revit 2024 on.
    /// </param>
    /// <param name="isInstance"><see langword="true"/> to create an instance binding; <see langword="false"/> to create a type binding.</param>
    /// <param name="allowVaryBetweenGroups">Whether instance values may vary between elements in a group.</param>
    /// <returns><see langword="true"/> when the parameter is present with the requested binding; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The caller must invoke this operation in a modifiable Revit API context.</remarks>
    bool Add(
        Document document, Guid targetParameterId, IEnumerable<BuiltInCategory> builtInCategories,
        ParameterGroupId parameterGroup, bool isInstance = true, bool allowVaryBetweenGroups = false);
}
