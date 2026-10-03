using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.Utils;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class DocumentationLauncher : IDocumentationLauncher
{
    public void Open(DocumentationPage page) => ExternalPageLauncher.Open<DocumentationLauncher>(page.GetUrl());
}
