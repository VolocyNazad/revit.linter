using Autodesk.Revit.ApplicationServices;
using Nice3point.TUnit.Revit;

namespace Revit.Linter.Languages.RevitTests;

public sealed class ApplicationPropertyFormulaTests : RevitApiTest
{
    [Test]
    public async Task Version_number_property_is_available()
    {
        Func<Application, bool> formula =
            PropertyFormulaCompiler.Compile<Application, bool>("!isnullorempty(property('VersionNumber'))");

        bool result = formula.Invoke(Application);

        await Assert.That(result).IsTrue();
    }
}
