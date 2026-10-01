using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateNotificationActivationTests
{
    [Theory]
    [InlineData("download", UpdateNotificationAction.Download)]
    [InlineData("release", UpdateNotificationAction.ReleaseNotes)]
    public void TryParse_TrustedNavigationAction_ReturnsActivation(
        string action,
        UpdateNotificationAction expected)
    {
        string arguments = $"action={action}&version=2.0.0&url=" + Uri.EscapeDataString(
            "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0");

        bool parsed = UpdateNotificationActivation.TryParse(arguments, out var activation);

        Assert.True(parsed);
        UpdateNotificationActivation actual = Assert.IsType<UpdateNotificationActivation>(activation);
        Assert.Equal(expected, actual.Action);
        Assert.Equal(new StableVersion(2, 0, 0), actual.Version);
        Assert.NotNull(actual.ReleasePage);
    }

    [Theory]
    [InlineData("later", UpdateNotificationAction.Later)]
    [InlineData("skip", UpdateNotificationAction.SkipVersion)]
    public void TryParse_LocalAction_DoesNotRequireUrl(
        string action,
        UpdateNotificationAction expected)
    {
        bool parsed = UpdateNotificationActivation.TryParse(
            $"action={action}&version=2.0.0", out var activation);

        Assert.True(parsed);
        UpdateNotificationActivation actual = Assert.IsType<UpdateNotificationActivation>(activation);
        Assert.Equal(expected, actual.Action);
        Assert.Null(actual.ReleasePage);
    }

    [Theory]
    [InlineData("http://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0")]
    [InlineData("https://example.com/VolocyNazad/revit.linter/releases/tag/v2.0.0")]
    [InlineData("https://github.com/Other/repository/releases/tag/v2.0.0")]
    [InlineData("https://user@github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0")]
    [InlineData("https://github.com:444/VolocyNazad/revit.linter/releases/tag/v2.0.0")]
    [InlineData("https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0#fragment")]
    public void TryParse_UntrustedNavigationUrl_ReturnsFalse(string url)
    {
        string arguments = "action=download&version=2.0.0&url=" + Uri.EscapeDataString(url);

        Assert.False(UpdateNotificationActivation.TryParse(arguments, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("action=unknown&version=2.0.0")]
    [InlineData("action=skip&version=preview")]
    [InlineData("action=download&version=2.0.0")]
    public void TryParse_InvalidArguments_ReturnsFalse(string arguments) =>
        Assert.False(UpdateNotificationActivation.TryParse(arguments, out _));
}
