using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MeetingReminder.Services;

/// <summary>
/// Schaltet den Autostart über den klassischen "Run"-Registry-Schlüssel des aktuellen Nutzers.
/// Braucht keine Admin-Rechte und ist die Windows-übliche Alternative zur Aufgabenplanung.
/// </summary>
public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MeetingReminder";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrEmpty(value);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            key.SetValue(ValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
