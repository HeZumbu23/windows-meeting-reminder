using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace MeetingReminder.Services;

/// <summary>
/// Merkt sich pro Tag, welche Erinnerungen schon gezeigt wurden, damit ein Neustart der App
/// (Autostart, Absturz, Neu-Deploy) bereits weggeklickte Erinnerungen nicht innerhalb der
/// Gnadenfrist erneut zeigt. Liegt bewusst unter %LocalAppData% statt im Repository: reiner
/// Laufzeitzustand, nichts zum Einchecken.
/// </summary>
public sealed class FiredReminderStore
{
    private readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeetingReminder",
        "fired-reminders.json");

    public HashSet<string> Load(DateOnly today)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var state = JsonSerializer.Deserialize<State>(File.ReadAllText(_filePath));
                if (state is not null && state.Date == FormatDate(today))
                {
                    return new HashSet<string>(state.Keys);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unlesbarer Zustand: lieber einmal zu oft erinnern als eine Erinnerung zu verpassen.
        }

        return new HashSet<string>();
    }

    public void Save(DateOnly today, IEnumerable<string> keys)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var state = new State { Date = FormatDate(today), Keys = new List<string>(keys) };
            File.WriteAllText(_filePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nicht speicherbar: schlimmstenfalls erscheint nach einem Neustart eine Erinnerung erneut.
        }
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private sealed class State
    {
        public string Date { get; set; } = string.Empty;

        public List<string> Keys { get; set; } = new();
    }
}
