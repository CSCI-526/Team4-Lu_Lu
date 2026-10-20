using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Hands a text file to the browser as a download. Only a WebGL player can:
/// the Editor and other platforms report false, so the caller saves the file
/// itself instead.
/// </summary>
public static class WebDownload {
    // Public Methods  #########################################################
    /// <summary>
    /// starts a browser download of <paramref name="content"/>
    /// </summary>
    /// <returns>if the browser took the download, ie a WebGL player</returns>
    public static bool SaveText(string filename, string content) {
#if UNITY_WEBGL && !UNITY_EDITOR
        MonkobraDownloadText(filename, content);
        if (Debug.isDebugBuild) {
            Debug.Log("WebDownload:\tdownload started " + filename);
        }
        return true;
#else
        return false;
#endif
    }

    // Private Helpers  ########################################################
#if UNITY_WEBGL && !UNITY_EDITOR
    // Plugins/WebGL/Download.jslib
    [DllImport("__Internal")]
    private static extern void MonkobraDownloadText(
        string filename,
        string content
    );
#endif
}
