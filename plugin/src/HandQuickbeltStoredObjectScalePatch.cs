using FistVR;
using HarmonyLib;

namespace HandQuickbelts
{
    [HarmonyPatch]
    internal static class HandQuickbeltStoredObjectScalePatch
    {
        [HarmonyPatch(typeof(FVRPhysicalObject), "BeginInteraction")]
        [HarmonyPrefix]
        private static void BeforeBeginInteraction(FVRPhysicalObject __instance)
        {
            RestoreBeforeParentChange(__instance);
        }

        [HarmonyPatch(typeof(FVRPhysicalObject), "SetParentage")]
        [HarmonyPrefix]
        private static void BeforeSetParentage(FVRPhysicalObject __instance)
        {
            RestoreBeforeParentChange(__instance);
        }

        [HarmonyPatch(typeof(FVRPhysicalObject), "SetQuickBeltSlot")]
        [HarmonyPrefix]
        private static void BeforeSetQuickBeltSlot(FVRPhysicalObject __instance, FVRQuickBeltSlot slot)
        {
            if (slot == null)
            {
                RestoreBeforeParentChange(__instance);
            }
        }

        private static void RestoreBeforeParentChange(FVRPhysicalObject physicalObject)
        {
            if (physicalObject == null || physicalObject.QuickbeltSlot == null)
            {
                return;
            }

            HandQuickbeltStoredObjectScaler scaler = physicalObject.QuickbeltSlot.GetComponent<HandQuickbeltStoredObjectScaler>();
            if (scaler != null)
            {
                scaler.RestoreBeforeRemoval(physicalObject);
            }
        }
    }
}
