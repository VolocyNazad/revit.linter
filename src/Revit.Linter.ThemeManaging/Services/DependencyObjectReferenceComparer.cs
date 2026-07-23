using System.Windows;
using System.Runtime.CompilerServices;

namespace Revit.Linter.ThemeManaging.Services;

internal sealed partial class ThemeService
{
    private sealed class DependencyObjectReferenceComparer : IEqualityComparer<DependencyObject>
    {
        public static DependencyObjectReferenceComparer Instance { get; } = new();

        public bool Equals(DependencyObject? x, DependencyObject? y) => ReferenceEquals(x, y);

        public int GetHashCode(DependencyObject obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
