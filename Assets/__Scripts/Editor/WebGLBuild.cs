using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Supermarket.EditorTools
{
  /// <summary>
  /// Repeatable WebGL build for the course submission. Output goes to
  /// Builds/WebGL (ignored by git); zip that folder to upload to Unity Play.
  /// </summary>
  public static class WebGLBuild
  {
    public const string OutputPath = "Builds/WebGL";

    [MenuItem("Tools/Supermarket/Build/WebGL")]
    public static void BuildFromMenu()
    {
      BuildWebGL();
    }

    /// <summary>Entry point for Unity MCP / batch automation.</summary>
    public static void BuildWebGL()
    {
      List<string> scenes = new List<string>();
      for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
      {
        EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
        if (scene.enabled) scenes.Add(scene.path);
      }

      if (scenes.Count == 0)
      {
        Debug.LogError("WebGL build aborted: no enabled scenes in Build Settings.");
        return;
      }

      if (Directory.Exists(OutputPath))
      {
        Directory.Delete(OutputPath, true);
      }

      BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
      {
        scenes = scenes.ToArray(),
        locationPathName = OutputPath,
        target = BuildTarget.WebGL,
        options = BuildOptions.None
      });

      BuildSummary summary = report.summary;
      if (summary.result != BuildResult.Succeeded)
      {
        Debug.LogError(string.Format(
          "WebGL build failed: {0} error(s), {1} warning(s). See the console above.",
          summary.totalErrors, summary.totalWarnings));
        return;
      }

      Debug.Log(string.Format(
        "WebGL build succeeded: {0} ({1:N1} MB) in {2:N1}s. Zip this folder and upload it to Unity Play.",
        OutputPath,
        summary.totalSize / (1024f * 1024f),
        summary.totalTime.TotalSeconds));
    }
  }
}
