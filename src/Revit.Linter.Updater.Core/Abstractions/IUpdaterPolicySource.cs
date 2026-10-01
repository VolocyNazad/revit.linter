using Revit.Linter.Updater.Core.Models;

namespace Revit.Linter.Updater.Core.Abstractions;

/// <summary>Reads updater policy without modifying its backing store.</summary>
public interface IUpdaterPolicySource
{
    /// <summary>Reads policy applied to every user on the machine.</summary>
    UpdaterPolicy ReadMachinePolicy();

    /// <summary>Reads policy applied to the current user.</summary>
    UpdaterPolicy ReadUserPolicy();
}
