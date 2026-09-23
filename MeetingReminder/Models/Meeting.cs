using System.Text.Json.Serialization;

namespace MeetingReminder.Models;

public sealed class Meeting
{
    public DayOfWeek Day { get; set; }

    public int Hour { get; set; }

    public int Minute { get; set; }

    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public TimeSpan TimeOfDay => new(Hour, Minute, 0);

    public override string ToString() => $"{Day}, {Hour:D2}:{Minute:D2} Uhr - {Title}";
}
