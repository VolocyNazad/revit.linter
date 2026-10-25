using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.ExternalCommands;

namespace Revit.Linter;

/// <summary>Opens the Revit Linter issue tracker in the default browser.</summary>
/// <remarks>The command does not require an active document or a Revit transaction.</remarks>
[Transaction(TransactionMode.Manual)]
public sealed class OpenSupportCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute() =>
        Program.Provider.GetRequiredService<IShellLauncher>().OpenUrl(ProductIdentity.SupportUrl);
}
