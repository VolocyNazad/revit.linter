using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using TUnit.Core.Executors;

namespace Revit.Linter.Languages.RevitTests;

public sealed class DocumentFilterFactoryTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Boxed_boolean_document_condition_compiles_and_evaluates()
    {
        CountingFormulaCompilationNotifier notifier = new();
        DocumentFilterFactory factory = new(NullLogger<DocumentFilterFactory>.Instance, notifier);
        const string formula = "if(property('IsFamilyDocument'), false, true)";

        Func<Document, bool> filter = factory.CreateBoolean(formula, fallback: false);

        await Assert.That(filter(_document!)).IsTrue();
        await Assert.That(factory.TryValidateBoolean(formula)).IsTrue();
        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Non_boolean_root_is_still_rejected()
    {
        CountingFormulaCompilationNotifier notifier = new();
        DocumentFilterFactory factory = new(NullLogger<DocumentFilterFactory>.Instance, notifier);

        Func<Document, bool> filter = factory.CreateBoolean("'text'", fallback: false);

        await Assert.That(filter(_document!)).IsFalse();
        await Assert.That(factory.TryValidateBoolean("'text'")).IsFalse();
        await Assert.That(notifier.NotificationCount).IsEqualTo(1);
    }

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }

        public void Notify() => NotificationCount++;
    }
}
