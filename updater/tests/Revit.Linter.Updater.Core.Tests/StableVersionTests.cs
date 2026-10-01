using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Tests;

public sealed class StableVersionTests
{
    [Theory]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("V1.2.3+abcdef", 1, 2, 3)]
    [InlineData("1.2.3.42", 1, 2, 3)]
    public void TryParse_SupportedValue_ReturnsStableVersion(
        string value, int major, int minor, int patch)
    {
        bool parsed = StableVersion.TryParse(value, out StableVersion version);

        Assert.True(parsed);
        Assert.Equal(new StableVersion(major, minor, patch), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("1.2.3-preview.1")]
    [InlineData("release")]
    public void TryParse_UnsupportedValue_ReturnsFalse(string? value) =>
        Assert.False(StableVersion.TryParse(value, out _));

    [Fact]
    public void CompareTo_UsesMajorMinorPatchOrder()
    {
        Assert.True(new StableVersion(2, 0, 0).CompareTo(new StableVersion(1, 99, 99)) > 0);
        Assert.True(new StableVersion(1, 3, 0).CompareTo(new StableVersion(1, 2, 99)) > 0);
        Assert.True(new StableVersion(1, 2, 4).CompareTo(new StableVersion(1, 2, 3)) > 0);
    }
}
