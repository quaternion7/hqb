using System;
using System.Collections.Generic;
using System.Reflection;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace HandQuickbelts
{
    [HarmonyPatch]
    internal static class SpawnlockScaleCompatibility
    {
        private const float RelativeTolerance = 0.01f;

        internal struct CloneScaleState
        {
            internal bool Eligible;
            internal Vector3 OriginalWorldScale;
            internal Vector3 StoredLocalScale;
            internal Vector3 PrefabScale;
        }

        // Compatibility postfixes on these overrides run after the base method returns.
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] types = { typeof(FVRFireArmMagazine), typeof(FVRFireArmRound),
                typeof(FVRFireArmClip), typeof(FVRGrenade), typeof(PinnedGrenade), typeof(Speedloader) };
            foreach (Type type in types)
            {
                yield return AccessTools.DeclaredMethod(type, "DuplicateFromSpawnLock", new[] { typeof(FVRViveHand) });
            }
        }

        [HarmonyPrefix]
        private static void Prefix(FVRPhysicalObject __instance, out CloneScaleState __state)
        {
            __state = new CloneScaleState();
            if (__instance == null || __instance.QuickbeltSlot == null)
            {
                return;
            }
            FVRQuickBeltSlot slot = __instance.QuickbeltSlot;
            if (slot.GetComponent<HandQuickbeltSlotMarker>() == null || slot.QuickbeltRoot == null
                || !__instance.transform.IsChildOf(slot.QuickbeltRoot) || __instance.ObjectWrapper == null)
            {
                return;
            }

            Vector3 parentScale = __instance.transform.parent.lossyScale;
            // Restrict this heuristic to the known, uniformly scaled palm hierarchy.
            if (!Close(parentScale.x, 0.1f) || !Close(parentScale.y, parentScale.x)
                || !Close(parentScale.z, parentScale.x))
            {
                return;
            }

            __state.StoredLocalScale = __instance.transform.localScale;
            Vector3 originalLocalScale = __state.StoredLocalScale;
            HandQuickbeltMiniaturizer miniaturizer = slot.GetComponent<HandQuickbeltMiniaturizer>();
            if (miniaturizer != null)
            {
                miniaturizer.TryGetOriginalScale(__instance, out originalLocalScale);
            }
            __state.OriginalWorldScale = Vector3.Scale(originalLocalScale, parentScale);
            GameObject prefab = __instance.ObjectWrapper.GetGameObject();
            if (prefab == null)
            {
                return;
            }
            __state.PrefabScale = prefab.transform.localScale;
            __state.Eligible = true;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(FVRPhysicalObject __instance, GameObject __result, CloneScaleState __state)
        {
            if (!__state.Eligible || __result == null || __instance == null || __result == __instance.gameObject)
            {
                return;
            }
            // A scale-copying postfix transfers the stored local scale into world space.
            // That scale includes optional fitting, so it need not be 10x the original.
            Vector3 expectedOversizedScale = __state.StoredLocalScale;
            Vector3 actualScale = __result.transform.lossyScale;
            // Vanilla clones use the prefab scale, even when the stored source was resized.
            // Prefer a missed correction over changing an otherwise normal vanilla clone.
            if (Close(actualScale.x, __state.PrefabScale.x) && Close(actualScale.y, __state.PrefabScale.y)
                && Close(actualScale.z, __state.PrefabScale.z))
            {
                return;
            }
            if (Close(actualScale.x, expectedOversizedScale.x)
                && Close(actualScale.y, expectedOversizedScale.y)
                && Close(actualScale.z, expectedOversizedScale.z))
            {
                // Restore the pre-fit size, retaining small deviations within tolerance.
                // Require a uniform correction so rotated/parented clones remain safe.
                float correction = __state.OriginalWorldScale.x / expectedOversizedScale.x;
                if (Close(correction, correction)
                    && Close(expectedOversizedScale.y * correction, __state.OriginalWorldScale.y)
                    && Close(expectedOversizedScale.z * correction, __state.OriginalWorldScale.z))
                {
                    __result.transform.localScale *= correction;
                }
            }
        }

        private static bool Close(float actual, float expected)
        {
            return actual > 0f && expected > 0f && !float.IsInfinity(actual) && !float.IsInfinity(expected)
                && Mathf.Abs(actual - expected) <= expected * RelativeTolerance;
        }
    }
}
