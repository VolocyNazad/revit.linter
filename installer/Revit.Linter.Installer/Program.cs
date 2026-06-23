using Revit.Linter.Installer;
using System.Security.Cryptography;
using System.Text;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

const int RevitVersion = 2025;
const string AddinName = "Revit.Linter";
const string Configuration = "Release 2025.0.0";
const string Platform = "net8.0-windows";
const string Vendor = "VolocyNazad";

Dictionary<int, string> guidMap = new() {
    {2021, "3e2b063d-e79e-4dd0-bdfc-1023eedecda3"},
    {2022, "121fe212-97d5-4d4c-acfa-74b46abda0e4"},
    {2023, "487c9122-7d4c-46d5-846f-45f5d45b6cb3"},
    {2024, "31c75b42-b188-48ae-8efc-44f52db48e52"},
    {2025, "df23ae86-4887-4bed-8cd3-fe1a4208480b"},
    {2026, "fb1e02f6-398a-4192-a384-c73bba90edc9"},
    {2027, "a44e9577-2101-4f23-9f97-fffe493c7b13"},
};

string solutionPath = TryGetSolutionDirectoryPath();

Version version = new(1, 0, 0);
string productGuid = GenerateProductGuid(AddinName, RevitVersion, version);

Project project = new() {
    MajorUpgrade = MajorUpgrade.Default,
    UpgradeCode = new Guid(guidMap[RevitVersion]),
    GUID = new Guid(productGuid),
    Version = version,
    Name = AddinName,
    ControlPanelInfo = {
        Name = AddinName,
        Manufacturer = Vendor,
        Comments = "Revit linter installer.",
        //ProductIcon = ,     
        HelpLink = "https://github.com/VolocyNazad/revit.linter",
    },
    Platform = WixSharp.Platform.x64,
    UI = WUI.WixUI_InstallDir,
    Scope = InstallScope.perUser,
    //BackgroundImage = ,
    //BannerImage = ,
    Dirs = [
        new InstallDir($@"%AppDataFolder%\{AddinName}\{RevitVersion}",
            new Dir("sources", new Files(
                Path.Combine(
                    solutionPath,
                    $@"src\{AddinName}\bin\x64\{Configuration}\{Platform}",
                    "*.*")
                ))
        ),
    ],
    RegValues = [
    ],
    Actions = [
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


static string TryGetSolutionDirectoryPath(string? currentPath = null)
{
    DirectoryInfo? directory = new(currentPath ?? Directory.GetCurrentDirectory());
    while (directory != null && directory.GetFiles("Revit.Linter.slnx").Length == 0)
        directory = directory.Parent;
    return directory!.FullName;
}
static string GenerateProductGuid(string productName, int revitVersion, Version version)
{
    string input = $"{productName}-{revitVersion}-{version.Major}.{version.Minor}.{version.Build}";
    byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
    return new Guid(hash).ToString();
}
