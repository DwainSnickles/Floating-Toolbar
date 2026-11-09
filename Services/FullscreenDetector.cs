using System.Runtime.InteropServices;
using System.Text;

namespace FloatingToolbar.Services;

/// <summary>
/// Detects whether the foreground window is a real fullscreen application (games, video,
/// presentations) rather than a maximized window or our own toolbar.
/// </summary>
public static class FullscreenDetector
{
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const int EdgeTolerance = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder name, int maximum);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    public static bool IsFullscreenInForeground(IntPtr ignoredWindow)
    {
        try
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero || window == ignoredWindow) return false;
            if (IsIconic(window)) return false;

            GetWindowThreadProcessId(window, out var processId);
            if (processId == (uint)Environment.ProcessId) return false;

            var className = new StringBuilder(64);
            GetClassName(window, className, className.Capacity);
            var name = className.ToString();
            if (name is "Shell_TrayWnd" or "Progman" or "WorkerW") return false;

            if (!GetWindowRect(window, out var windowRect)) return false;

            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero) return false;

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info)) return false;

            // A maximized window stops at the taskbar, a fullscreen one covers the whole monitor.
            return windowRect.Left <= info.Monitor.Left + EdgeTolerance &&
                   windowRect.Top <= info.Monitor.Top + EdgeTolerance &&
                   windowRect.Right >= info.Monitor.Right - EdgeTolerance &&
                   windowRect.Bottom >= info.Monitor.Bottom - EdgeTolerance;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
