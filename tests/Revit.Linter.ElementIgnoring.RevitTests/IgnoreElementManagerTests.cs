using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Microsoft.Extensions.DependencyInjection;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ElementIgnoring.DI;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementIgnoring.RevitTests;

public sealed class IgnoreElementManagerTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void Cleanup() => _document?.Close(false);

    [Test]
    public async Task Dependency_injection_resolves_detector_provider_and_list_manager_as_same_singleton()
    {
        using ServiceProvider services = CreateServices();

        object detector = services.GetRequiredService<IIgnoreElementDetector>();
        object provider = services.GetRequiredService<IIgnoreElementProvider>();
        object manager = services.GetRequiredService<IIgnoreListManager>();

        await Assert.That(ReferenceEquals(detector, provider)).IsTrue();
        await Assert.That(ReferenceEquals(provider, manager)).IsTrue();
        await Assert.That(ReferenceEquals(manager, services.GetRequiredService<IIgnoreListManager>())).IsTrue();
    }

    [Test]
    public async Task Element_without_entry_is_not_ignored()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();

        bool ignored = services.GetRequiredService<IIgnoreElementDetector>()
            .IsElementIgnored("TEST-001", wall);
        int entries = services.GetRequiredService<IIgnoreListManager>()
            .GetEntries(_document!).Count;

        await Assert.That(ignored).IsFalse();
        await Assert.That(entries).IsEqualTo(0);
    }

    [Test]
    public async Task Ignore_without_transaction_fails()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();

        IgnoreElementFeedback feedback = WithUiCulture(
            "en-US",
            () => services.GetRequiredService<IIgnoreElementProvider>().Ignore("TEST-001", wall));

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Failed);
        await Assert.That(feedback.Message).IsEqualTo("The ignore list could not be updated.");
    }

    [Test]
    public async Task Ignore_writes_entry_and_detector_finds_it()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            feedback = provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
        await Assert.That(manager.GetEntries(_document!).Count).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document!)[0].Code).IsEqualTo("TEST-001");
        await Assert.That(manager.GetEntries(_document!)[0].Exists).IsTrue();
    }

    [Test]
    public async Task Ignore_appends_codes_as_separate_entries()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();

        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("FIRST", wall);
            provider.Ignore("SECOND", wall);
            transaction.Commit();
        }

        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Ignore_does_not_duplicate_existing_code()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();

        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            IgnoreElementFeedback repeated = provider.Ignore("TEST-001", wall);
            transaction.Commit();

            await Assert.That(repeated.Result).IsEqualTo(IgnoreElementResult.Success);
        }

        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Detector_matches_complete_code_only()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(detector.IsElementIgnored("TEST", wall)).IsFalse();
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
    }

    [Test]
    public async Task Ignore_sees_entry_inside_the_same_transaction()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        bool ignoredInside;
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            ignoredInside = detector.IsElementIgnored("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(ignoredInside).IsTrue();
    }

    [Test]
    public async Task Ignore_rollback_leaves_no_entry()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            IgnoreElementFeedback feedback = provider.Ignore("TEST-001", wall);
            transaction.RollBack();

            await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        }

        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsFalse();
        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(0);
    }

    [Test]
    public async Task IgnoreMany_ignores_several_elements_at_once()
    {
        Wall first = CreateWall();
        Wall second = CreateWall(offset: 20, elevation: 10);
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document!, "Ignore elements"))
        {
            transaction.Start();
            feedback = provider.IgnoreMany("TEST-001", [first, second]);
            transaction.Commit();
        }

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(detector.IsElementIgnored("TEST-001", first)).IsTrue();
        await Assert.That(detector.IsElementIgnored("TEST-001", second)).IsTrue();
    }

    [Test]
    public async Task IgnoreMany_is_atomic_when_an_element_cannot_be_ignored()
    {
        Wall wall = CreateWall();
        DataStorage storage = CreateStorage();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document!, "Ignore elements"))
        {
            transaction.Start();
            feedback = provider.IgnoreMany("TEST-001", [wall, storage]);
            transaction.Commit();
        }

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Failed);
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsFalse();
        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Remove_deletes_the_entry()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }

        bool removed;
        using (Transaction transaction = new(_document!, "Remove ignore entry"))
        {
            transaction.Start();
            removed = manager.Remove("TEST-001", wall);
            transaction.Commit();
        }

        await Assert.That(removed).IsTrue();
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsFalse();
    }

    [Test]
    public async Task Deleted_element_is_listed_as_missing_and_RemoveMissing_cleans_it()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();
        using (Transaction transaction = new(_document!, "Ignore element"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            transaction.Commit();
        }
        using (Transaction transaction = new(_document!, "Delete wall"))
        {
            transaction.Start();
            _document!.Delete(wall.Id);
            transaction.Commit();
        }

        await Assert.That(manager.GetEntries(_document).Count).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document)[0].Exists).IsFalse();

        int removed;
        using (Transaction transaction = new(_document, "Remove missing entries"))
        {
            transaction.Start();
            removed = manager.RemoveMissing(_document);
            transaction.Commit();
        }

        await Assert.That(removed).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_removes_every_entry()
    {
        Wall first = CreateWall();
        Wall second = CreateWall(offset: 20, elevation: 10);
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();
        using (Transaction transaction = new(_document!, "Ignore elements"))
        {
            transaction.Start();
            provider.IgnoreMany("TEST-001", [first, second]);
            transaction.Commit();
        }

        int removed;
        using (Transaction transaction = new(_document!, "Clear ignore entries"))
        {
            transaction.Start();
            removed = manager.Clear(_document!);
            transaction.Commit();
        }

        await Assert.That(removed).IsEqualTo(2);
        await Assert.That(manager.GetEntries(_document!).Count).IsEqualTo(0);
    }

    [Test]
    public async Task IgnoreDocument_writes_entry_and_detector_finds_it()
    {
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();

        IgnoreElementFeedback feedback;
        using (Transaction transaction = new(_document!, "Ignore document"))
        {
            transaction.Start();
            feedback = provider.IgnoreDocument("DOC-001", _document!);
            transaction.Commit();
        }

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(detector.IsDocumentIgnored("DOC-001", _document!)).IsTrue();
        await Assert.That(detector.IsDocumentIgnored("DOC-002", _document!)).IsFalse();
        await Assert.That(manager.GetEntries(_document!).Count).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document!)[0].ElementIdValue).IsNull();
        await Assert.That(manager.GetEntries(_document!)[0].Exists).IsTrue();
    }

    [Test]
    public async Task IgnoreDocument_is_idempotent()
    {
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        using (Transaction transaction = new(_document!, "Ignore document"))
        {
            transaction.Start();
            provider.IgnoreDocument("DOC-001", _document!);
            IgnoreElementFeedback repeated = provider.IgnoreDocument("DOC-001", _document!);
            transaction.Commit();

            await Assert.That(repeated.Result).IsEqualTo(IgnoreElementResult.Success);
        }

        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Document_ignore_suppresses_element_checks_of_the_same_code()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        using (Transaction transaction = new(_document!, "Ignore document"))
        {
            transaction.Start();
            provider.IgnoreDocument("TEST-001", _document!);
            transaction.Commit();
        }

        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
        await Assert.That(detector.IsElementIgnored("TEST-002", wall)).IsFalse();
    }

    [Test]
    public async Task IgnoreDocument_rollback_leaves_no_entry()
    {
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();

        using (Transaction transaction = new(_document!, "Ignore document"))
        {
            transaction.Start();
            IgnoreElementFeedback feedback = provider.IgnoreDocument("DOC-001", _document!);
            transaction.RollBack();

            await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        }

        await Assert.That(detector.IsDocumentIgnored("DOC-001", _document!)).IsFalse();
        await Assert.That(
            services.GetRequiredService<IIgnoreListManager>().GetEntries(_document!).Count).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveDocument_deletes_the_entry_but_keeps_element_entries()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();
        IIgnoreElementDetector detector = services.GetRequiredService<IIgnoreElementDetector>();
        using (Transaction transaction = new(_document!, "Ignore"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            provider.IgnoreDocument("TEST-001", _document!);
            transaction.Commit();
        }

        bool removed;
        using (Transaction transaction = new(_document!, "Remove document entry"))
        {
            transaction.Start();
            removed = manager.RemoveDocument("TEST-001", _document!);
            transaction.Commit();
        }

        await Assert.That(removed).IsTrue();
        await Assert.That(detector.IsDocumentIgnored("TEST-001", _document!)).IsFalse();
        await Assert.That(detector.IsElementIgnored("TEST-001", wall)).IsTrue();
    }

    [Test]
    public async Task RemoveMissing_keeps_document_entries()
    {
        Wall wall = CreateWall();
        using ServiceProvider services = CreateServices();
        IIgnoreElementProvider provider = services.GetRequiredService<IIgnoreElementProvider>();
        IIgnoreListManager manager = services.GetRequiredService<IIgnoreListManager>();
        using (Transaction transaction = new(_document!, "Ignore"))
        {
            transaction.Start();
            provider.Ignore("TEST-001", wall);
            provider.IgnoreDocument("DOC-001", _document!);
            transaction.Commit();
        }
        using (Transaction transaction = new(_document!, "Delete wall"))
        {
            transaction.Start();
            _document!.Delete(wall.Id);
            transaction.Commit();
        }

        int removed;
        using (Transaction transaction = new(_document, "Remove missing entries"))
        {
            transaction.Start();
            removed = manager.RemoveMissing(_document);
            transaction.Commit();
        }

        await Assert.That(removed).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document).Count).IsEqualTo(1);
        await Assert.That(manager.GetEntries(_document)[0].ElementIdValue).IsNull();
    }

    private Wall CreateWall(double offset = 0, double elevation = 0)
    {
        using Transaction transaction = new(_document!, "Create test wall");
        transaction.Start();
        Level level = Level.Create(_document!, elevation);
        Wall wall = Wall.Create(
            _document!,
            Line.CreateBound(new XYZ(0, offset, 0), new XYZ(10, offset, 0)),
            level.Id,
            false);
        transaction.Commit();
        return wall;
    }

    private DataStorage CreateStorage()
    {
        using Transaction transaction = new(_document!, "Create test storage");
        transaction.Start();
        DataStorage storage = DataStorage.Create(_document!);
        transaction.Commit();
        return storage;
    }

    private static ServiceProvider CreateServices()
    {
        ServiceCollection services = new();
        // A new query service per provider: the test cache never invalidates on transactions.
        services.AddSingleton(TestDocumentQueries.Create());
        services.AddElementIgnoringModule();
        return services.BuildServiceProvider();
    }

    private static T WithUiCulture<T>(string cultureName, Func<T> action)
    {
        System.Globalization.CultureInfo previousCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
            return action();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
