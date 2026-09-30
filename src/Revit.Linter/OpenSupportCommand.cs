using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Opens the Revit Linter issue tracker in the default browser.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenSupportCommand : ExternalCommand
{
#pragma warning disable S1075 // This is the intentional, stable support destination.
    private const string SupportUrl = "https://github.com/VolocyNazad/revit.linter/issues";
#pragma warning restore S1075

    /// <inheritdoc />
    public override void Execute() => ExternalPageLauncher.Open<OpenSupportCommand>(SupportUrl);
}
