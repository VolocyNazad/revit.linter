namespace Revit.Linter.CollisionDiagnostics.RevitTests.Infrastructure;

/// <summary>Names the host and two link documents built for the collision-link tests.</summary>
/// <param name="HostTitle">The title of the saved host document.</param>
/// <param name="HostPath">The path of the saved host document.</param>
/// <param name="LinkAPath">The path of the first saved link document.</param>
/// <param name="LinkBPath">The path of the second saved link document.</param>
internal sealed record CollisionTestModelResult(string HostTitle, string HostPath, string LinkAPath, string LinkBPath);
