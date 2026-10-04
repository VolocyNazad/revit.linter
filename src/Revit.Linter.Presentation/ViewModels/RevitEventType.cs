namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Identifies a Revit application event observed by an interaction view model.
/// </summary>
public enum RevitEventType
{
    /// <summary>A view was activated.</summary>
    ViewActivated,
    /// <summary>The active document changed.</summary>
    DocumentFocusChanged,
    /// <summary>A general application change occurred.</summary>
    Application,
    /// <summary>A document was closed.</summary>
    DocumentClosed,
    /// <summary>A document was opened.</summary>
    DocumentOpened,
    /// <summary>A document was created.</summary>
    DocumentCreated,
    /// <summary>A document's model data changed.</summary>
    DocumentChanged,
    /// <summary>A family was loaded into a document.</summary>
    FamilyLoadedIntoDocument,

}
