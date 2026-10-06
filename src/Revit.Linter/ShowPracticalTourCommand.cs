using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Infrastructure.ExternalCommands;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;

namespace Revit.Linter;

/// <summary>Resumes the optional practical tour from its saved step.</summary>
[Transaction(TransactionMode.Manual)]
public sealed class ShowPracticalTourCommand : ExternalCommand
{
    /// <inheritdoc />
    public override void Execute()
    {
        IWelcomeHost host = Program.Provider.GetRequiredService<IWelcomeHost>();
        Program.Provider.GetRequiredService<IPracticalTour>().Start(host.HasOpenDocument);
    }
}
