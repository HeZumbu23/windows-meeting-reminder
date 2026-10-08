namespace MeetingReminder.Models;

/// <summary>Über das Tray-Menü ("Einstellungen...") konfigurierbarer Erinnerungs-Vorlauf.</summary>
public sealed class ReminderSettings
{
    /// <summary>Vorlauf des grünen (frühen) Hinweis-Popups, in Sekunden.</summary>
    public int GreenLeadSeconds { get; set; } = 5 * 60;

    /// <summary>Vorlauf des roten (dringlichen) Popups, in Sekunden.</summary>
    public int RedLeadSeconds { get; set; } = 30;
}
