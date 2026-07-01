using System.Windows;
using System.Windows.Media;

namespace Revit.Linter.StatusBar.Infrasructure.Extensions;

/// <summary>
/// VisualTreeHelperUtils
/// </summary>
internal static class VisualTreeHelperUtils
{
    /// <summary>
    /// FindParent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="child"></param>
    /// <returns></returns>
    public static T? FindParent<T>(this DependencyObject child) where T : DependencyObject
    {
        if (child is null) return null;

        var parentElement = VisualTreeHelper.GetParent(child);

        while (parentElement != null)
        {
            if (parentElement is T parent)
                return parent;

            parentElement = VisualTreeHelper.GetParent(parentElement);
        }

        return null;
    }

    /// <summary>
    /// FindParent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="child"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T? FindParent<T>(this DependencyObject child, string name) where T : FrameworkElement
    {
        if (child is null) return null;

        var parentElement = VisualTreeHelper.GetParent(child);

        while (parentElement != null)
        {
            if (parentElement is T parent && parent.Name == name)
                return parent;

            parentElement = VisualTreeHelper.GetParent(parentElement);
        }

        return null;
    }

    /// <summary>
    /// FindChild
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static T? FindChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        if (parent is null) return null;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var childElement = VisualTreeHelper.GetChild(parent, i);

            if (childElement is T child)
                return child;

            if (childElement.FindChild<T>() is T result)
                return result;
        }

        return null;
    }

    /// <summary>
    /// FindChild
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T? FindChild<T>(this DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is null) return null;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var childElement = VisualTreeHelper.GetChild(parent, i);

            if (childElement is T child && child.Name == name)
                return child;

            if (childElement.FindChild<T>(name) is T result)
                return result;
        }

        return null;
    }
}