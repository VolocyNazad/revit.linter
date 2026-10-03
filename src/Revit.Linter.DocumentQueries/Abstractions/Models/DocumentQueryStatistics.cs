namespace Revit.Linter.DocumentQueries.Abstractions.Models;

/// <summary>
/// Describes how often one query name was answered from the cache and how long its misses took.
/// </summary>
/// <param name="Query">The query name shared by the counted keys.</param>
/// <param name="Hits">The number of requests answered from the cache.</param>
/// <param name="Misses">The number of requests that had to compute the value.</param>
/// <param name="MissMilliseconds">
/// The time spent computing the missed values. A query computed inside another query's factory is
/// counted in both, so the values of different queries must not be added together.
/// </param>
public sealed record DocumentQueryStatistics(string Query, int Hits, int Misses, double MissMilliseconds);