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

    private readonly BindingList<MeetingRow> _rows;
    private readonly DataGridView _grid;

    public EditMeetingsForm(IEnumerable<Meeting> meetings)
    {
        Text = "Termine bearbeiten";
        Width = 640;
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
            Width = 130,
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
            Text = "Wochentag, Uhrzeit (z.B. 09:00) und Titel eintragen. Leere Zeile am Ende = neuer Termin.",
            Dock = DockStyle.Top,
            Height = 30,
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
                MessageBox.Show(
                    this,
                    $"Ungültige Uhrzeit '{row.Time}' bei Termin '{row.Title}'. Bitte Format HH:mm verwenden (z.B. 09:00).",
                    "Meeting Reminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            result.Add(new Meeting
            {
                Day = row.Day,
                Hour = time.Hours,
                Minute = time.Minutes,
                Title = row.Title.Trim(),
            });
        }

        SavedMeetings = result;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static bool TryParseTime(string text, out TimeSpan time)
    {
        text = text.Trim();
        return TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out time)
            || (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time)
                && time >= TimeSpan.Zero
                && time < TimeSpan.FromDays(1));
    }

    private sealed record DayOption(DayOfWeek Day, string Label);

    private sealed class MeetingRow
    {
        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;

        public string Time { get; set; } = "09:00";

        public string Title { get; set; } = string.Empty;

        public static MeetingRow FromMeeting(Meeting meeting) => new()
        {
            Day = meeting.Day,
            Time = $"{meeting.Hour:D2}:{meeting.Minute:D2}",
            Title = meeting.Title,
        };
    }
}
