using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace Revit.Linter.StatusBar.Views;

/// <summary>
/// Interaction logic for ProgressBarStackPanel.xaml
/// </summary>
/// https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/progressbar-styles-and-templates?view=netframeworkdesktop-4.8
public sealed partial class ProgressBarStackPanel
{
    /// <summary>
    /// ProgressBarData
    /// </summary>
    public ProgressBarData Data { get; } = new();
    /// <summary>
    /// ProgressBarStackPanel
    /// </summary>
    public ProgressBarStackPanel()
    {
        Initialize();
    }
    /// <summary>
    /// ProgressBarStackPanel
    /// </summary>
    /// <param name="hasCancelButton"></param>
    public ProgressBarStackPanel(bool hasCancelButton)
    {
        Data.HasCancelButton = hasCancelButton;
        Initialize();
    }
    /// <summary>
    /// ProgressBarStackPanel
    /// </summary>
    /// <param name="hasCancelButton"></param>
    /// <param name="minimumValue"></param>
    /// <param name="maximumValue"></param>
    public ProgressBarStackPanel(bool hasCancelButton, double minimumValue, double maximumValue)
    {
        Data.HasCancelButton = hasCancelButton;
        Data.CurrentValue = minimumValue;
        Data.MinimumValue = minimumValue;
        Data.MaximumValue = maximumValue;
        Initialize();
    }
    /// <summary>
    /// SetCurrentOperation
    /// </summary>
    /// <param name="currentOperation"></param>
    /// <returns></returns>
    public ProgressBarStackPanel SetCurrentOperation(string currentOperation)
    {
        Data.CurrentOperation = currentOperation;
        return this;
    }

    private void Initialize()
    {
        DataContext = Data;
        InitializeComponent();
        this.AddResourceThemes();
    }

    /// <summary>
    /// ProgressBarData
    /// </summary>
    public partial class ProgressBarData : ObservableObject
    {
        /// <summary>
        /// IsIndeterminate
        /// </summary>
        [ObservableProperty]
        public partial bool IsIndeterminate { get; set; } = false;
        /// <summary>
        /// CurrentOperation
        /// </summary>
        [ObservableProperty]
        public partial string CurrentOperation { get; set; } = "Loading";
        /// <summary>
        /// CurrentValue
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayValue))]
        public partial double CurrentValue { get; set; } = 0;
        /// <summary>
        /// MinimumValue
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayValue))]
        public partial double MinimumValue { get; set; } = 0;
        /// <summary>
        /// MaximumValue
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayValue))]
        public partial double MaximumValue { get; set; } = 100;
        /// <summary>
        /// DisplayValue
        /// </summary>
        public double DisplayValue => ((int)MaximumValue == (int)MinimumValue) ? 100 : 100.0 * (CurrentValue - MinimumValue) / (MaximumValue - MinimumValue);
        /// <summary>
        /// CommandCancel
        /// </summary>
        public ICommand? CommandCancel { get; set; }
        /// <summary>
        /// HasCancelButton
        /// </summary>
        [ObservableProperty]
        public partial bool HasCancelButton { get; set; } = false;
    }
}
