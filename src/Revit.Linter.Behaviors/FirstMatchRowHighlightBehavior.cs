using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

using VolocyNazad.Wpf.DependencyProperties;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Highlights the first realized grid row whose diagnostic code matches the target.
/// </summary>
/// <remarks>
/// Only realized containers can carry an effect, so virtualized rows are evaluated when they load
/// and whenever the shown collection changes. An empty <see cref="TargetCode"/> disables matching
/// and clears the highlight. Row items are matched through <see cref="IHasDiagnosticCode"/>;
/// items without a code never match. The key never names an observer; it only marks the row.
/// </remarks>
[GenerateDependencyProperty<string>(
    "HighlightKey",
    DefaultValueExpression = "string.Empty",
    OnChanged = nameof(OnHighlightSetupChanged),
    PropertyXmlDocumentation = "Gets or sets the onboarding key set on the first matching realized row.")]
[GenerateDependencyProperty<string>(
    "TargetCode",
    DefaultValueExpression = "string.Empty",
    OnChanged = nameof(OnHighlightSetupChanged),
    PropertyXmlDocumentation = "Gets or sets the diagnostic code of the row to highlight.")]
public sealed partial class FirstMatchRowHighlightBehavior : Behavior<DataGrid>
{
    private INotifyCollectionChanged? _observedItems;
    private DependencyPropertyDescriptor? _itemsSourceDescriptor;

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
        AssociatedObject.LoadingRow += OnLoadingRow;
        AssociatedObject.UnloadingRow += OnUnloadingRow;
        AssociatedObject.DataContextChanged += OnDataContextChanged;

        // The items source is rebound when the view model replaces its collection view;
        // watching the property keeps the collection subscription on the shown collection.
        _itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(DataGrid));
        _itemsSourceDescriptor?.AddValueChanged(AssociatedObject, OnItemsSourceChanged);

        SubscribeToItems();
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        UnsubscribeFromItems();
        if (_itemsSourceDescriptor is not null)
        {
            _itemsSourceDescriptor.RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
            _itemsSourceDescriptor = null;
        }

        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        AssociatedObject.UnloadingRow -= OnUnloadingRow;
        AssociatedObject.LoadingRow -= OnLoadingRow;
        AssociatedObject.Unloaded -= OnUnloaded;
        AssociatedObject.Loaded -= OnLoaded;
        base.OnDetaching();
    }

    private static void OnHighlightSetupChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
    {
        if (source is FirstMatchRowHighlightBehavior behavior && behavior.AssociatedObject is not null)
            behavior.EvaluateRealizedRows();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        SubscribeToItems();
        EvaluateRealizedRows();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args) => UnsubscribeFromItems();

    private void OnLoadingRow(object? sender, DataGridRowEventArgs args) => EvaluateRealizedRows();

    private static void OnUnloadingRow(object? sender, DataGridRowEventArgs args)
        => OnboardingHighlight.SetKey(args.Row, null);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) => SubscribeToItems();

    private void OnItemsSourceChanged(object? sender, EventArgs args) => SubscribeToItems();

    private void SubscribeToItems()
    {
        if (AssociatedObject is null) return;
        INotifyCollectionChanged? items = AssociatedObject.ItemsSource as INotifyCollectionChanged;
        if (ReferenceEquals(items, _observedItems)) return;
        UnsubscribeFromItems();
        _observedItems = items;
        if (_observedItems is not null)
            _observedItems.CollectionChanged += OnItemsChanged;
    }

    private void UnsubscribeFromItems()
    {
        if (_observedItems is null) return;
        _observedItems.CollectionChanged -= OnItemsChanged;
        _observedItems = null;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (AssociatedObject is null) return;
        if (!AssociatedObject.Dispatcher.CheckAccess())
        {
            _ = AssociatedObject.Dispatcher.InvokeAsync(EvaluateRealizedRows);
            return;
        }

        EvaluateRealizedRows();
    }

    private void EvaluateRealizedRows()
    {
        if (AssociatedObject is null) return;
        string targetCode = TargetCode;
        string highlightKey = HighlightKey;
        FirstMatchRowHighlight.Refresh(
            AssociatedObject,
            item => item is IHasDiagnosticCode coded
                && !string.IsNullOrEmpty(targetCode)
                && string.Equals(coded.Code, targetCode, StringComparison.Ordinal),
            highlightKey);
    }
}
