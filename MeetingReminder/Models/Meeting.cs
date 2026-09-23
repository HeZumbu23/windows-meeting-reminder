using System.Text.Json.Serialization;

namespace MeetingReminder.Models;

public sealed class Meeting
{
    public DayOfWeek Day { get; set; }

    public int Hour { get; set; }

    public int Minute { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>1 = jede Woche (Standard), 2 = alle zwei Wochen ausgehend von <see cref="StartDate"/>.</summary>
    public int IntervalWeeks { get; set; } = 1;

    /// <summary>Erster Termin bei zwei-wöchentlichem Rhythmus. Bei wöchentlichen Terminen ungenutzt.</summary>
    public DateOnly? StartDate { get; set; }

    [JsonIgnore]
    public TimeSpan TimeOfDay => new(Hour, Minute, 0);

    public override string ToString() => IntervalWeeks > 1
        ? $"{Day}, {Hour:D2}:{Minute:D2} Uhr - {Title} (alle {IntervalWeeks} Wochen ab {StartDate:dd.MM.yyyy})"
        : $"{Day}, {Hour:D2}:{Minute:D2} Uhr - {Title}";
}
