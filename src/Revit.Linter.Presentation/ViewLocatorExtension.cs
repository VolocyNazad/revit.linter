using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Markup;

namespace Revit.Linter.Presentation;

/// <summary>
/// Resolves a WPF view from dependency injection by applying the repository's ViewModel-to-View naming convention.
/// </summary>
/// <param name="viewModelType">The view-model type whose matching view is requested.</param>
[MarkupExtensionReturnType(typeof(FrameworkElement))]
public sealed class ViewLocatorExtension(Type viewModelType) : MarkupExtension
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets or sets the view-model type used to locate the view.
    /// </summary>
    [ConstructorArgument("viewModelType")]
    public Type ViewModelType { get; set; } = viewModelType;

    /// <summary>
    /// Initializes the process-wide service provider used by view-locator markup extensions.
    /// </summary>
    /// <param name="serviceProvider">The application service provider that contains registered views.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    /// <remarks>The first successful initialization wins; subsequent calls do not replace the provider.</remarks>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        Interlocked.CompareExchange(ref _serviceProvider, serviceProvider, null);
    }

    /// <summary>
    /// Resolves the convention-matched view from the initialized application service provider.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider for the current markup-extension invocation.</param>
    /// <returns>The registered view corresponding to <see cref="ViewModelType"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The locator is not initialized, the view-model type does not follow the naming convention,
    /// or the corresponding view type cannot be found.
    /// </exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        IServiceProvider provider = _serviceProvider
            ?? throw new InvalidOperationException("The view locator has not been initialized.");
        Type viewType = ResolveViewType(ViewModelType);
        return provider.GetRequiredService(viewType);
    }

    private static Type ResolveViewType(Type viewModelType)
    {
        const string viewModelSuffix = "ViewModel";
        if (!viewModelType.Name.EndsWith(viewModelSuffix, StringComparison.Ordinal))
            throw new InvalidOperationException($"View model type '{viewModelType.FullName}' must end with '{viewModelSuffix}'.");

        string? viewModelNamespace = viewModelType.Namespace;
        if (viewModelNamespace is null || viewModelNamespace.IndexOf(".ViewModels", StringComparison.Ordinal) < 0)
            throw new InvalidOperationException($"View model type '{viewModelType.FullName}' must be located in a ViewModels namespace.");

        string viewNamespace = viewModelNamespace.Replace(".ViewModels", ".Views");
        string viewName = viewModelType.Name.Substring(0, viewModelType.Name.Length - viewModelSuffix.Length) + "View";
        string viewFullName = $"{viewNamespace}.{viewName}";
        return viewModelType.Assembly.GetType(viewFullName)
            ?? throw new InvalidOperationException($"View '{viewFullName}' was not found for '{viewModelType.FullName}'.");
    }
}
