using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Revit.Linter.ElementVisualization.Abstractions.Models;

namespace Revit.Linter.ElementVisualization.Services;

/// <summary>Owns the transactions and the view activation of one pipeline run.</summary>
/// <remarks>
/// The scope keeps the declared step order: a step that changes the document runs inside a shared
/// transaction, and a step that needs the view on screen closes the current transaction, activates
/// the view, and lets the following steps open a new transaction. Revit accepts a change of the
/// active view only while the document is not modifiable, which is why the scope, and not the step,
/// performs the activation.
/// </remarks>
internal sealed class VisualizationApplyScope : IDisposable
{
    private readonly Document _document;
    private readonly View _view;
    private readonly Action<View>? _activateView;
    private readonly string _pipelineName;
    private readonly ILogger _logger;
    private readonly TransactionGroup? _transactionGroup;
    private Transaction? _stepTransaction;
    private bool _viewActivated;
    private bool _finished;

    /// <summary>Starts the transaction group that makes the pipeline atomic.</summary>
    /// <param name="document">The document the visualization changes.</param>
    /// <param name="view">The view the steps are applied to.</param>
    /// <param name="activateView">Activates the view for the user; <see langword="null"/> in tests or when no view is shown.</param>
    /// <param name="pipelineName">The user-facing pipeline name, used for transaction names and logs.</param>
    /// <param name="logger">Writes the step-boundary diagnostics.</param>
    public VisualizationApplyScope(
        Document document,
        View view,
        Action<View>? activateView,
        string pipelineName,
        ILogger logger)
    {
        _document = document;
        _view = view;
        _activateView = activateView;
        _pipelineName = pipelineName;
        _logger = logger;
        _transactionGroup = VisualizationTransactions.StartGroup(
            document,
            ElementVisualizationLocalizations.GetString("applyVisualization_transactionGroup", pipelineName));
    }

    /// <summary>Prepares the transaction and the view state the given step runs in.</summary>
    /// <param name="step">The step that is about to execute.</param>
    public void Prepare(ElementVisualizationStep step)
    {
        if (step.RequiresActiveView && !_viewActivated)
        {
            CommitStepTransaction();
            _viewActivated = ActivateView();
        }

        if (step.RequiresTransaction)
            EnsureStepTransaction();
        else
            CommitStepTransaction();
    }

    /// <summary>Commits the last step transaction and merges the run into one undo item.</summary>
    public void Complete()
    {
        CommitStepTransaction();
        if (!_viewActivated)
            ActivateView();
        VisualizationTransactions.Assimilate(_transactionGroup);
        _finished = true;
    }

    /// <summary>Discards every change of the run, including changes of already committed steps.</summary>
    public void Rollback()
    {
        if (_finished) return;
        _finished = true;
        VisualizationTransactions.RollBack(_stepTransaction);
        _stepTransaction = null;
        VisualizationTransactions.RollBack(_transactionGroup);
    }

    /// <inheritdoc />
    public void Dispose() => Rollback();

    private void EnsureStepTransaction()
    {
        if (_stepTransaction is not null || _document.IsModifiable) return;

        Transaction started = new(
            _document,
            ElementVisualizationLocalizations.GetString("applyVisualization_transactionStep", _pipelineName));
        try
        {
            if (started.Start() != TransactionStatus.Started)
                throw new InvalidOperationException("Failed to start a visualization step transaction.");
        }
        catch
        {
            started.Dispose();
            throw;
        }

        _stepTransaction = started;
    }

    private void CommitStepTransaction()
    {
        Transaction? current = _stepTransaction;
        if (current is null) return;
        _stepTransaction = null;
        try
        {
            if (current.GetStatus() == TransactionStatus.Started
                && current.Commit() != TransactionStatus.Committed)
                throw new InvalidOperationException("Failed to commit a visualization step transaction.");
        }
        finally
        {
            current.Dispose();
        }
    }

    // A view can only be activated between two committed transactions.
    private bool ActivateView()
    {
        if (_activateView is null) return false;
        if (_document.IsModifiable)
        {
            _logger.LogDebug(
                "The visualization view {ViewName} was not activated for pipeline {VisualizationName} " +
                "because an outer transaction keeps the document modifiable",
                _view.Name, _pipelineName);
            return true;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        _activateView(_view);
        _logger.LogInformation(
            "Activated visualization view {ViewName} for pipeline {VisualizationName} in {ElapsedMilliseconds:F0} ms",
            _view.Name, _pipelineName, stopwatch.Elapsed.TotalMilliseconds);
        return true;
    }
}
