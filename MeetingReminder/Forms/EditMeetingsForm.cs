using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MeetingReminder.Models;

namespace MeetingReminder.Forms;

/// <summary>
/// Einfacher Dialog zum Anzeigen/Bearbeiten der Terminliste als Tabelle
/// (Wochentag, Uhrzeit, Titel) - Alternative zum direkten Editieren der JSON-Datei.
/// </summary>
public sealed class EditMeetingsForm : Form
{
    private static readonly DayOption[] DayOptions =
    [
        new(DayOfWeek.Monday, "Montag"),
        new(DayOfWeek.Tuesday, "Dienstag"),
        new(DayOfWeek.Wednesday, "Mittwoch"),
        new(DayOfWeek.Thursday, "Donnerstag"),
        new(DayOfWeek.Friday, "Freitag"),
        new(DayOfWeek.Saturday, "Samstag"),
        new(DayOfWeek.Sunday, "Sonntag"),
    ];

    private static readonly IntervalOption[] IntervalOptions =
    [
        new(1, "Jede Woche"),
        new(2, "Alle 2 Wochen"),
    ];

    private readonly BindingList<MeetingRow> _rows;
    private readonly DataGridView _grid;

    public EditMeetingsForm(IEnumerable<Meeting> meetings)
    {
        Text = "Termine bearbeiten";
        Width = 860;
        Height = 480;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowIcon = false;
        ShowInTaskbar = false;

        _rows = new BindingList<MeetingRow>(meetings.Select(MeetingRow.FromMeeting).ToList());

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            RowHeadersVisible = false,
            DataSource = _rows,
        };

        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(MeetingRow.Day),
            HeaderText = "Wochentag",
            Name = "Day",
            DataSource = DayOptions,
            DisplayMember = nameof(DayOption.Label),
            ValueMember = nameof(DayOption.Day),
            Width = 120,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MeetingRow.Time),
            HeaderText = "Uhrzeit (HH:mm)",
            Name = "Time",
            Width = 110,
        });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(MeetingRow.IntervalWeeks),
            HeaderText = "Rhythmus",
            Name = "IntervalWeeks",
            DataSource = IntervalOptions,
            DisplayMember = nameof(IntervalOption.Label),
            ValueMember = nameof(IntervalOption.Weeks),
            Width = 120,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MeetingRow.StartDateText),
            HeaderText = "Ab Datum (bei 2-wöch.)",
            Name = "StartDate",
            Width = 150,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MeetingRow.Title),
            HeaderText = "Titel",
            Name = "Title",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        var hintLabel = new Label
        {
            Text = "Wochentag, Uhrzeit, Rhythmus und Titel eintragen. Bei \"Alle 2 Wochen\" zusätzlich das erste " +
                   "Datum (TT.MM.JJJJ) eintragen - der Wochentag wird dann daraus übernommen. Leere Zeile am " +
                   "Ende = neuer Termin.",
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(8, 8, 8, 0),
        };

        var saveButton = new Button { Text = "Speichern", Width = 100 };
        var cancelButton = new Button { Text = "Abbrechen", Width = 100 };
        saveButton.Click += (_, _) => TrySave();
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 50,
            Padding = new Padding(8),
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);

        Controls.Add(_grid);
        Controls.Add(buttonPanel);
        Controls.Add(hintLabel);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    /// <summary>Enthält die bereinigte Terminliste, nachdem der Dialog mit "Speichern" geschlossen wurde.</summary>
    public List<Meeting>? SavedMeetings { get; private set; }

    private void TrySave()
    {
        _grid.EndEdit();

        var result = new List<Meeting>();
        foreach (var row in _rows)
        {
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                continue;
            }

            if (!TryParseTime(row.Time, out var time))
            {
                ShowError($"Ungültige Uhrzeit '{row.Time}' bei Termin '{row.Title}'. Bitte Format HH:mm verwenden (z.B. 09:00).");
                return;
            }

            var day = row.Day;
            DateOnly? startDate = null;
            var intervalWeeks = Math.Max(1, row.IntervalWeeks);

            if (intervalWeeks > 1)
            {
                if (!TryParseDate(row.StartDateText, out var parsedDate))
                {
                    ShowError(
                        $"Ungültiges Startdatum '{row.StartDateText}' bei Termin '{row.Title}'. " +
                        "Bitte Format TT.MM.JJJJ verwenden (z.B. 13.10.2026).");
                    return;
                }

                startDate = parsedDate;
                day = parsedDate.DayOfWeek;
            }

            result.Add(new Meeting
            {
                Day = day,
                Hour = time.Hours,
                Minute = time.Minutes,
                Title = row.Title.Trim(),
                IntervalWeeks = intervalWeeks,
                StartDate = startDate,
            });
        }

        SavedMeetings = result;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowError(string message) =>
        MessageBox.Show(this, message, "Meeting Reminder", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static bool TryParseTime(string text, out TimeSpan time)
    {
        text = text.Trim();
        return TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out time)
            || (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time)
                && time >= TimeSpan.Zero
                && time < TimeSpan.FromDays(1));
    }

    private static bool TryParseDate(string text, out DateOnly date)
    {
        text = text.Trim();
        return DateOnly.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private sealed record DayOption(DayOfWeek Day, string Label);

    private sealed record IntervalOption(int Weeks, string Label);

    private sealed class MeetingRow
    {
        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;

        public string Time { get; set; } = "09:00";

        public string Title { get; set; } = string.Empty;

        public int IntervalWeeks { get; set; } = 1;

        public string StartDateText { get; set; } = string.Empty;

        public static MeetingRow FromMeeting(Meeting meeting) => new()
        {
            Day = meeting.Day,
            Time = $"{meeting.Hour:D2}:{meeting.Minute:D2}",
            Title = meeting.Title,
            IntervalWeeks = meeting.IntervalWeeks,
            StartDateText = meeting.StartDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
