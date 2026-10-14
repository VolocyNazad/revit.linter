using Microsoft.Extensions.Logging;
using Revit.Linter.ElementAccentor.Abstractions.Services;

namespace Revit.Linter.ElementAccentor.Services;

internal sealed class SetCategoryVisibilityService(ILogger<SetCategoryVisibilityService> logger)
    : ISetCategoryVisibilityService
{
    private readonly ILogger<SetCategoryVisibilityService> _logger = logger;

    public IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyList<BuiltInCategory> categories)
    {
        List<ElementId> hidden = Execute(document, view, categories);
        if (hidden.Count == 0)
            return new RestoreSession(static () => { });

        return new RestoreSession(() =>
        {
            if (document.IsValidObject && view.IsValidObject)
                TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("restoreCategoryVisibility_transaction"), () =>
                {
                    foreach (ElementId categoryId in hidden)
                        view.SetCategoryHidden(categoryId, false);
                });
        });
    }

    private List<ElementId> Execute(Document document, View? view, IReadOnlyList<BuiltInCategory> categories)
    {
        if (categories.Count == 0)
        {
            _logger.LogInformation("Failed to set category visibility. The category list is empty.");
            return [];
        }
        if (view is null)
        {
            _logger.LogInformation("Active view not found.");
            return [];
        }

        HashSet<ElementId> visible = ResolveVisibleCategories(document, categories);
        List<ElementId> hidden =
        [
            .. document.Settings.Categories
                .Cast<Category>()
                .Where(category => category.Parent is null)
                .Where(category => !visible.Contains(category.Id))
                .Where(category => view.CanCategoryBeHidden(category.Id))
                .Where(category => !view.GetCategoryHidden(category.Id))
                .Select(category => category.Id),
        ];
        if (hidden.Count == 0)
        {
            _logger.LogInformation("No categories to hide: only the listed categories are shown already.");
            return [];
        }

        TransactionExecutor.Execute(document, ElementAccentorLocalizations.GetString("applyCategoryVisibility_transaction"), () =>
        {
            foreach (ElementId categoryId in hidden)
                view.SetCategoryHidden(categoryId, true);
        });

        _logger.LogInformation(
            "Set category visibility in view {ViewName}: {VisibleCategoryCount} categories stay visible, {HiddenCategoryCount} categories hidden.",
            view.Name, visible.Count, hidden.Count);
        return hidden;
    }

    // Categories absent from the document are ignored rather than failing the step: one
    // configuration runs across documents whose category sets differ.
    private HashSet<ElementId> ResolveVisibleCategories(Document document, IReadOnlyList<BuiltInCategory> categories)
    {
        HashSet<ElementId> visible = [];
        foreach (BuiltInCategory builtInCategory in categories)
        {
            Category? category = Category.GetCategory(document, builtInCategory);
            if (category is null)
            {
                _logger.LogInformation(
                    "Category {BuiltInCategory} is not present in document {DocumentTitle}; keeping the remaining ones.",
                    builtInCategory, document.Title);
                continue;
            }
            visible.Add(category.Id);
        }
        return visible;
    }
}
