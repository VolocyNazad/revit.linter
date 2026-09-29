#if BEFORE2024
using Revit.Sugar;
#endif

namespace Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

internal static class CategoryExtensions
{
    public static bool IsBuiltInCategory(this Category category, BuiltInCategory builtInCategory)
    {
#if BEFORE2024
        return category.Id.IsCategory(builtInCategory);
#else
        return category.BuiltInCategory == builtInCategory;
#endif
    }
}
