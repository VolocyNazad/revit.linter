using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ValueStore.Abstractions.Services;

namespace Revit.Linter.ValueStore.Tests;

public sealed class DiagnosticIdOverrideTests
{
    [Fact]
    public void Initializes_from_store_and_persists_application_changes()
    {
        var settings = new ElementDiagnosticOverridesSettings();
        settings.Overrides["TEST"] = new DiagnosticOverrideSettings
        {
            Severity = DiagnosticSeverity.Error,
            IsActive = false,
        };
        var store = new ValueStoreStub<ElementDiagnosticOverridesSettings>(settings);
        using var item = new ElementDiagnosticIdOverride(CreateIdentity(), store);

        Assert.Equal(DiagnosticSeverity.Error, item.Severity);
        Assert.False(item.IsActive);

        item.IsActive = true;

        Assert.Equal(1, store.UpdateCount);
        Assert.True(store.CurrentValue.Overrides["TEST"].IsActive);
    }

    [Fact]
    public void External_change_updates_override_without_writing_back()
    {
        var store = new ValueStoreStub<ElementDiagnosticOverridesSettings>(new());
        using var item = new ElementDiagnosticIdOverride(CreateIdentity(), store);

        store.Publish(new ElementDiagnosticOverridesSettings
        {
            Overrides =
            {
                ["TEST"] = new DiagnosticOverrideSettings
                {
                    Severity = DiagnosticSeverity.Error,
                    IsActive = false,
                },
            },
        });

        Assert.Equal(DiagnosticSeverity.Error, item.Severity);
        Assert.False(item.IsActive);
        Assert.Equal(0, store.UpdateCount);
    }

    [Fact]
    public void Failed_persistence_keeps_previous_state()
    {
        var store = new ValueStoreStub<ElementDiagnosticOverridesSettings>(new())
        {
            UpdateException = new IOException("Write failed"),
        };
        using var item = new ElementDiagnosticIdOverride(CreateIdentity(), store);

        Assert.Throws<IOException>(() => item.IsActive = false);

        Assert.True(item.IsActive);
    }

    private static ElementDiagnosticId CreateIdentity()
        => new("TEST", "Description", "Message", DiagnosticSeverity.Warning, true, false, string.Empty);

    private sealed class ValueStoreStub<T>(T value) : IValueStore<T> where T : class
    {
        private readonly List<Action<T>> _listeners = [];

        public T CurrentValue { get; private set; } = value;
        public int UpdateCount { get; private set; }
        public Exception? UpdateException { get; init; }

        public IDisposable OnChange(Action<T> listener)
        {
            _listeners.Add(listener);
            return new Subscription(() => _listeners.Remove(listener));
        }

        public void Update(Action<T> change)
        {
            if (UpdateException is not null) throw UpdateException;
            UpdateCount++;
            change(CurrentValue);
            Publish(CurrentValue);
        }

        public void Publish(T value)
        {
            CurrentValue = value;
            foreach (var listener in _listeners.ToArray()) listener(value);
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }
}
