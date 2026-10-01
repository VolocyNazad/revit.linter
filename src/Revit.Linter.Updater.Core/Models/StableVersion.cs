namespace Revit.Linter.Updater.Core.Models;

/// <summary>Represents the stable <c>MAJOR.MINOR.PATCH</c> portion of a product version.</summary>
public readonly record struct StableVersion(int Major, int Minor, int Patch) : IComparable<StableVersion>
{
    /// <summary>Parses a stable release tag or assembly informational version.</summary>
    /// <remarks>
    /// An optional leading <c>v</c>, a fourth numeric assembly component, and build metadata after
    /// <c>+</c> are ignored. Prerelease suffixes are rejected because the updater follows stable releases.
    /// </remarks>
    public static bool TryParse(string? value, out StableVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        ReadOnlySpan<char> text = value.Trim();
        if (text.Length > 1 && text[0] is 'v' or 'V')
            text = text[1..];

        int metadataIndex = text.IndexOf('+');
        if (metadataIndex >= 0)
            text = text[..metadataIndex];

        if (text.Contains('-'))
            return false;

        string[] components = text.ToString().Split('.');
        if (components.Length is < 3 or > 4 ||
            !int.TryParse(components[0], out int major) ||
            !int.TryParse(components[1], out int minor) ||
            !int.TryParse(components[2], out int patch) ||
            major < 0 || minor < 0 || patch < 0)
        {
            return false;
        }

        if (components.Length == 4 && !int.TryParse(components[3], out _))
            return false;

        version = new StableVersion(major, minor, patch);
        return true;
    }

    /// <inheritdoc />
    public int CompareTo(StableVersion other)
    {
        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        int minorComparison = Minor.CompareTo(other.Minor);
        return minorComparison != 0 ? minorComparison : Patch.CompareTo(other.Patch);
    }

    /// <summary>Returns whether the left version is older than the right version.</summary>
    public static bool operator <(StableVersion left, StableVersion right) => left.CompareTo(right) < 0;

    /// <summary>Returns whether the left version is newer than the right version.</summary>
    public static bool operator >(StableVersion left, StableVersion right) => left.CompareTo(right) > 0;

    /// <summary>Returns whether the left version is not newer than the right version.</summary>
    public static bool operator <=(StableVersion left, StableVersion right) => left.CompareTo(right) <= 0;

    /// <summary>Returns whether the left version is not older than the right version.</summary>
    public static bool operator >=(StableVersion left, StableVersion right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
