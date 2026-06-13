namespace Revit.Linter.ParameterElementDiagnostics.Infrastructure.Extensions;

public static class ElementIdExtensions
{
    extension(ElementId id)
    {
#if BEFORE2024
        public int Value()
        {
            return id.IntegerValue;
        }
#else
        public long Value()
        {
            return id.Value;
        }
#endif

    }
}
