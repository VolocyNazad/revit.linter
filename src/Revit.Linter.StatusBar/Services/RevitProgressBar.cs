using Revit.Linter.StatusBar.Infrastructure.Utils;
using Revit.Linter.StatusBar.Views;
using System.Diagnostics;

namespace Revit.Linter.StatusBar.Services;

/// <summary>
/// RevitProgressBar
/// </summary>
/// <remarks>
/// RevitProgressBar
/// </remarks>
/// <param name="hasCancelButton"></param>
public sealed class RevitProgressBar(bool hasCancelButton = false) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly ProgressBarStackPanel _progressBarStackPanel = new(hasCancelButton);

    /// <summary>
    /// Run
    /// </summary>
    /// <param name="currentOperation"></param>
    /// <param name="count"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public RevitProgressBar Run(string currentOperation, int count, Action<int> action)
    {
        return SetCurrentOperation(currentOperation).Run(count, action);
    }
    /// <summary>
    /// Run
    /// </summary>
    /// <param name="count"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public RevitProgressBar Run(int count, Action<int> action)
    {
        return Run(Enumerable.Range(0, count), action);
    }
    /// <summary>
    /// Run
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="currentOperation"></param>
    /// <param name="collection"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public RevitProgressBar Run<T>(string currentOperation, IEnumerable<T> collection, Action<T> action)
    {
        return SetCurrentOperation(currentOperation).Run(collection, action);
    }

    /// <summary>
    /// Run
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public RevitProgressBar Run<T>(IEnumerable<T> collection, Action<T> action)
    {
        _progressBarStackPanel.Data.CurrentValue = 0;
        _progressBarStackPanel.Data.MinimumValue = 0;
        _progressBarStackPanel.Data.MaximumValue = collection.Count();
        foreach (var item in collection)
        {
            var current = _progressBarStackPanel.Data.CurrentValue;
            action?.Invoke(item);
            _progressBarStackPanel.Data.CurrentValue = current + 1;
            if (RefreshStopwatchBackground().IsCancelling())
                break;
        }
        return this;
    }

    /// <summary>
    /// SetCurrentOperation
    /// </summary>
    /// <param name="currentOperation"></param>
    /// <returns></returns>
    public RevitProgressBar SetCurrentOperation(string currentOperation)
    {
        _progressBarStackPanel.Data.CurrentOperation = currentOperation;
        return this;
    }

    /// <summary>
    /// SetCurrentValue
    /// </summary>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    public RevitProgressBar SetCurrentValue(double currentValue)
    {
        _progressBarStackPanel.Data.CurrentValue = currentValue;
        return this;
    }

    /// <summary>
    /// SetMinimumValue
    /// </summary>
    /// <param name="minimumValue"></param>
    /// <returns></returns>
    public RevitProgressBar SetMinimumValue(double minimumValue)
    {
        _progressBarStackPanel.Data.MinimumValue = minimumValue;
        return this;
    }

    /// <summary>
    /// SetMaximumValue
    /// </summary>
    /// <param name="maximumValue"></param>
    /// <returns></returns>
    public RevitProgressBar SetMaximumValue(double maximumValue)
    {
        _progressBarStackPanel.Data.MaximumValue = maximumValue;
        return this;
    }

    /// <summary>
    /// SetIsIndeterminate
    /// </summary>
    /// <param name="isIndeterminate"></param>
    /// <returns></returns>
    public RevitProgressBar SetIsIndeterminate(bool isIndeterminate)
    {
        _progressBarStackPanel.Data.IsIndeterminate = isIndeterminate;
        return this;
    }

    /// <summary>
    /// SetHasCancelButton
    /// </summary>
    /// <param name="hasCancelButton"></param>
    /// <returns></returns>
    public RevitProgressBar SetHasCancelButton(bool hasCancelButton)
    {
        _progressBarStackPanel.Data.HasCancelButton = hasCancelButton;
        return this;
    }

    /// <summary>
    /// Increment
    /// </summary>
    /// <param name="incrementCurrentValue"></param>
    /// <returns></returns>
    public RevitProgressBar Increment(int incrementCurrentValue = 1)
    {
        _progressBarStackPanel.Data.CurrentValue += incrementCurrentValue;
        RefreshStopwatchBackground();
        return this;
    }

    private bool CancelPressed { get; set; } = false;
    /// <summary>
    /// IsCancelling
    /// </summary>
    /// <returns></returns>
    public bool IsCancelling()
    {
        if (_progressBarStackPanel.Data.CommandCancel is null)
        {
            _progressBarStackPanel.Data.CommandCancel = new RelayCommand(Cancel);
        }
        return CancelPressed;
    }
    /// <summary>
    /// Cancel
    /// </summary>
    public void Cancel()
    {
        CancelPressed = true;
    }

    private readonly bool _forceRefresh = StatusBarController.IsVisible;

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        StatusBarController.Hide();
        _stopwatch.Stop();

        RefreshBackground(_forceRefresh);
    }

    /// <summary>
    /// Refresh Milliseconds (default: 50)
    /// </summary>
    public int RefreshMilliseconds { get; set; } = 50;
    /// <summary>
    /// Initialize Milliseconds (default: 250)
    /// </summary>
    public int InitializeMilliseconds { get; set; } = 250;

    private RevitProgressBar RefreshStopwatchBackground()
    {
        if (InitializeMilliseconds > 0 && _stopwatch.ElapsedMilliseconds < InitializeMilliseconds)
        {
            return this;
        }
        InitializeMilliseconds = 0;
        if (_stopwatch.ElapsedMilliseconds > RefreshMilliseconds)
        {
            StatusBarController.Show(_progressBarStackPanel);
            RefreshBackground();
            _stopwatch.Restart();
        }
        return this;
    }

    private static void RefreshBackground(bool disable = false)
    {
        //if (!disable)
        //    RevitRibbonController.Disable();

        ApplicationUtils.DoEvents();
        //ApplicationUtils.SetCursorWait();
        //_progressBarStackPanel.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        if (disable)
        {
            ApplicationUtils.SetCursorDefault();
            RevitRibbonController.Enable();
        }
        else
        {
            ApplicationUtils.SetCursorWait();
            RevitRibbonController.Disable();
        }
    }
}
