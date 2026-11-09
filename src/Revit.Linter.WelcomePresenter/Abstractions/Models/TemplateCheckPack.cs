namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>
/// Identifies a shipped check block generated from an ADSK project template.
/// </summary>
/// <remarks>
/// A block describes the project parameters of one discipline template and adds one fill check per text
/// parameter of that template; both parts are appended to the installed example configuration. Each pack
/// ships in two editions: the Revit 2021-2023 <c>PG_*</c> parameter group spelling and the ForgeTypeId
/// values of Revit 2024 and newer. The welcome window installs the edition of the running Revit version.
/// </remarks>
public enum TemplateCheckPack
{
    /// <summary>Architecture (АР).</summary>
    Architecture,

    /// <summary>Water supply and sewerage (ВК).</summary>
    WaterSupply,

    /// <summary>Reinforced concrete structures (КЖ).</summary>
    ReinforcedConcrete,

    /// <summary>Steel structures (КМ).</summary>
    SteelStructures,

    /// <summary>Heating and ventilation (ОВ).</summary>
    Hvac,

    /// <summary>Low-current systems (СС).</summary>
    LowCurrentSystems,

    /// <summary>Technology (ТХ).</summary>
    Technology,

    /// <summary>Electrical systems (ЭОМ).</summary>
    Electrical,
}
