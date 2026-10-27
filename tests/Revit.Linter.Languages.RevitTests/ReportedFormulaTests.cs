using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Languages.Factories;

namespace Revit.Linter.Languages.RevitTests;

public sealed class ReportedFormulaTests : RevitApiTest
{
    // The formula a user reported as "not working": the conditional evaluates to object, and the check
    // factory must convert that root value to a boolean instead of rejecting the whole formula.
    private const string ReportedFormula =
        "if(!contains(parameter(me, 'Имя'),'Лоджия'), true, parameter(me, 'ADSK_Тип помещения')==3)";

    [Test]
    public async Task Reported_conditional_formula_compiles_into_boolean_element_check()
    {
        CountingFormulaCompilationNotifier notifier = new();
        ElementFunctionFactory factory = new(NullLogger<ElementFunctionFactory>.Instance, notifier);

        Func<Element, bool> check = factory.CreateBoolean(ReportedFormula, fallback: true);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
        await Assert.That(check).IsNotNull();
        await Assert.That(factory.TryValidateBoolean(ReportedFormula)).IsTrue();
    }

    [Test]
    public async Task Conditional_with_boxed_boolean_branch_is_accepted_by_the_check()
    {
        CountingFormulaCompilationNotifier notifier = new();
        ElementFunctionFactory factory = new(NullLogger<ElementFunctionFactory>.Instance, notifier);

        _ = factory.CreateBoolean("if(true, true, false)", fallback: true);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
        await Assert.That(factory.TryValidateBoolean("if(true, true, false)")).IsTrue();
    }

    [Test]
    public async Task Object_branches_keep_the_object_result()
    {
        CountingFormulaCompilationNotifier notifier = new();
        ElementFunctionFactory factory = new(NullLogger<ElementFunctionFactory>.Instance, notifier);

        _ = factory.Create<object>("if(true, parameter(me, 'A'), parameter(me, 'B'))", fallback: string.Empty);

        await Assert.That(notifier.NotificationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Malformed_formula_still_falls_back_and_notifies()
    {
        CountingFormulaCompilationNotifier notifier = new();
        ElementFunctionFactory factory = new(NullLogger<ElementFunctionFactory>.Instance, notifier);

        _ = factory.CreateBoolean("if(true,,false,true)==true", fallback: true);

        await Assert.That(notifier.NotificationCount).IsEqualTo(1);
        await Assert.That(factory.TryValidateBoolean("if(true,,false,true)==true")).IsFalse();
    }

    private sealed class CountingFormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public int NotificationCount { get; private set; }

        public void Notify() => NotificationCount++;
    }
}
