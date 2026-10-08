using System.IO;
using System.Text.Json;
using MeetingReminder.Models;

namespace MeetingReminder.Services;

/// <summary>
/// Liest und schreibt die Erinnerungs-Einstellungen (Vorlaufzeiten) als "settings.json" im selben
/// Verzeichnis wie meetings.json (siehe <see cref="DataDirectoryResolver"/>).
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsStore()
    {
        FilePath = Path.Combine(DataDirectoryResolver.Resolve(), "settings.json");
    }

    public string FilePath { get; }

    public ReminderSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            var defaults = new ReminderSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ReminderSettings>(json, JsonOptions) ?? new ReminderSettings();
        }
        catch (JsonException)
        {
            return new ReminderSettings();
        }
    }

    public void Save(ReminderSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
