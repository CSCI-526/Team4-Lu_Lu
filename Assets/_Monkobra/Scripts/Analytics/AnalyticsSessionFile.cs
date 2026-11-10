using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// The file half of analytics: where the session report lives and how it gets
/// out, as a file or as a browser download. Holds no analytics data, the text
/// comes from <see cref="AnalyticsMarkdownWriter"/>.
/// </summary>
public class AnalyticsSessionFile {
    // Public Members  #########################################################
    /// <summary>
    /// folder every report lands in
    /// </summary>
    public static string FolderPath {
        get {
            return Path.Combine(Application.persistentDataPath, FOLDER_NAME);
        }
    }

    /// <summary>
    /// full path of the session file, empty until one opens
    /// </summary>
    public string FilePath {
        get; private set;
    } = string.Empty;

    /// <summary>
    /// if a session file is open
    /// </summary>
    public bool IsOpen {
        get {
            return !string.IsNullOrEmpty(FilePath);
        }
    }

    // Public Methods  #########################################################
    /// <summary>
    /// whether the folder exists and <see cref="FilePath"/> now names the file
    /// of the session that started at <paramref name="start"/>; nothing is
    /// written yet
    /// </summary>
    public bool Open(DateTime start) {
        try {
            Directory.CreateDirectory(FolderPath);
        } catch (Exception e) {
            FilePath = string.Empty;
            Debug.LogWarning("Analytics:\tfail to open session file: " + e);
            return false;
        }

        FilePath = Path.Combine(FolderPath, GetFileName(start));
        return true;
    }

    /// <summary>
    /// forgets the file, after a failed first save
    /// </summary>
    public void Close() {
        FilePath = string.Empty;
    }

    /// <summary>
    /// whether the whole <paramref name="session"/> was written to the open
    /// file
    /// </summary>
    public bool Save(SessionData session) {
        if (!IsOpen) {
            return false;
        }

        return WriteFile(
            FilePath,
            AnalyticsMarkdownWriter.BuildSession(session)
        );
    }

    /// <summary>
    /// if the browser took the report of <paramref name="session"/> as a
    /// download, ie a WebGL player; elsewhere false, so the caller saves the
    /// file instead
    /// </summary>
    public bool TryDownload(SessionData session) {
        return WebDownload.SaveText(
            GetFileName(session.StartClock),
            AnalyticsMarkdownWriter.BuildSession(session)
        );
    }

    /// <summary>
    /// whether <paramref name="text"/> was written to
    /// <paramref name="path"/>, a failure logs a warning
    /// </summary>
    public static bool WriteFile(string path, string text) {
        try {
            File.WriteAllText(path, text, new UTF8Encoding(false));
        } catch (Exception e) {
            Debug.LogWarning("Analytics:\tfail to write session: " + e);
            return false;
        }

        return true;
    }

    // constants  ##############################################################
    private const string FOLDER_NAME = "Analytics";

    // private methods  ########################################################
    // named by the session start time, so files sort chronologically
    private static string GetFileName(DateTime start) {
        return "0" + start.ToString(
            "yyyy-MM-dd_HH_mm_ss",
            CultureInfo.InvariantCulture
        ) + ".md";
    }
}
