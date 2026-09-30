using Autodesk.Revit.Attributes;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter;

/// <summary>Opens the author's GitHub Sponsors page in the default browser.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenSponsorCommand : ExternalCommand
{
#pragma warning disable S1075 // This is the intentional, stable sponsorship destination.
    private const string SponsorUrl = "https://github.com/sponsors/VolocyNazad";
#pragma warning restore S1075

    /// <inheritdoc />
    public override void Execute() => ExternalPageLauncher.Open<OpenSponsorCommand>(SponsorUrl);
}
