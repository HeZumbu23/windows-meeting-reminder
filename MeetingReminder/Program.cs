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

        // DPI-Awareness wird bereits über app.manifest (dpiAwareness PerMonitorV2) gesetzt;
        // ein zusätzlicher Application.SetHighDpiMode-Aufruf würde sich damit überschneiden (WFAC010).
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject as Exception);

        // Explizit installieren statt uns auf den (lazy initialisierten) Standard-Kontext zu
        // verlassen, damit das Marshalling unten garantiert funktioniert.
        var uiContext = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(uiContext);

        var context = new TrayApplicationContext();

        // Bester Versuch für Strg+C/Strg+Pause beim Start über "dotnet run" im Terminal: falls das
        // Signal den Prozess tatsächlich erreicht (nicht garantiert, da die App als reine GUI-App
        // ohne eigenes Konsolenfenster läuft), fahren wir sauber über denselben Weg herunter wie
        // über "Beenden" im Tray-Menü, statt den Prozess hart abzuschießen. Der Handler läuft auf
        // einem separaten Thread, daher Marshalling über den UI-SynchronizationContext.
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            uiContext.Post(_ => context.RequestExit(), null);
        };

        Application.Run(context);
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
