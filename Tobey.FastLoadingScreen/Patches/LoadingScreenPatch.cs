using BepInEx.Logging;
using HarmonyLib;
using Straitjacket;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
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

    internal static class Start
    {
        static IEnumerable<MethodBase> TargetMethods() => new[] {
            AccessTools.Method(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.StartNewGame)),
            AccessTools.Method(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.LoadGameAsync)),
            AccessTools.FirstMethod(typeof(WaitScreen), (info) => info.Name == "Show")
            }
        .Where(info => info is not null);

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static void Patch()
        {
            if (stopwatch?.IsRunning ?? false)
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
    }

    internal static class Finish
    {
        static MethodBase TargetMethod() => AccessTools.FirstMethod(
            typeof(WaitScreen),
            (info) => info.Name == nameof(WaitScreen.ReportStageDurations) || info.Name == "Show");

        private static Coroutine end;

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static void Patch()
        {
            if (stopwatch is not null)
            {
                if (end is not null)
                {
                    (CoroutineHost.Initialize() as MonoBehaviour).StopCoroutine(end);
                }

                end = (CoroutineHost.Initialize() as MonoBehaviour).StartCoroutine(End());
            }
        }

        static Traverse<bool> IsWaitingOrShown => Traverse.Create(WaitScreen.main).Field(nameof(WaitScreen.isWaiting)) switch
        {
            Traverse t when t.FieldExists() => new(t),
            _ => Traverse.Create(WaitScreen.main).Field<bool>("isShown")
        };

        private static IEnumerator End()
        {
            yield return null;
            yield return new WaitWhile(() => IsWaitingOrShown.Value);

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
}
