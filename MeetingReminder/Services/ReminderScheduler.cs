using System;
using System.Collections.Generic;
using MeetingReminder.Models;

namespace MeetingReminder.Services;

/// <summary>
/// Prüft periodisch, ob ein Termin fällig ist, und feuert dann <see cref="MeetingsDue"/>.
/// Arbeitet bewusst mit einem einfachen Polling-Timer statt akkumulierten Tick-Zählern:
/// dadurch spielt es keine Rolle, ob der Rechner zwischenzeitlich im Energiesparmodus war -
/// nach dem Aufwachen vergleicht der nächste Tick einfach wieder die aktuelle Uhrzeit.
/// </summary>
public sealed class ReminderScheduler
{
    /// <summary>
    /// Wie lange nach dem geplanten Zeitpunkt ein Termin noch als "gerade fällig" gilt.
    /// Fängt kurze Standby-Phasen um den Termin herum ab.
    /// </summary>
    private static readonly TimeSpan GraceWindow = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(20);

    private readonly Func<List<Meeting>> _meetingsProvider;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly HashSet<string> _firedToday = new();
    private readonly List<SnoozedReminder> _snoozed = new();
    private DateTime _lastCheckedDate = DateTime.MinValue;

    public ReminderScheduler(Func<List<Meeting>> meetingsProvider)
    {
        _meetingsProvider = meetingsProvider;
        _timer = new System.Windows.Forms.Timer { Interval = (int)CheckInterval.TotalMilliseconds };
        _timer.Tick += (_, _) => Check();
    }

    public event EventHandler<IReadOnlyList<Meeting>>? MeetingsDue;

    public void Start()
    {
        _lastCheckedDate = DateTime.Now.Date;
        Check();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    /// <summary>Merkt sich einen Termin zum erneuten Auslösen nach <paramref name="delay"/>.</summary>
    public void Snooze(IEnumerable<Meeting> meetings, TimeSpan delay)
    {
        var fireAt = DateTime.Now.Add(delay);
        foreach (var meeting in meetings)
        {
            _snoozed.Add(new SnoozedReminder(meeting, fireAt));
        }
    }

    private void Check()
    {
        var now = DateTime.Now;
        if (now.Date != _lastCheckedDate)
        {
            _firedToday.Clear();
            _lastCheckedDate = now.Date;
        }

        var due = new List<Meeting>();

        for (var i = _snoozed.Count - 1; i >= 0; i--)
        {
            if (_snoozed[i].FireAt <= now)
            {
                due.Add(_snoozed[i].Meeting);
                _snoozed.RemoveAt(i);
            }
        }

        foreach (var meeting in _meetingsProvider())
        {
            if (meeting.Day != now.DayOfWeek)
            {
                continue;
            }

            if (meeting.IntervalWeeks > 1 && !IsDueThisInterval(meeting, now))
            {
                continue;
            }

            var scheduledAt = now.Date + meeting.TimeOfDay;
            if (now < scheduledAt || now > scheduledAt + GraceWindow)
            {
                continue;
            }

            var key = $"{meeting.Day}|{meeting.Hour:D2}{meeting.Minute:D2}|{meeting.Title}";
            if (!_firedToday.Add(key))
            {
                continue;
            }

            due.Add(meeting);
        }

        if (due.Count > 0)
        {
            MeetingsDue?.Invoke(this, due);
        }
    }

    private static bool IsDueThisInterval(Meeting meeting, DateTime now)
    {
        if (meeting.StartDate is not { } startDate)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(now);
        if (startDate > today)
        {
            return false;
        }

        var daysSinceStart = today.DayNumber - startDate.DayNumber;
        return daysSinceStart % (7 * meeting.IntervalWeeks) == 0;
    }

    private sealed record SnoozedReminder(Meeting Meeting, DateTime FireAt);
}
