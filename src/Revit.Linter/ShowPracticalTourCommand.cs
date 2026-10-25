using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.WelcomePresenter.Abstractions;

namespace Revit.Linter;

/// <summary>Resumes the optional practical tour from its saved step.</summary>
[Transaction(TransactionMode.Manual)]
public sealed class ShowPracticalTourCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        IPracticalTourHost host = Program.Provider.GetRequiredService<IPracticalTourHost>();
        Program.Provider.GetRequiredService<IPracticalTour>().Start(host.HasOpenDocument);
    }
}
