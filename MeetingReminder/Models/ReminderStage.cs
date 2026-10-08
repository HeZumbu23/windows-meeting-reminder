namespace MeetingReminder.Models;

/// <summary>Welche der beiden Erinnerungsstufen ein Popup auslöst.</summary>
public enum ReminderStage
{
    /// <summary>Früher, unaufdringlicherer Hinweis (grün), Standard 5 Minuten vorher.</summary>
    Green,

    /// <summary>Später, dringlicher Hinweis (rot), Standard 30 Sekunden vorher.</summary>
    Red,
}
