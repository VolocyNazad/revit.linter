using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Intercepts the F1 key on the current thread while a help tooltip is open.
/// </summary>
/// <remarks>
/// A dockable pane does not own the keyboard focus while the pointer merely hovers over one of its buttons, so
/// WPF key events never reach it and Revit would answer F1 with its own help. A thread keyboard hook sees the
/// key before Revit does. The hook exists only between <see cref="Install"/> and <see cref="Uninstall"/> and
/// lets every other key through.
/// </remarks>
internal static class HelpKeyHook
{
    private const int KeyboardHook = 2;
    private const int ActionCode = 0;
    private const int F1VirtualKey = 0x70;
    private const long KeyReleasedFlag = 0x80000000L;

    private static IntPtr _hook;

    // Kept in a field so the delegate handed to Windows is not collected while the hook is installed.
    private static HookProcedure? _procedure;
    private static Action? _helpRequested;

    private delegate IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Starts intercepting F1; a previously installed hook is replaced.
    /// </summary>
    /// <param name="helpRequested">
    /// Invoked once, asynchronously on the current dispatcher, for the first F1 press. Later presses are still
    /// swallowed until <see cref="Uninstall"/> so that auto-repeat neither repeats the action nor reaches Revit.
    /// </param>
    public static void Install(Action helpRequested)
    {
        Uninstall();
        _helpRequested = helpRequested;
        _procedure = OnKeyboard;
        _hook = SetWindowsHookEx(KeyboardHook, _procedure, IntPtr.Zero, GetCurrentThreadId());
    }

    /// <summary>Stops intercepting F1. Safe to call when no hook is installed.</summary>
    public static void Uninstall()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        _procedure = null;
        _helpRequested = null;
    }

    private static IntPtr OnKeyboard(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code != ActionCode || wParam.ToInt64() != F1VirtualKey)
            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

        bool pressed = (lParam.ToInt64() & KeyReleasedFlag) == 0;
        Action? helpRequested = _helpRequested;
        if (pressed && helpRequested is not null)
        {
            _helpRequested = null;
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(helpRequested);
        }

        // A non-zero result keeps the key from the window that has the focus.
        return new IntPtr(1);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProcedure procedure, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
