using Revit.Linter.Updater.Core.Services;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdaterInstanceGateTests
{
    [Fact]
    public void TryAcquire_ConcurrentInstance_ReturnsFalse()
    {
        string name = CreateName();
        using var owner = new UpdaterInstanceGate(name);
        using var concurrent = new UpdaterInstanceGate(name);

        Assert.True(owner.TryAcquire());
        Assert.False(concurrent.TryAcquire());
    }

    [Fact]
    public void RequestManualCheck_ActiveInstanceConsumesSignalOnce()
    {
        string name = CreateName();
        using var owner = new UpdaterInstanceGate(name);
        using var concurrent = new UpdaterInstanceGate(name);
        Assert.True(owner.TryAcquire());

        concurrent.RequestManualCheck();

        Assert.True(owner.ConsumeManualCheckRequest());
        Assert.False(owner.ConsumeManualCheckRequest());
    }

    [Fact]
    public void Dispose_OwnerReleasesGateForNextInstance()
    {
        string name = CreateName();
        using (var owner = new UpdaterInstanceGate(name))
            Assert.True(owner.TryAcquire());

        using var next = new UpdaterInstanceGate(name);
        Assert.True(next.TryAcquire());
    }

    private static string CreateName() => $"Volocy.Revit.Linter.Tests.{Guid.NewGuid():N}";
}
