using System;
using System.IO;

namespace MeetingReminder.Services;

/// <summary>
/// Findet das Verzeichnis, in dem die eingecheckten Daten-/Einstellungsdateien
/// (meetings.json, settings.json) liegen: ausgehend vom Ausführungsverzeichnis nach oben
/// gesucht nach MeetingReminder.csproj. Läuft die .exe eigenständig ohne umgebendes
/// Repository (z.B. nach "dotnet publish" an einen anderen Ort kopiert), wird stattdessen
/// das Verzeichnis neben der .exe verwendet.
/// </summary>
internal static class DataDirectoryResolver
{
    private const string ProjectFileName = "MeetingReminder.csproj";

    public static string Resolve()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (dir.GetFiles(ProjectFileName).Length > 0)
            {
                return dir.FullName;
            }
        }

        return AppContext.BaseDirectory;
    }
}
