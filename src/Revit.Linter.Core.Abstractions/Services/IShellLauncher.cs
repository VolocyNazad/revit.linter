namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Hands files, folders and addresses to the Windows shell, which opens them with their registered application.
/// </summary>
/// <remarks>
/// A method returns after the request is handed to the shell and never throws. A failed launch is logged and
/// reported to the user by the implementation, so callers only use the result to decide whether a follow-up
/// workflow fact applies.
/// </remarks>
public interface IShellLauncher
{
    /// <summary>Opens the file with the application associated with its type.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the file was handed to the shell; otherwise <see langword="false"/>.</returns>
    bool Open(string path);

    /// <summary>Opens the folder in File Explorer.</summary>
    /// <param name="path">The folder path.</param>
    /// <returns><see langword="true"/> when the folder was handed to the shell; otherwise <see langword="false"/>.</returns>
    bool OpenFolder(string path);

    /// <summary>Shows the Windows "Open with" dialog so the user can choose an application for the file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>
    /// <see langword="true"/> when the user chose an application; <see langword="false"/> when the dialog was
    /// cancelled, could not be shown or the launch failed.
    /// </returns>
    bool OpenWith(string path);

    /// <summary>Opens the file in Notepad.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when Notepad was started; otherwise <see langword="false"/>.</returns>
    bool OpenInNotepad(string path);

    /// <summary>Shows the file selected in File Explorer.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when Explorer was started; otherwise <see langword="false"/>.</returns>
    bool RevealInExplorer(string path);

    /// <summary>Opens the address in the default browser.</summary>
    /// <param name="url">The absolute address to open.</param>
    /// <returns><see langword="true"/> when the address was handed to the shell; otherwise <see langword="false"/>.</returns>
    bool OpenUrl(string url);
}
