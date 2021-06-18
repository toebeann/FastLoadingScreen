using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using QModManager.API.ModLoading;
using Logger = BepInEx.Subnautica.Logger;

namespace Straitjacket.Subnautica.Mods.FastLoadingScreen
{
    using Patches;

    [QModCore]
    public static class Main
    {
        private const string Game
#if SUBNAUTICA
            = "Subnautica";
#elif BELOWZERO
            = "Below Zero";
#endif


        [QModPatch, Obsolete("Should not be used!", true)]
        public static void Initialise()
        {
            Logger.LogInfo($"Initialising Fast Loading Screen for {Game} v{Assembly.GetExecutingAssembly().GetName().Version}...");
            var stopwatch = Stopwatch.StartNew();

            ApplyHarmonyPathches();

            stopwatch.Stop();
            Logger.LogInfo($"Initialised in {stopwatch.ElapsedMilliseconds}ms.");
        }

        private static void ApplyHarmonyPathches()
        {
            var stopwatch = Stopwatch.StartNew();

            var harmony = new Harmony("Fast Loading Screen");
            harmony.PatchAll(typeof(WaitScreenPatch));

            stopwatch.Stop();
            Logger.LogInfo($"Harmony patches applied in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
