using MaterialDesignThemes.Wpf;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Revit.Linter.ThemeManaging.Services;

internal sealed partial class ThemeService : IThemeService
{
    private readonly List<WeakReference<FrameworkElement>> _elements = [];
    private bool _isDarkTheme;

    public void Register(FrameworkElement element)
    {
        if (element is null)
            throw new ArgumentNullException(nameof(element));

        _elements.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (_elements.Any(reference => reference.TryGetTarget(out var target) && ReferenceEquals(target, element)))
            return;

        _elements.Add(new WeakReference<FrameworkElement>(element));
        ApplyTheme(element, _isDarkTheme);
    }

    public void ChangeTheme(bool isDarkTheme)
    {
        _isDarkTheme = isDarkTheme;

        for (int index = _elements.Count - 1; index >= 0; index--)
        {
            if (_elements[index].TryGetTarget(out var element))
                ApplyTheme(element, isDarkTheme);
            else
                _elements.RemoveAt(index);
        }
    }

    private static void ApplyTheme(DependencyObject root, bool isDarkTheme)
    {
        var visited = new HashSet<DependencyObject>(DependencyObjectReferenceComparer.Instance);
        ApplyTheme(root, isDarkTheme, visited);
    }

    private static void ApplyTheme(DependencyObject element, bool isDarkTheme, ISet<DependencyObject> visited)
    {
        if (!visited.Add(element)) return;

        if (element is FrameworkElement frameworkElement)
            UpdateResources(frameworkElement.Resources, isDarkTheme);
        else if (element is FrameworkContentElement frameworkContentElement)
            UpdateResources(frameworkContentElement.Resources, isDarkTheme);

        foreach (object child in LogicalTreeHelper.GetChildren(element))
        {
            if (child is DependencyObject dependencyObject)
                ApplyTheme(dependencyObject, isDarkTheme, visited);
        }

        if (element is not Visual and not Visual3D) return;

        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
            ApplyTheme(VisualTreeHelper.GetChild(element, index), isDarkTheme, visited);
    }

    private static void UpdateResources(ResourceDictionary resources, bool isDarkTheme)
    {
        foreach (ResourceDictionary dictionary in resources.MergedDictionaries)
        {
            if (dictionary is BundledTheme bundledTheme)
                bundledTheme.BaseTheme = isDarkTheme ? BaseTheme.Dark : BaseTheme.Light;

            UpdateResources(dictionary, isDarkTheme);
        }
    }
}
