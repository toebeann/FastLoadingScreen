using HarmonyLib;
using System;
using System.Reflection;
using QModManager.API.ModLoading;

namespace Straitjacket.Subnautica.Mods.FastLoadingScreen
{
    [QModCore]
    public static class Main
    {
        [QModPatch, Obsolete("Should not be used!", true)]
        public static void Patch()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "FastLoadingScreen");
        }
    }
}
