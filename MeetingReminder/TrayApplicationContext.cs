using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using MeetingReminder.Forms;
using MeetingReminder.Models;
using MeetingReminder.Services;

namespace MeetingReminder;

/// <summary>
/// Hält die Anwendung ohne sichtbares Hauptfenster am Leben, verwaltet das Tray-Icon,
/// den Terminplan und die Popup-Anzeige.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly MeetingStore _store;
    private readonly ReminderScheduler _scheduler;
    private readonly ToolStripMenuItem _autostartMenuItem;
    private readonly Queue<IReadOnlyList<Meeting>> _pendingPopups = new();

    private List<Meeting> _meetings;
    private ReminderPopupForm? _activePopup;
    private bool _isExiting;

    public TrayApplicationContext()
    {
        _store = new MeetingStore();
        _meetings = _store.Load();

        var editMenuItem = new ToolStripMenuItem("Termine bearbeiten...");
        editMenuItem.Click += (_, _) => EditMeetings();

        var openFileMenuItem = new ToolStripMenuItem("Termindatei im Explorer anzeigen");
        openFileMenuItem.Click += (_, _) => OpenDataFileLocation();

        _autostartMenuItem = new ToolStripMenuItem("Automatisch mit Windows starten")
        {
            CheckOnClick = true,
            Checked = AutostartManager.IsEnabled(),
        };
        _autostartMenuItem.Click += (_, _) => AutostartManager.SetEnabled(_autostartMenuItem.Checked);

        var exitMenuItem = new ToolStripMenuItem("Beenden");
        exitMenuItem.Click += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.Add(editMenuItem);
        menu.Items.Add(openFileMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitMenuItem);

        _trayIcon = new NotifyIcon
        {
            Icon = TrayIconFactory.CreateTrayIcon(),
            Visible = true,
            Text = "Meeting Reminder",
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => EditMeetings();

        _scheduler = new ReminderScheduler(() => _meetings);
        _scheduler.MeetingsDue += OnMeetingsDue;
        _scheduler.Start();
    }

    private void OnMeetingsDue(object? sender, IReadOnlyList<Meeting> dueMeetings)
    {
        _pendingPopups.Enqueue(dueMeetings);
        ShowNextPopupIfIdle();
    }

    private void ShowNextPopupIfIdle()
    {
        if (_isExiting || _activePopup is not null || _pendingPopups.Count == 0)
        {
            return;
        }

        var dueMeetings = _pendingPopups.Dequeue();
        var popup = new ReminderPopupForm(dueMeetings);
        _activePopup = popup;

        popup.FormClosed += (_, _) =>
        {
            if (popup.SnoozeRequested)
            {
                _scheduler.Snooze(dueMeetings, TimeSpan.FromMinutes(5));
            }

            popup.Dispose();
            _activePopup = null;
            ShowNextPopupIfIdle();
        };

        popup.Show();
        popup.Activate();
    }

    private void EditMeetings()
    {
        using var form = new EditMeetingsForm(_meetings);
        if (form.ShowDialog() == DialogResult.OK && form.SavedMeetings is not null)
        {
            _meetings = form.SavedMeetings;
            _store.Save(_meetings);
        }
    }

    private void OpenDataFileLocation()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{_store.FilePath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ordner konnte nicht geöffnet werden: {ex.Message}",
                "Meeting Reminder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _scheduler.Stop();
        _pendingPopups.Clear();
        _activePopup?.Close();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        ExitThread();
    }
}
