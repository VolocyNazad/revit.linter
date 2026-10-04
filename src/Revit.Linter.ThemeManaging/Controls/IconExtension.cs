using System.Windows.Markup;

namespace Revit.Linter.ThemeManaging.Controls;

/// <summary>
/// Creates a <see cref="PathIcon"/> in markup, for example as the content of a button.
/// </summary>
[MarkupExtensionReturnType(typeof(PathIcon))]
public sealed class IconExtension : MarkupExtension
{
    /// <summary>
    /// Initializes the extension without an icon; set <see cref="Kind"/> afterwards.
    /// </summary>
    public IconExtension()
    {
    }

    /// <summary>
    /// Initializes the extension with the icon to create.
    /// </summary>
    /// <param name="kind">The icon to draw.</param>
    public IconExtension(IconKind kind) => Kind = kind;

    /// <summary>
    /// Gets or sets the icon to draw.
    /// </summary>
    [ConstructorArgument("kind")]
    public IconKind Kind { get; set; }

    /// <summary>
    /// Creates a new icon element for each use of the extension.
    /// </summary>
    /// <param name="serviceProvider">The markup service provider; not used.</param>
    /// <returns>A new <see cref="PathIcon"/> showing <see cref="Kind"/>.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider) => new PathIcon { Kind = Kind };
}
