using Autodesk.Revit.UI;

namespace Revit.Linter.CollisionTestModels.Infrastructure.Ribbon;

/// <summary>
/// Describes the ribbon button that builds collision-testing documents with shared coordinates.
/// </summary>
/// <remarks>
/// Button texts are hardcoded because this is test tooling rather than product UI;
/// product buttons resolve their texts through localization.
/// The button data carries no icon images: the host applies the shared button icon when
/// adding the button, so this module stays free of WPF image resources.
/// </remarks>
public static class CollisionTestRibbon
{
    /// <summary>
    /// Name of the ribbon panel that hosts the collision-test button.
    /// </summary>
    /// <remarks>
    /// The panel is created on the diagnostics tab, separately from the product diagnostics panel,
    /// so test tooling never crowds product commands. The name is hardcoded like the button texts.
    /// </remarks>
    public const string PanelName = "Test models";

    /// <summary>Enables the collision-test button outside development builds.</summary>
    public const string VisibilityEnvironmentVariable = "REVIT_LINTER_TEST_TOOLS";

    /// <summary>
    /// Gets whether the collision-test button should be created.
    /// </summary>
    /// <remarks>
    /// Always visible in development builds; in other builds it appears only when
    /// the <c>REVIT_LINTER_TEST_TOOLS</c> environment variable is <c>1</c> or <c>true</c>.
    /// A dedicated variable is used instead of the hosting environment so that test tooling
    /// never changes logging or configuration selection for the product.
    /// </remarks>
    public static bool IsEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            string? value = Environment.GetEnvironmentVariable(VisibilityEnvironmentVariable);
            return string.Equals(value, "1", StringComparison.Ordinal)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
#endif
        }
    }
    /// <summary>
    /// Creates the button data for <c>CreateCollisionTestModelsCommand</c>.
    /// </summary>
    /// <param name="assemblyPath">Path of the assembly containing the command.</param>
    /// <returns>Button data without icon images; the host sets the shared icon.</returns>
    public static PushButtonData CreateButtonData(string assemblyPath) =>
        new(
            "CreateCollisionTestModelsButton",
            "Collision test models",
            assemblyPath,
            typeof(CreateCollisionTestModelsCommand).FullName!)
        {
            ToolTip = "Create a host with two shared-coordinates links whose walls overlap",
            LongDescription = "Creates two saved link models and a saved host with both links placed "
                + "by shared coordinates, so collision rules with link checks enabled report hits.",
        };
}
