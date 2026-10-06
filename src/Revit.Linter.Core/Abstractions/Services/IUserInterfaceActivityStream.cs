using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Publishes completed UI workflow facts to optional, passive observers.</summary>
/// <remarks>Publishing an activity must not change the result or control flow of the originating feature.</remarks>
public interface IUserInterfaceActivityStream
{
    /// <summary>Occurs after a user-facing workflow activity completes.</summary>
    event EventHandler<UserInterfaceActivity>? ActivityPublished;

    /// <summary>Publishes a completed activity.</summary>
    void Publish(UserInterfaceActivity activity);
}
