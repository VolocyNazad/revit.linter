using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class DocumentationLauncher(
    IShellLauncher shellLauncher,
    IUserInterfaceActivityStream activityStream) : IDocumentationLauncher
{
    public void Open(DocumentationPage page)
    {
        shellLauncher.OpenUrl(page.GetUrl());
        activityStream.Publish(new DocumentationOpenedActivity());
    }
}
