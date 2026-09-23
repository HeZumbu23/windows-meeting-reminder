using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingReminder.Models;

namespace MeetingReminder.Services;

/// <summary>
/// Liest und schreibt die Terminliste als JSON-Datei unter %AppData%\MeetingReminder.
/// Die Datei kann bei Bedarf auch von Hand editiert werden (Tray-Menü "Termindatei anzeigen").
/// </summary>
public sealed class MeetingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public MeetingStore()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingReminder");
        Directory.CreateDirectory(dataDirectory);
        FilePath = Path.Combine(dataDirectory, "meetings.json");
    }

    public string FilePath { get; }

    public List<Meeting> Load()
    {
        if (!File.Exists(FilePath))
        {
            var defaults = CreateDefaultMeetings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var meetings = JsonSerializer.Deserialize<List<Meeting>>(json, JsonOptions);
            return meetings ?? CreateDefaultMeetings();
        }
        catch (JsonException)
        {
            // Beschädigte Datei nicht überschreiben - Nutzer bekommt einfach die Beispieltermine,
            // die manuell bearbeitete Datei bleibt zur Fehlersuche erhalten.
            return CreateDefaultMeetings();
        }
    }

    public void Save(List<Meeting> meetings)
    {
        var json = JsonSerializer.Serialize(meetings, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    private static List<Meeting> CreateDefaultMeetings() =>
    [
        new Meeting { Day = DayOfWeek.Monday, Hour = 9, Minute = 0, Title = "Daily Standup" },
        new Meeting { Day = DayOfWeek.Tuesday, Hour = 9, Minute = 0, Title = "Daily Standup" },
        new Meeting { Day = DayOfWeek.Wednesday, Hour = 9, Minute = 0, Title = "Daily Standup" },
        new Meeting { Day = DayOfWeek.Wednesday, Hour = 15, Minute = 0, Title = "Sprint Review" },
        new Meeting { Day = DayOfWeek.Thursday, Hour = 9, Minute = 0, Title = "Daily Standup" },
        new Meeting { Day = DayOfWeek.Friday, Hour = 9, Minute = 0, Title = "Daily Standup" },
        new Meeting
        {
            Day = DayOfWeek.Thursday,
            Hour = 14,
            Minute = 0,
            Title = "Retro (Beispiel: alle 2 Wochen)",
            IntervalWeeks = 2,
            StartDate = new DateOnly(2026, 10, 1),
        },
    ];
}
