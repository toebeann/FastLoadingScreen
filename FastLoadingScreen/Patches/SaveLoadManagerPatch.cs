using HarmonyLib;
using System;

namespace Straitjacket.Subnautica.Mods.FastLoadingScreen.Patches
{
    internal static class SaveLoadManagerPatch
    {
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadAsync), argumentTypes: new Type[0])]
        [HarmonyPostfix]
        public static void LoadAsyncPostfix()
        {

        }
    }
}
