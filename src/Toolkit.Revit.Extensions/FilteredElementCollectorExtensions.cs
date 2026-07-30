using Autodesk.Revit.DB;

namespace Toolkit.Revit.Extensions;

public static class FilteredElementCollectorExtensions
{
    extension(FilteredElementCollector collector)
    {
        public FilteredElementCollector OfClass<T>() where T : Element
            => collector.OfClass(typeof(T));

        /// <summary>
        /// Applies native class and element/type filters for the requested Revit element type.
        /// </summary>
        public FilteredElementCollector WhereElementIs<T>() where T : Element
        {
            collector.OfClass<T>();

            return typeof(ElementType).IsAssignableFrom(typeof(T))
                ? collector.WhereElementIsElementType()
                : collector.WhereElementIsNotElementType();
        }
    }
}
