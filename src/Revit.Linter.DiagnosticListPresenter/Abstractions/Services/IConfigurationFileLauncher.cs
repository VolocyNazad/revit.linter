namespace Revit.Linter.DiagnosticListPresenter.Abstractions.Services;

/// <summary>Opens diagnostic configuration files for the user.</summary>
/// <remarks>
/// The methods return after the request is handed to the shell. A failure to start the requested
/// application is logged and reported to the user by the implementation; it is never thrown to the caller.
/// </remarks>
public interface IConfigurationFileLauncher
{
    /// <summary>Opens the file with the application associated with its type.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns><see langword="true"/> when the file was handed to the shell; otherwise <see langword="false"/>.</returns>
    bool Open(string path);

    /// <summary>Shows the Windows "Open with" dialog so the user can choose an application.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns>
    /// <see langword="true"/> when the user chose an application; <see langword="false"/> when the dialog
    /// was cancelled or could not be shown.
    /// </returns>
    bool OpenWith(string path);

    /// <summary>Opens the file in Notepad.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns><see langword="true"/> when Notepad was started; otherwise <see langword="false"/>.</returns>
    bool OpenInNotepad(string path);

    /// <summary>Shows the file selected in File Explorer.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns><see langword="true"/> when Explorer was started; otherwise <see langword="false"/>.</returns>
    bool RevealInExplorer(string path);
}
