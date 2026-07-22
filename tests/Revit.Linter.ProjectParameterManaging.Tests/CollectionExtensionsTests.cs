using Nice3point.TUnit.Revit;
using Revit.Linter.ProjectParameterManaging.Infrastructure.Extensions;

namespace Revit.Linter.ProjectParameterManaging.Tests;

public sealed class CollectionExtensionsTests : RevitApiTest
{
    [Test]
    public async Task SetEquals_ignores_item_order()
    {
        bool result = new[] { 1, 2, 3 }.SetEquals([3, 1, 2]);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task SetEquals_returns_false_for_different_items()
    {
        bool result = new[] { 1, 2, 3 }.SetEquals([1, 2, 4]);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task SetEquals_returns_false_for_different_counts()
    {
        bool result = new[] { 1, 2 }.SetEquals([1, 2, 2]);

        await Assert.That(result).IsFalse();
    }
}
