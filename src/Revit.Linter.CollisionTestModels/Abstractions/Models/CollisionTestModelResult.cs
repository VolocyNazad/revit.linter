namespace Revit.Linter.CollisionTestModels.Abstractions.Models;

/// <summary>
/// Describes the documents produced for collision-diagnostic testing: a saved host
/// with two linked models placed by shared coordinates.
/// </summary>
/// <param name="HostTitle">Title of the created host document.</param>
/// <param name="HostPath">File path of the saved host document.</param>
/// <param name="LinkAPath">File path of the first saved link model.</param>
/// <param name="LinkBPath">File path of the second saved link model.</param>
public sealed record CollisionTestModelResult(string HostTitle, string HostPath, string LinkAPath, string LinkBPath);
