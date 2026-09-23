using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Markup;

namespace Revit.Linter.Presentation;

[MarkupExtensionReturnType(typeof(FrameworkElement))]
public sealed class ViewLocatorExtension(Type viewModelType) : MarkupExtension
{
    private static IServiceProvider? _serviceProvider;

    [ConstructorArgument("viewModelType")]
    public Type ViewModelType { get; set; } = viewModelType;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        Interlocked.CompareExchange(ref _serviceProvider, serviceProvider, null);
    }

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
        if (viewModelNamespace is null || !viewModelNamespace.Contains(".ViewModels", StringComparison.Ordinal))
            throw new InvalidOperationException($"View model type '{viewModelType.FullName}' must be located in a ViewModels namespace.");

        string viewNamespace = viewModelNamespace.Replace(".ViewModels", ".Views", StringComparison.Ordinal);
        string viewName = viewModelType.Name[..^viewModelSuffix.Length] + "View";
        string viewFullName = $"{viewNamespace}.{viewName}";
        return viewModelType.Assembly.GetType(viewFullName)
            ?? throw new InvalidOperationException($"View '{viewFullName}' was not found for '{viewModelType.FullName}'.");
    }
}
