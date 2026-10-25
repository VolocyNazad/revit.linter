using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Revit.Linter.Infrastructure.Utils;

/// <summary>Shows the Windows "Open with" dialog for a file.</summary>
/// <remarks>
/// The dialog belongs to the shell, so it lists the applications Windows itself offers for the file type
/// and opens the file with the chosen one. The <c>OAIF_EXEC</c> flag is required on Windows 10 and later:
/// without it the shell shows a message that points the user to the default-apps settings instead of
/// offering a choice, and the registration flags have been ignored by the shell since Windows 10.
/// </remarks>
internal static class OpenWithDialog
{
    private const int Cancelled = unchecked((int)0x800704C7);

    /// <summary>Shows the dialog over the given owner window.</summary>
    /// <param name="owner">The window handle the dialog belongs to, or <see cref="IntPtr.Zero"/>.</param>
    /// <param name="path">The file to open.</param>
    /// <returns>
    /// <see langword="true"/> when the user chose an application; <see langword="false"/> when the dialog
    /// was cancelled.
    /// </returns>
    /// <exception cref="Win32Exception">The shell could not show the dialog.</exception>
    public static bool Show(IntPtr owner, string path)
    {
        var info = new OpenAsInfo
        {
            FileName = path,
            Flags = OpenAsInfoOption.Exec,
        };
        int result = SHOpenWithDialog(owner, ref info);
        if (result == 0) return true;
        if (result == Cancelled) return false;
        throw Marshal.GetExceptionForHR(result) ?? new Win32Exception(result);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHOpenWithDialog(IntPtr owner, ref OpenAsInfo info);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenAsInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string FileName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? ClassName;

        public OpenAsInfoOption Flags;
    }

    [Flags]
    private enum OpenAsInfoOption : uint
    {
        Exec = 0x4,
    }
}
