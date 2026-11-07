using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class SetCategoryVisibilityService(ILogger<SetCategoryVisibilityService> logger)
    : ISetCategoryVisibilityService
{
    private readonly ILogger<SetCategoryVisibilityService> _logger = logger;

    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyList<BuiltInCategory> categories,
        CategoryVisibilityMode mode)
    {
        (List<ElementId> hidden, List<ElementId> shown) = Execute(document, view, categories, mode);
        if (hidden.Count == 0 && shown.Count == 0)
            return new RestoreSession(static () => { });

        return new RestoreSession(() =>
        {
            if (!document.IsValidObject || !view.IsValidObject) return;
            TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreCategoryVisibility_transaction"), () =>
            {
                foreach (ElementId categoryId in hidden)
                    view.SetCategoryHidden(categoryId, false);
                foreach (ElementId categoryId in shown)
                    view.SetCategoryHidden(categoryId, true);
            });
        });
    }

    private (List<ElementId> Hidden, List<ElementId> Shown) Execute(
        Document document,
        View? view,
        IReadOnlyList<BuiltInCategory> categories,
        CategoryVisibilityMode mode)
    {
        if (categories.Count == 0)
        {
            _logger.LogInformation("Failed to set category visibility. The category list is empty.");
            return ([], []);
        }
        if (view is null)
        {
            _logger.LogInformation("Active view not found.");
            return ([], []);
        }

        HashSet<ElementId> listed = ResolveCategories(document, categories);
        (List<ElementId> hidden, List<ElementId> shown) = Plan(document, view, listed, mode);
        if (hidden.Count == 0 && shown.Count == 0)
        {
            _logger.LogInformation(
                "No category visibility to change in view {ViewName} in {Mode} mode.",
                view.Name, mode);
            return ([], []);
        }

        TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyCategoryVisibility_transaction"), () =>
        {
            foreach (ElementId categoryId in hidden)
                view.SetCategoryHidden(categoryId, true);
            foreach (ElementId categoryId in shown)
                view.SetCategoryHidden(categoryId, false);
        });

        _logger.LogInformation(
            "Set category visibility in view {ViewName} in {Mode} mode: {ListedCategoryCount} categories listed, " +
            "{HiddenCategoryCount} categories hidden, {ShownCategoryCount} categories shown.",
            view.Name, mode, listed.Count, hidden.Count, shown.Count);
        return (hidden, shown);
    }

    private static (List<ElementId> Hidden, List<ElementId> Shown) Plan(
        Document document,
        View view,
        HashSet<ElementId> listed,
        CategoryVisibilityMode mode) => mode switch
        {
            CategoryVisibilityMode.OnlyVisible =>
                (HideAllExcept(document, view, listed), ShowListed(listed, view)),
            CategoryVisibilityMode.OnlyHidden =>
                (HideListed(listed, view), ShowAllExcept(document, view, listed)),
            CategoryVisibilityMode.Show => ([], ShowListed(listed, view)),
            CategoryVisibilityMode.Hide => (HideListed(listed, view), []),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode), mode, "Unsupported category visibility mode.")
        };

    private static List<ElementId> HideListed(HashSet<ElementId> listed, View view) =>
        [.. listed
            .Where(categoryId => view.CanCategoryBeHidden(categoryId))
            .Where(categoryId => !view.GetCategoryHidden(categoryId))];

    private static List<ElementId> ShowListed(HashSet<ElementId> listed, View view) =>
        [.. listed
            .Where(categoryId => view.CanCategoryBeHidden(categoryId))
            .Where(categoryId => view.GetCategoryHidden(categoryId))];

    private static List<ElementId> HideAllExcept(Document document, View view, HashSet<ElementId> listed) =>
        [.. TopLevelCategories(document)
            .Where(category => !listed.Contains(category.Id))
            .Where(category => view.CanCategoryBeHidden(category.Id))
            .Where(category => !view.GetCategoryHidden(category.Id))
            .Select(category => category.Id)];

    private static List<ElementId> ShowAllExcept(Document document, View view, HashSet<ElementId> listed) =>
        [.. TopLevelCategories(document)
            .Where(category => !listed.Contains(category.Id))
            .Where(category => view.CanCategoryBeHidden(category.Id))
            .Where(category => view.GetCategoryHidden(category.Id))
            .Select(category => category.Id)];

    private static IEnumerable<Category> TopLevelCategories(Document document) =>
        document.Settings.Categories.Cast<Category>().Where(category => category.Parent is null);

    // Categories absent from the document are ignored rather than failing the step: one
    // configuration runs across documents whose category sets differ.
    private HashSet<ElementId> ResolveCategories(Document document, IReadOnlyList<BuiltInCategory> categories)
    {
        HashSet<ElementId> present = [];
        foreach (BuiltInCategory builtInCategory in categories)
        {
            Category? category = Category.GetCategory(document, builtInCategory);
            if (category is null)
            {
                _logger.LogInformation(
                    "Category {BuiltInCategory} is not present in document {DocumentTitle}; skipping it.",
                    builtInCategory, document.Title);
                continue;
            }
            present.Add(category.Id);
        }
        return present;
    }
}
