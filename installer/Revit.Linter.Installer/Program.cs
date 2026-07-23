using Revit.Linter.Installer;
using System.Security.Cryptography;
using System.Text;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

const string AddInName = "Revit.Linter";
const string Vendor = "VolocyNazad";

if (args.Length != 4 || !int.TryParse(args[0], out int revitVersion))
{
    Console.Error.WriteLine(
        "Usage: Revit.Linter.Installer <revit-version> <product-version> <source-directory> <output-directory>");
    return 1;
}

if (!Version.TryParse(args[1], out Version? version))
{
    Console.Error.WriteLine($"Invalid product version: '{args[1]}'. Expected format: major.minor.patch.");
    return 1;
}

string sourceDirectory = Path.GetFullPath(args[2]);
string outputDirectory = Path.GetFullPath(args[3]);

if (!Directory.Exists(sourceDirectory))
{
    Console.Error.WriteLine($"Source directory does not exist: '{sourceDirectory}'.");
    return 1;
}

Directory.CreateDirectory(outputDirectory);

Dictionary<int, string> guidMap = new()
{
    {2021, "3e2b063d-e79e-4dd0-bdfc-1023eedecda3"},
    {2022, "121fe212-97d5-4d4c-acfa-74b46abda0e4"},
    {2023, "487c9122-7d4c-46d5-846f-45f5d45b6cb3"},
    {2024, "31c75b42-b188-48ae-8efc-44f52db48e52"},
    {2025, "df23ae86-4887-4bed-8cd3-fe1a4208480b"},
    {2026, "fb1e02f6-398a-4192-a384-c73bba90edc9"},
    {2027, "a44e9577-2101-4f23-9f97-fffe493c7b13"},
};

if (!guidMap.TryGetValue(revitVersion, out string? upgradeCode))
{
    Console.Error.WriteLine($"Upgrade code is not configured for Revit {revitVersion}.");
    return 1;
}

string productGuid = GenerateProductGuid(AddInName, revitVersion, version);

Project project = new()
{
    MajorUpgrade = MajorUpgrade.Default,
    UpgradeCode = new Guid(upgradeCode),
    GUID = new Guid(productGuid),
    Version = version,
    Name = AddInName,
    OutDir = outputDirectory,
    OutFileName = $"RevitLinter-{version}-rvt{revitVersion}",
    ControlPanelInfo =
    {
        Name = AddInName,
        Manufacturer = Vendor,
        Comments = "Revit linter installer.",
        HelpLink = "https://github.com/VolocyNazad/revit.linter",
    },
    Platform = WixSharp.Platform.x64,
    UI = WUI.WixUI_InstallDir,
    Scope = InstallScope.perUser,
    Dirs =
    [
        new InstallDir($@"%AppDataFolder%\{AddInName}\{revitVersion}",
            new Dir("sources", new Files(Path.Combine(sourceDirectory, "*.*"))))
    ],
    Properties =
    [
        new Property("REVIT_VERSION", revitVersion.ToString())
    ],
    Actions =
    [
        new ManagedAction(CustomActions.CreateManifest,
            Return.ignore,
            When.After,
            Step.InstallFinalize,
            Condition.NOT_Installed),
        new ManagedAction(CustomActions.RemoveManifest,
            Return.check,
            When.Before,
            Step.LaunchConditions,
            Condition.Installed),
    ],
};

project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);
project.BuildMsi();
return 0;

static string GenerateProductGuid(string productName, int revitVersion, Version version)
{
    string input = $"{productName}-{revitVersion}-{version.Major}.{version.Minor}.{version.Build}";
    byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
    return new Guid(hash).ToString();
}
