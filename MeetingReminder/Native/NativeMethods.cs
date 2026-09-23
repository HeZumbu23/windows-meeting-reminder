using System.Runtime.InteropServices;

namespace MeetingReminder.Native;

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool DestroyIcon(IntPtr handle);
}
