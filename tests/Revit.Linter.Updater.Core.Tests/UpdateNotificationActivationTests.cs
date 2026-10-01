using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class UpdateNotificationActivationTests
{
    [Fact]
    public void TryParse_TrustedReleaseAction_ReturnsActivation()
    {
        string arguments = "action=release&version=2.0.0&url=" + Uri.EscapeDataString(
            "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0");

        bool parsed = UpdateNotificationActivation.TryParse(arguments, out var activation);

        Assert.True(parsed);
        UpdateNotificationActivation actual = Assert.IsType<UpdateNotificationActivation>(activation);
        Assert.Equal(UpdateNotificationAction.ReleaseNotes, actual.Action);
        Assert.Equal(new StableVersion(2, 0, 0), actual.Version);
        Assert.NotNull(actual.ReleasePage);
        Assert.Null(actual.Installer);
    }

    [Fact]
    public void TryParse_TrustedDownloadAction_ReturnsInstaller()
    {
        string arguments = ValidDownloadArguments();

        bool parsed = UpdateNotificationActivation.TryParse(arguments, out var activation);

        Assert.True(parsed);
        UpdateNotificationActivation actual = Assert.IsType<UpdateNotificationActivation>(activation);
        Assert.Equal(UpdateNotificationAction.Download, actual.Action);
        Assert.Equal("RevitLinter-2.0.0.msi", actual.Installer!.Name);
        Assert.Equal(1234, actual.Installer.Size);
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
        string arguments = "action=release&version=2.0.0&url=" + Uri.EscapeDataString(url);

        Assert.False(UpdateNotificationActivation.TryParse(arguments, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("action=unknown&version=2.0.0")]
    [InlineData("action=skip&version=preview")]
    [InlineData("action=download&version=2.0.0")]
    public void TryParse_InvalidArguments_ReturnsFalse(string arguments) =>
        Assert.False(UpdateNotificationActivation.TryParse(arguments, out _));

    [Theory]
    [InlineData("name", "Other.msi")]
    [InlineData("downloadUrl", "https://example.com/RevitLinter-2.0.0.msi")]
    [InlineData("size", "0")]
    [InlineData("digest", "sha256:invalid")]
    public void TryParse_InvalidInstallerMetadata_ReturnsFalse(string key, string value)
    {
        var values = System.Web.HttpUtility.ParseQueryString(ValidDownloadArguments());
        values[key] = value;

        Assert.False(UpdateNotificationActivation.TryParse(values.ToString(), out _));
    }

    private static string ValidDownloadArguments()
    {
        var values = System.Web.HttpUtility.ParseQueryString(string.Empty);
        values["action"] = "download";
        values["version"] = "2.0.0";
        values["url"] = "https://github.com/VolocyNazad/revit.linter/releases/tag/v2.0.0";
        values["name"] = "RevitLinter-2.0.0.msi";
        values["downloadUrl"] =
            "https://github.com/VolocyNazad/revit.linter/releases/download/v2.0.0/RevitLinter-2.0.0.msi";
        values["size"] = "1234";
        values["digest"] = "sha256:" + new string('a', 64);
        return values.ToString()!;
    }
}
