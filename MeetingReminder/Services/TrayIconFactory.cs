using System.Drawing;
using System.Drawing.Drawing2D;
using MeetingReminder.Native;

namespace MeetingReminder.Services;

/// <summary>
/// Zeichnet das Tray-Icon (rote Uhr) zur Laufzeit, damit keine binäre .ico-Datei
/// im Repository gepflegt werden muss.
/// </summary>
internal static class TrayIconFactory
{
    public static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var background = new SolidBrush(Color.FromArgb(220, 30, 30));
            g.FillEllipse(background, 1, 1, 30, 30);

            using var outline = new Pen(Color.White, 2f);
            g.DrawEllipse(outline, 4, 4, 24, 24);

            using var hands = new Pen(Color.White, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(hands, 16, 16, 16, 8);
            g.DrawLine(hands, 16, 16, 22, 18);
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(hIcon);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }
}
