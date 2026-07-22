using Nice3point.TUnit.Revit;
using Revit.Linter.ElementIgnoring.Abstractions.Models;

namespace Revit.Linter.ElementIgnoring.Tests;

public sealed class IgnoreElementFeedbackTests : RevitApiTest
{
    [Test]
    public async Task Success_creates_successful_feedback_without_message()
    {
        IgnoreElementFeedback feedback = IgnoreElementFeedback.Success();

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Success);
        await Assert.That(feedback.Message).IsNull();
    }

    [Test]
    public async Task Failed_preserves_failure_message()
    {
        IgnoreElementFeedback feedback = IgnoreElementFeedback.Failed("Failure");

        await Assert.That(feedback.Result).IsEqualTo(IgnoreElementResult.Failed);
        await Assert.That(feedback.Message).IsEqualTo("Failure");
    }
}
