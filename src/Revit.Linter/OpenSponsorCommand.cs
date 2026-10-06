using Autodesk.Revit.Attributes;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Opens the author's GitHub Sponsors page in the default browser.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenSponsorCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute() => ExternalPageLauncher.Open<OpenSponsorCommand>(ProductIdentity.SponsorUrl);
}
