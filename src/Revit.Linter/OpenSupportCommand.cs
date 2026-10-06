using Autodesk.Revit.Attributes;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Opens the Revit Linter issue tracker in the default browser.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenSupportCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute() => ExternalPageLauncher.Open<OpenSupportCommand>(ProductIdentity.SupportUrl);
}
