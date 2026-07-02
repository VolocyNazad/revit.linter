using Autodesk.Revit.DB;

namespace Toolkit.Revit.Extensions;

public static class ElementFilterUtils
{
    public static ElementFilter AllFilter() =>
        new LogicalOrFilter(new ElementIsElementTypeFilter(true), new ElementIsElementTypeFilter(false));

    public static ElementFilter EmptyFilter() =>
        new LogicalAndFilter(new ElementIsElementTypeFilter(true), new ElementIsElementTypeFilter(false));

    public static ElementFilter CategoryTypeFilter(Document doc, CategoryType categoryType = CategoryType.Model, bool inverse = false)
    {
        Categories categories = doc.Settings.Categories;
        ICollection<ElementId> ids = new List<ElementId>(categories.Size);
        foreach (Category category in categories)
        {
            if (category.CategoryType == categoryType) ids.Add(category.Id);
        }

        return new ElementMulticategoryFilter(ids, inverse);
    }
}
