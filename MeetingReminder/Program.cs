using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MeetingReminder;

internal static class Program
{
    private const string SingleInstanceMutexName = "Local\\MeetingReminder_SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Meeting Reminder läuft bereits im Task-Tray (unten rechts neben der Uhr).",
                "Meeting Reminder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception);

        Application.Run(new TrayApplicationContext());
    }

    private static void LogException(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MeetingReminder");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "error.log"),
                $"{DateTime.Now:O} {exception}{Environment.NewLine}");
        }
        catch
        {
            // Logging darf die Anwendung nicht zusätzlich zum Absturz bringen.
        }
    }
}
