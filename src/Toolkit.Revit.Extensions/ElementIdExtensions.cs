using Autodesk.Revit.DB;

namespace Toolkit.Revit.Extensions;

public static class ElementIdExtensions
{
    extension(ElementId id)
    {
        public bool IsCategory(BuiltInCategory category)
        {
#if BEFORE2024
            return id.IntegerValue == (int)category;
#else
            return id.Value == (long)category;
#endif
        }

        public BuiltInCategory ToBuiltInCategory()
        {
#if BEFORE2024
            return (BuiltInCategory)id.IntegerValue;
#else
            return (BuiltInCategory)id.Value;
#endif
        }

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
