using BepInEx.Logging;
using HarmonyLib;
using Straitjacket;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UWE;

namespace Tobey.FastLoadingScreen.Patches;
internal static class LoadingScreenPatch
{
    private static ManualLogSource Logger => FastLoadingScreen.Log;

    private static int frameRate;
    private static int vSyncCount;

    private static double benchmark = -1;
    private static Stopwatch stopwatch;
    private static bool benchmarking;

    [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.LoadGameAsync))]
    [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.StartNewGame))]
    [HarmonyPrefix]
    public static void LoadPrefix(uGUI_MainMenu __instance)
    {
        if (__instance.isStartingNewGame || (stopwatch is Stopwatch sw && sw.IsRunning))
        {
            return;
        }

        benchmarking = VirtualKey.GetKey(KeyCode.LeftControl);
        if (benchmarking)
        {
            Logger.LogMessage("Running benchmark without boosts for comparison, starting timer...");
        }
        else
        {
            Logger.LogInfo("Boosting loading times...");
            frameRate = Application.targetFrameRate;
            Application.targetFrameRate = -1;

            vSyncCount = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
        }
        stopwatch = Stopwatch.StartNew();
    }

    private static Coroutine end;

    [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.ReportStageDurations))]
    [HarmonyPrefix]
    public static void ReportStageDurationsPrefix()
    {
        if (stopwatch is not null)
        {
            if (end is Coroutine)
            {
                CoroutineHost.StopCoroutine(end);
            }

            end = CoroutineHost.StartCoroutine(End());
        }
    }

    private static IEnumerator End()
    {
        int frame = Time.frameCount;
        yield return new WaitUntil(() => Time.frameCount > frame);
        yield return new WaitWhile(() => WaitScreen.main.isWaiting);

        stopwatch.Stop();
        if (benchmarking)
        {
            benchmark = stopwatch.Elapsed.TotalSeconds;
            Logger.LogMessage($"Benchmark complete, loading completed in {benchmark:N2}s.");
        }
        else
        {
            Logger.LogInfo($"Loading completed in {stopwatch.Elapsed.TotalSeconds:N2}s, " +
                    $"resetting FPS cap and VSync per user preferences ({frameRate}, {vSyncCount})");
            Application.targetFrameRate = frameRate;
            QualitySettings.vSyncCount = vSyncCount;

            if (benchmark >= 0)
            {
                Logger.LogMessage($"Loading completed in {stopwatch.Elapsed.TotalSeconds:N2}s, vs. unboosted benchmark of {benchmark:N2}s.");
                if (stopwatch.Elapsed.TotalSeconds < benchmark)
                {
                    Logger.LogMessage($"Improvement of {benchmark - stopwatch.Elapsed.TotalSeconds:N2}s.");
                }
                else if (stopwatch.Elapsed.TotalSeconds > benchmark)
                {
                    Logger.LogMessage($"Degradation of {stopwatch.Elapsed.TotalSeconds - benchmark:N2}s.");
                }
                else
                {
                    Logger.LogMessage("No performance difference recorded.");
                }
            }
        }
        stopwatch = null;
        end = null;
    }
}
