using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Verifies that the interaction view-model base observes the host event source instead of subscribing to
/// Revit events itself.
/// </summary>
public sealed class RevitInteractionViewModelTests
{
    [Fact]
    public async Task Initialize_SubscribesToTheHostEventSource()
    {
        var source = new FakeRevitEventSource();
        var viewModel = new RecordingInteractionViewModel(source);

        await viewModel.Initialize(TestContext.Current.CancellationToken);
        source.Raise(RevitEventType.DocumentOpened);

        Assert.Equal([RevitEventType.DocumentOpened], viewModel.ReceivedEvents);
    }

    [Fact]
    public async Task Deinitialize_StopsObservingTheHostEventSource()
    {
        var source = new FakeRevitEventSource();
        var viewModel = new RecordingInteractionViewModel(source);
        await viewModel.Initialize(TestContext.Current.CancellationToken);

        await viewModel.Deinitialize(TestContext.Current.CancellationToken);
        source.Raise(RevitEventType.DocumentChanged);

        Assert.Empty(viewModel.ReceivedEvents);
    }

    [Fact]
    public async Task EveryRevitEventType_ReachesTheViewModel()
    {
        var source = new FakeRevitEventSource();
        var viewModel = new RecordingInteractionViewModel(source);
        await viewModel.Initialize(TestContext.Current.CancellationToken);

        var expected = (RevitEventType[])Enum.GetValues(typeof(RevitEventType));
        foreach (RevitEventType revitEventType in expected)
            source.Raise(revitEventType);

        Assert.Equal(expected, viewModel.ReceivedEvents);
    }

    private sealed class RecordingInteractionViewModel(FakeRevitEventSource source)
        : RevitInteractionViewModel(source)
    {
        public List<RevitEventType> ReceivedEvents { get; } = [];

        protected override void OnRevitChanged(RevitEventType revitEventType) =>
            ReceivedEvents.Add(revitEventType);
    }

    private sealed class FakeRevitEventSource : IRevitEventSource
    {
        public event Action<RevitEventType>? Changed;

        public void Raise(RevitEventType revitEventType) => Changed?.Invoke(revitEventType);
    }
}
