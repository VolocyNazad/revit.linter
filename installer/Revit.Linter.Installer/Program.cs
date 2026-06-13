using System.Diagnostics;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using WixToolset.Dtf.WindowsInstaller;

namespace Revit.Linter.Installer;

public class Program
{   
    static void Main()
    {
        string solutionPath = TryGetSolutionDirectoryPath();

        Version version = new(1, 0, 0);
        string productGuid = GenerateProductGuid("Revit.Linter", version);

        Project project = new() {
            UpgradeCode = new Guid("a666ef07-a7be-5555-34af-2fdaaad3fcea"),
            GUID = new Guid(productGuid),
            Version = version,
            Name = "Revit.Linter",
            ControlPanelInfo = {
                Name = "Revit.Linter",
                Manufacturer = "Revit.Linter",
                Comments = "Revit linter installer.",
                //ProductIcon = @"Resources\Icon.ico",
                //Contact = "",      
                //HelpLink = "",
            },
            Platform = Platform.x64,
            UI = WUI.WixUI_InstallDir,
            Scope = InstallScope.perUser,
            MajorUpgrade = MajorUpgrade.Default,
            //BackgroundImage = Path.Combine(@"\Resources\BackgroundImage.png"),
            //BannerImage = Path.Combine(@"\Resources\BannerImage.png"),
            Dirs = [
                new InstallDir(@"%AppDataFolder%\Revit.Linter",
                    new Dir("sources", new Files(
                        Path.Combine(
                            solutionPath,
                            @"src\Revit.Linter\bin\x64\Debug 2021.1.4\net48", // todo заменить на релиз, и реадизовать мультиверсионность
                            "*.*")
                        ))
                ),
                new Dir(
                    @"%Desktop%",
                    new ExeFileShortcut(
                        "Revit.Linter",
                        @"[INSTALLDIR]Revit.Linter\Revit.Linter.exe", arguments: "") { WorkingDirectory = "[INSTALLDIR]" }
                ),
                new Dir(
                    @"%ProgramMenu%",
                    new ExeFileShortcut(
                        "Revit.Linter",
                        @"[INSTALLDIR]Revit.Linter\Revit.Linter.exe", arguments: "") { WorkingDirectory = "[INSTALLDIR]" }
                ),
            ],
            RegValues = [
            ],
            Actions = [
                new ManagedAction(CustomActions.LaunchInstaller,
                     Return.ignore,
                     When.After,
                     Step.InstallFinalize,
                     Condition.NOT_Installed),
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
        while (directory != null && directory.GetFiles("*.sln").Length == 0)
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

public static class CustomActions
{
    [CustomAction]
    public static ActionResult LaunchInstaller(Session session)
    {
        try {
            Process.Start(new ProcessStartInfo {
                FileName = Path.Combine(session["INSTALLDIR"], "Revit.Linter", "Revit.Linter.exe"),
                UseShellExecute = true
            });
            return ActionResult.Success;
        } catch (Exception ex) {
            session.Log($"Failed to launch installer: {ex.Message}");
            return ActionResult.Failure;
        }
    }

    [CustomAction]
    public static ActionResult CloseProcesses(Session session)
    {
        try {
            foreach (var process in Process.GetProcessesByName("Revit.Linter"))
                process.Kill();
            return ActionResult.Success;
        } catch {
            return ActionResult.Success;
        }
    }
}