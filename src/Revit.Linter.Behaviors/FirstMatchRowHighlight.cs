using System.Windows;
using System.Windows.Controls;

namespace Revit.Linter.Behaviors;

/// <summary>Moves an onboarding highlight key to the first realized grid row that matches.</summary>
/// <remarks>
/// Only realized containers can carry an effect, so virtualized rows are evaluated when they load
/// and whenever the view changes. The key never names an observer; it only marks the row.
/// </remarks>
public static class FirstMatchRowHighlight
{
    /// <summary>
    /// Sets <paramref name="highlightKey"/> on the first realized row accepted by <paramref name="isMatch"/>
    /// and clears it from every other realized row.
    /// </summary>
    /// <param name="grid">The grid whose realized rows are evaluated in view order.</param>
    /// <param name="isMatch">Whether the row item is the highlight target.</param>
    /// <param name="highlightKey">The onboarding key to set on the matching row.</param>
    public static void Refresh(DataGrid grid, Func<object?, bool> isMatch, string highlightKey)
    {
        int firstMatchIndex = FindFirstMatchIndex(grid, isMatch);
        for (int index = 0; index < grid.Items.Count; index++)
        {
            if (grid.ItemContainerGenerator.ContainerFromIndex(index) is not DataGridRow row) continue;
            int rowIndex = grid.ItemContainerGenerator.IndexFromContainer(row);
            OnboardingHighlight.SetKey(
                row,
                rowIndex >= 0 && rowIndex == firstMatchIndex ? highlightKey : null);
        }
    }

    private static int FindFirstMatchIndex(DataGrid grid, Func<object?, bool> isMatch)
    {
        for (int index = 0; index < grid.Items.Count; index++)
            if (isMatch(grid.Items[index]))
                return index;

        return -1;
    }
}
