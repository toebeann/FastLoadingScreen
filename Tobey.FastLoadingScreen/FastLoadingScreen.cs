using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Diagnostics;

namespace Tobey.FastLoadingScreen;
using Patches;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class FastLoadingScreen : BaseUnityPlugin
{
    public static FastLoadingScreen Instance { get; private set; }
    internal static ManualLogSource Log => Instance.Logger;

    private void Awake()
    {
        // enforce singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        Logger.LogInfo($"Initialising Fast Loading Screen v{PluginInfo.PLUGIN_VERSION}");
        var stopwatch = Stopwatch.StartNew();

        ApplyHarmonyPatches();

        stopwatch.Stop();
        Logger.LogInfo($"Initialised in {stopwatch.ElapsedMilliseconds}");
    }

    private void ApplyHarmonyPatches()
    {
        var stopwatch = Stopwatch.StartNew();

        var harmony = new Harmony("Fast Loading Screen");
        harmony.PatchAll(typeof(LoadingScreenPatch));

        stopwatch.Stop();
        Logger.LogInfo($"Harmony patches applied in {stopwatch.ElapsedMilliseconds}ms.");
    }
}
