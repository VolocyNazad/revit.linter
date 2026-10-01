using Revit.Linter.Installer;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using WixSharp;

const string AddInName = "Revit.Linter";
const string Vendor = "VolocyNazad";
const string UpgradeCode = "ed6109bc-3ea6-4fe3-a1ab-e31a7db46ac1";
string[] legacyUpgradeCodes =
[
    "3e2b063d-e79e-4dd0-bdfc-1023eedecda3",
    "121fe212-97d5-4d4c-acfa-74b46abda0e4",
    "487c9122-7d4c-46d5-846f-45f5d45b6cb3",
    "31c75b42-b188-48ae-8efc-44f52db48e52",
    "df23ae86-4887-4bed-8cd3-fe1a4208480b",
    "fb1e02f6-398a-4192-a384-c73bba90edc9",
    "a44e9577-2101-4f23-9f97-fffe493c7b13"
];

if (args.Length < 4)
{
    Console.Error.WriteLine(
        "Usage: Revit.Linter.Installer <product-version> <updater-directory> <output-directory> <revit-version=source-directory> [...]");
    return 1;
}

if (!Version.TryParse(args[0], out Version? version))
{
    Console.Error.WriteLine($"Invalid product version: '{args[0]}'. Expected format: major.minor.patch.");
    return 1;
}

string updaterDirectory = Path.GetFullPath(args[1]);
string outputDirectory = Path.GetFullPath(args[2]);
if (!Directory.Exists(updaterDirectory) ||
    !System.IO.File.Exists(Path.Combine(updaterDirectory, "Revit.Linter.Updater.exe")))
{
    Console.Error.WriteLine($"Published updater directory is invalid: '{updaterDirectory}'.");
    return 1;
}

var targets = new List<RevitTarget>();
foreach (string value in args.Skip(3))
{
    int separator = value.IndexOf('=');
    if (separator <= 0 ||
        !int.TryParse(value[..separator], out int revitVersion) ||
        !Directory.Exists(value[(separator + 1)..]))
    {
        Console.Error.WriteLine($"Invalid Revit target: '{value}'. Expected revit-version=source-directory.");
        return 1;
    }

    targets.Add(new RevitTarget(revitVersion, Path.GetFullPath(value[(separator + 1)..])));
}

if (targets.Select(target => target.Version).Distinct().Count() != targets.Count)
{
    Console.Error.WriteLine("Each Revit version must be specified exactly once.");
    return 1;
}

Directory.CreateDirectory(outputDirectory);
string revitVersions = string.Join(';', targets.OrderBy(target => target.Version).Select(target => target.Version));
var coreFeature = new Feature(
    "Core",
    "Shared updater and application infrastructure.",
    isEnabled: true,
    allowChange: false)
{
    Id = new Id("Core")
};
Dictionary<int, Feature> revitFeatures = targets.ToDictionary(
    target => target.Version,
    target => new Feature(
        $"Revit {target.Version}",
        $"Revit Linter add-in for Autodesk Revit {target.Version}.",
        isEnabled: true,
        allowChange: true)
    {
        Id = new Id($"Revit{target.Version}")
    });

var installEntities = new List<WixEntity>
{
    new Files(coreFeature, Path.Combine(updaterDirectory, "*.*")),
    new Dir(
        coreFeature,
        "Addins",
        targets
            .OrderBy(target => target.Version)
            .Select(target => new Dir(
                revitFeatures[target.Version],
                target.Version.ToString(),
                new Dir(
                    revitFeatures[target.Version],
                    "sources",
                    new Files(revitFeatures[target.Version], Path.Combine(target.SourceDirectory, "*.*")))))
            .Cast<WixEntity>()
            .ToArray())
};

Project project = new()
{
    MajorUpgrade = MajorUpgrade.Default,
    UpgradeCode = new Guid(UpgradeCode),
    GUID = GenerateProductGuid(AddInName, version),
    Version = version,
    Name = AddInName,
    OutDir = outputDirectory,
    OutFileName = $"RevitLinter-{version}",
    ControlPanelInfo =
    {
        Name = AddInName,
        Manufacturer = Vendor,
        Comments = "Revit Linter per-user installer.",
        HelpLink = "https://github.com/VolocyNazad/revit.linter",
    },
    Platform = WixSharp.Platform.x64,
    UI = WUI.WixUI_FeatureTree,
    Scope = InstallScope.perUser,
    Dirs =
    [
        new InstallDir(
            $@"%LocalAppDataFolder%\Programs\{Vendor}\{AddInName}",
            installEntities.ToArray())
    ],
    Properties = [new Property("REVIT_VERSIONS", revitVersions)],
    RegValues =
    [
        new RegValue(
            coreFeature,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            AddInName,
            "\"[INSTALLDIR]Revit.Linter.Updater.exe\"")
    ],
    Actions =
    [
        new ManagedAction(
            CustomActions.PrepareForFileChanges,
            Return.check,
            When.Before,
            Step.InstallInitialize,
            Condition.Always),
        new ManagedAction(
            CustomActions.RemoveManifests,
            Return.check,
            When.Before,
            Step.RemoveFiles,
            Condition.BeingUninstalledAndNotBeingUpgraded),
        new ManagedAction(
            CustomActions.SynchronizeManifests,
            Return.check,
            When.After,
            Step.InstallFinalize,
            Condition.NOT_BeingRemoved),
    ],
};

project.WixSourceGenerated += document => AddLegacyUpgradeRows(document, legacyUpgradeCodes, version);
project.BuildMsi();
return 0;

static Guid GenerateProductGuid(string productName, Version version)
{
    string input = $"{productName}-{version.Major}.{version.Minor}.{version.Build}";
    byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
    return new Guid(hash);
}

static void AddLegacyUpgradeRows(XDocument document, string[] upgradeCodes, Version currentVersion)
{
    XElement package = document.Descendants().Single(element => element.Name.LocalName == "Package");
    XNamespace wix = package.Name.Namespace;
    string maximumVersion = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";

    for (int index = 0; index < upgradeCodes.Length; index++)
    {
        package.Add(new XElement(
            wix + "Upgrade",
            new XAttribute("Id", upgradeCodes[index]),
            new XElement(
                wix + "UpgradeVersion",
                new XAttribute("Minimum", "0.0.0"),
                new XAttribute("IncludeMinimum", "yes"),
                new XAttribute("Maximum", maximumVersion),
                new XAttribute("IncludeMaximum", "yes"),
                new XAttribute("Property", $"LEGACY_REVIT_LINTER_{index}"))));
    }
}

internal sealed record RevitTarget(int Version, string SourceDirectory);
