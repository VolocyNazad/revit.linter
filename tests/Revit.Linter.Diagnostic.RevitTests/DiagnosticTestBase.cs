using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

/// <summary>
/// Shared fixture for the diagnostic module tests that read or change document state: the helpers of
/// <see cref="DiagnosticTestFixture"/> plus a fresh project document per test.
/// </summary>
public abstract class DiagnosticTestBase : DiagnosticTestFixture
{
    private protected Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    private protected Element CreateLevel(double elevation = 0)
    {
        using Transaction transaction = new(_document!, "Create level");
        transaction.Start();
        Level level = Level.Create(_document!, elevation);
        transaction.Commit();
        return level;
    }
}
