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
    /// <exception cref="T:System.ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="T:System.InvalidOperationException">
    /// The view-model type does not follow the naming convention or the corresponding view type cannot be found.
    /// </exception>
    /// <remarks>
    /// The XAML designer runs without the application service provider. It receives the view built directly from
    /// its type with the matching design-time data applied, so the preview shows the composed sub-view instead of
    /// failing the designer; a view that cannot be built without application services resolves to
    /// <see cref="DependencyProperty.UnsetValue"/>.
    /// </remarks>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        Type viewType = ResolveViewType(ViewModelType);
        IServiceProvider? provider = _serviceProvider;
        return provider is null
            ? CreateDesignTimeView(viewType, ViewModelType)
            : provider.GetRequiredService(viewType);
    }

    private static object CreateDesignTimeView(Type viewType, Type viewModelType)
    {
        try
        {
            if (ResolveDesignDataType(viewModelType) is not Type designDataType
                || Activator.CreateInstance(viewType) is not FrameworkElement view
                || Activator.CreateInstance(designDataType) is not object designData)
                return DependencyProperty.UnsetValue;

            view.DataContext = designData;
            return view;
        }
        catch (Exception)
        {
            // The designer must survive a sub-view that cannot be composed without the application services.
            return DependencyProperty.UnsetValue;
        }
    }

    private static Type? ResolveDesignDataType(Type viewModelType)
    {
        const string viewModelSuffix = "ViewModel";
        string? viewModelNamespace = viewModelType.Namespace;
        if (viewModelNamespace is null || viewModelNamespace.IndexOf(".ViewModels", StringComparison.Ordinal) < 0)
            return null;

        string designDataNamespace = viewModelNamespace.Replace(".ViewModels", ".DesignData");
        string viewName = viewModelType.Name.Substring(0, viewModelType.Name.Length - viewModelSuffix.Length);
        return viewModelType.Assembly.GetType($"{designDataNamespace}.{viewName}DesignData");
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
