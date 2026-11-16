using Revit.Linter.Core.Abstractions;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Tests;

public sealed class RevitUpdateNotificationTests
{
    [Fact]
    public void HasTrustedReleasePage_OfficialRepositoryRelease_ReturnsTrue()
    {
        var notification = new RevitUpdateNotification(
            "3.2.0",
            $"{ProductIdentity.RepositoryUrl}/releases/tag/v3.2.0");

        Assert.True(notification.HasTrustedReleasePage());
    }

    [Theory]
    [InlineData("http://github.com/VolocyNazad/revit.linter/releases/tag/v3.2.0")]
    [InlineData("https://example.com/VolocyNazad/revit.linter/releases/tag/v3.2.0")]
    [InlineData("https://github.com/VolocyNazad/revit.linter/issues")]
    [InlineData("https://github.com/other/revit.linter/releases/tag/v3.2.0")]
    [InlineData("https://user@github.com/VolocyNazad/revit.linter/releases/tag/v3.2.0")]
    [InlineData("not a url")]
    public void HasTrustedReleasePage_UntrustedAddress_ReturnsFalse(string releasePage)
    {
        var notification = new RevitUpdateNotification("3.2.0", releasePage);

        Assert.False(notification.HasTrustedReleasePage());
    }
}
