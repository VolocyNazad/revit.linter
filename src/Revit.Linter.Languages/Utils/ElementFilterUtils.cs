namespace Revit.Linter.Languages.Utils;

internal static class ElementFilterUtils
{
    public static ElementFilter GetElementClassFilter(string className)
    {
        Type type = RevitClassUtils.GetType(className);
        ElementClassFilter filter = new(type);
        return filter;
    }
    public static ElementFilter GetElementCategoryFilter(string builtInCategoryName)
    {
#if AFTER2025
        BuiltInCategory builtInCategory = Enum.Parse<BuiltInCategory>(builtInCategoryName);
#else
        BuiltInCategory builtInCategory = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), builtInCategoryName);
#endif
        ElementCategoryFilter filter = new(builtInCategory);
        return filter;
    }
    public static ElementFilter GetElementWorksetFilter(Document document, string worksetName)
    {
        Workset workset = new FilteredWorksetCollector(document).ToWorksets().First(i => i.Name == worksetName);
        ElementWorksetFilter filter = new(workset.Id);
        return filter;
    }
}

