using Revit.Linter.WelcomePresenter.Abstractions.Models;
using System.Text;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Appends the shipped template check blocks to a composed example configuration.
/// </summary>
internal static class TemplateCheckComposer
{
    /// <summary>Gets the resource file name of a check block, for example <c>adsk-401.yaml</c>.</summary>
    /// <remarks>
    /// Both editions of a pack share this file name; the installer picks the resource set from the running
    /// Revit version, so the name itself does not identify the group spelling.
    /// </remarks>
    public static string GetFileName(TemplateCheckPack pack) => $"adsk-{GetCodeNumber(pack)}.yaml";

    /// <summary>Gets the rule code number of a check block, for example <c>401</c>.</summary>
    public static int GetCodeNumber(TemplateCheckPack pack) => pack switch
    {
        TemplateCheckPack.Architecture => 401,
        TemplateCheckPack.WaterSupply => 402,
        TemplateCheckPack.ReinforcedConcrete => 403,
        TemplateCheckPack.SteelStructures => 404,
        TemplateCheckPack.Hvac => 405,
        TemplateCheckPack.LowCurrentSystems => 406,
        TemplateCheckPack.Technology => 407,
        TemplateCheckPack.Electrical => 408,
        _ => throw new ArgumentOutOfRangeException(nameof(pack), pack, "Unknown template check pack."),
    };

    /// <summary>
    /// Appends the selected check blocks to a configuration file text.
    /// </summary>
    /// <param name="configuration">The composed example configuration text.</param>
    /// <param name="packs">The selected check packs, in the order they are appended.</param>
    /// <param name="readBlock">Returns the YAML rule block of one check pack.</param>
    /// <returns>The configuration with every block appended and exactly one final line break.</returns>
    /// <remarks>
    /// A block is a YAML sequence of list items - the project-parameter rule followed by one custom fill
    /// check per text parameter, with their comments - so appending it after the last rule keeps it inside
    /// the <c>rules</c> sequence. Line endings are normalized to the platform's.
    /// </remarks>
    public static string Append(
        string configuration, IReadOnlyCollection<TemplateCheckPack> packs, Func<TemplateCheckPack, string> readBlock)
    {
        StringBuilder result = new(configuration.Replace("\r\n", "\n"));

        foreach (TemplateCheckPack pack in packs.Distinct())
        {
            string block = readBlock(pack).Replace("\r\n", "\n").TrimEnd('\n');
            if (string.IsNullOrWhiteSpace(block))
                throw new InvalidOperationException($"The template check block '{GetFileName(pack)}' is empty.");

            string trimmed = result.ToString().TrimEnd('\n');
            result.Clear();
            result.Append(trimmed).Append('\n').Append('\n');
            result.Append(block).Append('\n');
        }

        string text = result.ToString().TrimEnd('\n');
        return text.Replace("\n", Environment.NewLine) + Environment.NewLine;
    }
}
