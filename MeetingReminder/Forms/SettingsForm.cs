using System;
using System.Drawing;
using System.Windows.Forms;
using MeetingReminder.Models;

namespace MeetingReminder.Forms;

/// <summary>Dialog zum Einstellen der beiden Erinnerungs-Vorlaufzeiten (grün/rot).</summary>
public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _greenMinutesInput;
    private readonly NumericUpDown _redSecondsInput;

    public SettingsForm(ReminderSettings settings)
    {
        Text = "Einstellungen";
        Width = 440;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowIcon = false;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(16),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var greenLabel = new Label
        {
            Text = "Grüner Hinweis: Minuten vor dem Meeting",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _greenMinutesInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 180,
            Value = Math.Clamp(settings.GreenLeadSeconds / 60, 0, 180),
            Dock = DockStyle.Fill,
        };

        var redLabel = new Label
        {
            Text = "Roter Hinweis: Sekunden vor dem Meeting",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _redSecondsInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 3600,
            Value = Math.Clamp(settings.RedLeadSeconds, 0, 3600),
            Dock = DockStyle.Fill,
        };

        var hintLabel = new Label
        {
            Text = "0 deaktiviert die jeweilige Stufe. Beide Hinweise feuern unabhängig voneinander.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Color.DimGray,
        };

        layout.Controls.Add(greenLabel, 0, 0);
        layout.Controls.Add(_greenMinutesInput, 1, 0);
        layout.Controls.Add(redLabel, 0, 1);
        layout.Controls.Add(_redSecondsInput, 1, 1);
        layout.Controls.Add(hintLabel, 0, 2);
        layout.SetColumnSpan(hintLabel, 2);

        var saveButton = new Button { Text = "Speichern", Width = 100 };
        var cancelButton = new Button { Text = "Abbrechen", Width = 100 };
        saveButton.Click += (_, _) =>
        {
            SavedSettings = new ReminderSettings
            {
                GreenLeadSeconds = (int)_greenMinutesInput.Value * 60,
                RedLeadSeconds = (int)_redSecondsInput.Value,
            };
            DialogResult = DialogResult.OK;
            Close();
        };
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

        Controls.Add(layout);
        Controls.Add(buttonPanel);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    /// <summary>Enthält die neuen Einstellungen, nachdem der Dialog mit "Speichern" geschlossen wurde.</summary>
    public ReminderSettings? SavedSettings { get; private set; }
}
