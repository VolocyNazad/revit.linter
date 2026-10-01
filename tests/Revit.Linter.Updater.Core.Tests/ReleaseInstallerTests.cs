using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class ReleaseInstallerTests
{
    private static readonly StableVersion Version = new(2, 0, 0);
    private const string Name = "RevitLinter-2.0.0.msi";
    private const string Url =
        "https://github.com/VolocyNazad/revit.linter/releases/download/v2.0.0/RevitLinter-2.0.0.msi";
    private static readonly string Digest = "sha256:" + new string('A', 64);

    [Fact]
    public void TryCreate_ValidMetadata_NormalizesDigest()
    {
        bool created = ReleaseInstaller.TryCreate(Version, Name, Url, 1024, Digest, out var installer);

        Assert.True(created);
        Assert.Equal(new string('a', 64), installer!.Sha256);
    }

    [Theory]
    [InlineData("RevitLinter.msi", Url, 1024)]
    [InlineData(Name, "http://github.com/VolocyNazad/revit.linter/releases/download/v2.0.0/RevitLinter-2.0.0.msi", 1024)]
    [InlineData(Name, "https://github.com/Other/repository/releases/download/v2.0.0/RevitLinter-2.0.0.msi", 1024)]
    [InlineData(Name, Url, 0)]
    [InlineData(Name, Url, 1073741825)]
    public void TryCreate_InvalidMetadata_ReturnsFalse(string name, string url, long size) =>
        Assert.False(ReleaseInstaller.TryCreate(Version, name, url, size, Digest, out _));
}
