using HarmonyLib;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UWE;
using Logger = BepInEx.Subnautica.Logger;

namespace Straitjacket.Subnautica.Mods.FastLoadingScreen.Patches
{
    internal static class WaitScreenPatch
    {
        private static int frameRate;
        private static int vSyncCount;

        private static double benchmark = -1;
        private static Stopwatch stopwatch;
        private static bool benchmarking;

        [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.Show))]
        [HarmonyPrefix]
        public static void ShowPrefix()
        {
            if (stopwatch is Stopwatch sw && sw.IsRunning)
                return;

            benchmarking = VirtualKey.GetKey(KeyCode.LeftControl);
            if (benchmarking)
            {
                Logger.LogMessage("Running benchmark without boosts for comparison, starting timer...");
            }
            else
            {
                Logger.LogMessage("Boosting loading times...");
                frameRate = Application.targetFrameRate;
                Application.targetFrameRate = -1;

                vSyncCount = QualitySettings.vSyncCount;
                QualitySettings.vSyncCount = 0;
            }
            stopwatch = Stopwatch.StartNew();
        }

        private static Coroutine end;
        [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.Hide))]
        [HarmonyPostfix]
        public static void EndPostfix()
        {
            if (end is Coroutine)
            {
#if SUBNAUTICA
                CoroutineHost.main.StopCoroutine(end);
#elif BELOWZERO
                CoroutineHost.StopCoroutine(end);
#endif
            }

            end = CoroutineHost.StartCoroutine(End());
        }

        private static IEnumerator End()
        {
            yield return null;
            yield return new WaitWhile(() => WaitScreen.main.isShown);

            stopwatch.Stop();
            if (benchmarking)
            {
                benchmark = stopwatch.Elapsed.TotalSeconds;
                Logger.LogMessage($"Benchmark complete, loading completed in {benchmark:N2}s.");
            }
            else
            {
                if (benchmark >= 0)
                {
                    Logger.LogMessage($"Loading completed in {stopwatch.Elapsed.TotalSeconds:N2}s, vs. unboosted benchmark of {benchmark:N2}s.");

                    Logger.LogMessage(stopwatch.Elapsed.TotalSeconds switch
                    {
                        double seconds when seconds < benchmark => $"Improvement of {benchmark - stopwatch.Elapsed.TotalSeconds:N2}s.",
                        double seconds when seconds > benchmark => $"Degradation of {stopwatch.Elapsed.TotalSeconds - benchmark:N2}s.",
                        _ => "No performance difference recorded."
                    });
                }
                else
                {
                    Logger.LogMessage($"Loading completed in {stopwatch.Elapsed.TotalSeconds:N2}s, " +
                    $"resetting FPS cap and VSync per user preferences ({frameRate}, {vSyncCount})");
                    Application.targetFrameRate = frameRate;
                    QualitySettings.vSyncCount = vSyncCount;
                }
            }
            stopwatch = null;
            end = null;
        }
    }
}
