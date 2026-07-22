#if BEFORE2024
using Autodesk.Revit.DB;

namespace Toolkit.Revit.Extensions;

public static class CategoryExtensions
{
    extension(Category category)
    {
        public BuiltInCategory BuiltInCategory => (BuiltInCategory)category.Id.IntegerValue;
    }
}
#endif