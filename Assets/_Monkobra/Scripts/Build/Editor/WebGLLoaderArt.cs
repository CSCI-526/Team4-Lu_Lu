using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Copies the title art into every WebGL build, under
/// <c>TemplateData/TitleScreen/</c>, where the FitWindow loading page reads
/// it. The page draws the same picture as the TitleScreen prefab, so it takes
/// the very sprites of <c>Sprites/TitleScreen/</c> and the title font, kept in
/// one place instead of a second copy inside the template.
/// </summary>
public class WebGLLoaderArt: IPostprocessBuildWithReport {
    // public members  #########################################################
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report) {
        if (report.summary.platform != BuildTarget.WebGL) {
            return;
        }

        string target = Path.Combine(report.summary.outputPath, TARGET_FOLDER);
        Directory.CreateDirectory(target);
        string[] sprites = Directory.GetFiles(SPRITE_FOLDER, "*.png");
        foreach (string sprite in sprites) {
            Copy(sprite, target);
        }
        Copy(FONT, target);
        Debug.Log(
            "WebGLLoaderArt:\tcopied " + (sprites.Length + 1) + " files to "
                + target
        );
    }

    // private methods  ########################################################
    private static void Copy(string source, string folder) {
        File.Copy(source, Path.Combine(folder, Path.GetFileName(source)), true);
    }

    // constants  ##############################################################
    private const string SPRITE_FOLDER = "Assets/_Monkobra/Sprites/TitleScreen";
    private const string FONT =
        "Assets/_Monkobra/Fonts/UI/MonkobraTutorial-SemiBold.ttf";
    private const string TARGET_FOLDER = "TemplateData/TitleScreen";
}
