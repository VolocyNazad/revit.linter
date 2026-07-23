namespace Revit.Linter.Build.Options;

public sealed record BuildOptions
{
    public string OutputDirectory { get; init; } = "output";
}

public sealed record PublishOptions
{
    public string? Version { get; init; }
    public bool Draft { get; init; }
}
