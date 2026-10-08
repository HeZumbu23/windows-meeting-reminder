using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MeetingReminder.Models;

namespace MeetingReminder.Forms;

/// <summary>
/// Großes, auffälliges Vollbild-Popup, das erscheint, sobald ein Termin fällig ist. Die Farbe
/// richtet sich nach der Erinnerungsstufe: grün für den frühen Hinweis, rot für den dringlichen
/// kurz vor dem Termin. Schließbar per Klick, ESC oder Enter; alternativ per "Snooze"-Button
/// 5 Minuten verschieben.
/// </summary>
public sealed class ReminderPopupForm : Form
{
    private const int AutoCloseAfterMilliseconds = 90_000;

    private static readonly Color RedBackColor = Color.FromArgb(200, 0, 0);
    private static readonly Color RedHintColor = Color.FromArgb(255, 225, 225);
    private static readonly Color GreenBackColor = Color.FromArgb(0, 140, 70);
    private static readonly Color GreenHintColor = Color.FromArgb(225, 255, 235);

    private readonly System.Windows.Forms.Timer _autoCloseTimer;

    public ReminderPopupForm(IReadOnlyList<Meeting> dueMeetings, ReminderStage stage)
    {
        if (dueMeetings.Count == 0)
        {
            throw new ArgumentException("Es muss mindestens ein Termin übergeben werden.", nameof(dueMeetings));
        }

        var backColor = stage == ReminderStage.Red ? RedBackColor : GreenBackColor;
        var hintColor = stage == ReminderStage.Red ? RedHintColor : GreenHintColor;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = backColor;
        Cursor = Cursors.Hand;
        Text = "Meeting Reminder";

        var layout = BuildLayout(dueMeetings, backColor, hintColor);
        Controls.Add(layout);

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = AutoCloseAfterMilliseconds };
        _autoCloseTimer.Tick += (_, _) => Close();
        _autoCloseTimer.Start();

        FormClosed += (_, _) => _autoCloseTimer.Dispose();
    }

    /// <summary>Wird nach dem Schließen ausgewertet, um ggf. eine erneute Erinnerung einzuplanen.</summary>
    public bool SnoozeRequested { get; private set; }

    private TableLayoutPanel BuildLayout(IReadOnlyList<Meeting> dueMeetings, Color backColor, Color hintColor)
    {
        var titleText = string.Join(Environment.NewLine, dueMeetings.Select(m => m.Title));
        var timeText = $"{dueMeetings[0].Hour:D2}:{dueMeetings[0].Minute:D2} Uhr";
        var headerText = BuildHeaderText(dueMeetings[0]);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = backColor,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var headerLabel = CreateLabel(headerText, 44f, FontStyle.Bold);
        var titleLabel = CreateLabel(titleText, 64f, FontStyle.Bold);
        var timeLabel = CreateLabel(timeText, 30f, FontStyle.Regular);

        var hintLabel = CreateLabel(
            "Klicken, ESC oder Enter zum Schließen",
            13f,
            FontStyle.Regular);
        hintLabel.ForeColor = hintColor;

        var snoozeButton = new Button
        {
            Text = "5 Minuten später erinnern",
            Font = new Font("Segoe UI", 14f),
            Dock = DockStyle.Fill,
            Margin = new Padding(200, 8, 200, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = backColor,
            Cursor = Cursors.Hand,
        };
        snoozeButton.FlatAppearance.BorderSize = 0;
        snoozeButton.Click += (_, _) =>
        {
            SnoozeRequested = true;
            Close();
        };

        layout.Controls.Add(headerLabel, 0, 0);
        layout.Controls.Add(titleLabel, 0, 1);
        layout.Controls.Add(timeLabel, 0, 2);
        layout.Controls.Add(snoozeButton, 0, 3);
        layout.Controls.Add(hintLabel, 0, 4);

        foreach (Control control in layout.Controls)
        {
            if (control != snoozeButton)
            {
                control.Click += (_, _) => Close();
            }
        }

        return layout;
    }

    /// <summary>
    /// Errechnet den Kopfzeilentext aus dem tatsächlichen Abstand zur Terminzeit, statt fest
    /// "JETZT" anzunehmen - passt sich so sowohl den konfigurierbaren Vorlaufzeiten (siehe
    /// <see cref="ReminderSettings"/>) als auch einer per Snooze verschobenen, ggf. bereits
    /// laufenden Erinnerung an. Unter einer Minute wird in Sekunden statt Minuten gezählt, damit
    /// z.B. der standardmäßig 30 Sekunden vorher feuernde rote Hinweis nicht ungenau auf
    /// "1 Minute" aufgerundet wird.
    /// </summary>
    private static string BuildHeaderText(Meeting meeting)
    {
        var now = DateTime.Now;
        var scheduledAt = now.Date + meeting.TimeOfDay;
        var remainingSeconds = (int)Math.Round((scheduledAt - now).TotalSeconds, MidpointRounding.AwayFromZero);

        if (remainingSeconds <= 0)
        {
            return "MEETING JETZT!";
        }

        if (remainingSeconds < 60)
        {
            return remainingSeconds == 1 ? "MEETING IN 1 SEKUNDE!" : $"MEETING IN {remainingSeconds} SEKUNDEN!";
        }

        var minutes = (int)Math.Round(remainingSeconds / 60.0, MidpointRounding.AwayFromZero);
        return minutes == 1 ? "MEETING IN 1 MINUTE!" : $"MEETING IN {minutes} MINUTEN!";
    }

    private Label CreateLabel(string text, float fontSize, FontStyle style) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", fontSize, style),
        ForeColor = Color.White,
        BackColor = Color.Transparent,
        AutoEllipsis = true,
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Escape or Keys.Enter)
        {
            Close();
        }
    }
}
