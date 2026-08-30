using FistVR;
using HarmonyLib;

namespace HandQuickbelts
{
    [HarmonyPatch(typeof(FVRPlayerBody), "ConfigureQuickbelt")]
    internal static class ConfigureQuickbeltPatch
    {
        [HarmonyPostfix]
        private static void Postfix(FVRPlayerBody __instance)
        {
            if (Plugin.Instance != null)
            {
                Plugin.Instance.OnQuickbeltConfigured(__instance);
            }
        }
    }
}

