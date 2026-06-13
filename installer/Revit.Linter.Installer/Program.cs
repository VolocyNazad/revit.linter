using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

namespace Revit.Linter.Installer;

public class Program
{
    private const string AddinName = "Revit.Linter";
    private const string Configuration = "Debug 2021.1.9";
    private const string Platform = "net48";
    private const string Vendor = "VolocyNazad";
    static void Main()
    {
        string solutionPath = TryGetSolutionDirectoryPath();

        Version version = new(1, 0, 0);
        string productGuid = GenerateProductGuid(AddinName, version);

        Project project = new() {
            UpgradeCode = new Guid("a666ef07-a7be-5555-34af-2fdaaad3fcea"),
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
            MajorUpgrade = MajorUpgrade.Default,
            //BackgroundImage = ,
            //BannerImage = ,
            Dirs = [
                new InstallDir($@"%AppDataFolder%\{AddinName}",
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
                new ManagedAction(CustomActions.CloseProcesses,
                    Return.check,
                    When.Before,
                    Step.LaunchConditions,
                    Condition.Installed)
            ],
        };
        project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);
        project.BuildMsi();
    }

    private static string TryGetSolutionDirectoryPath(string? currentPath = null)
    {
        DirectoryInfo? directory = new(currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && directory.GetFiles("*.slnx").Length == 0)
            directory = directory.Parent;
        return directory!.FullName;
    }
    private static string GenerateProductGuid(string productName, Version version)
    {
        string input = $"{productName}-{version.Major}.{version.Minor}.{version.Build}";
        byte[] hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash).ToString();
    }
}