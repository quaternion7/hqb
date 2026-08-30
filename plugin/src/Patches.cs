using FistVR;
using HarmonyLib;
using UnityEngine;

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
                Plugin.Instance.RebuildAfterQuickbeltChange(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(FVRViveHand), "TestQuickBeltDistances")]
    internal static class HandQuickbeltInteractionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(FVRViveHand __instance)
        {
            if (__instance == null || GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.QBSlots_Internal == null)
            {
                return;
            }

            FVRPhysicalObject physicalObject = __instance.CurrentInteractable as FVRPhysicalObject;
            if (physicalObject == null || physicalObject.QuickbeltSlot != null)
            {
                return;
            }

            FVRQuickBeltSlot closest = null;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < GM.CurrentPlayerBody.QBSlots_Internal.Count; index++)
            {
                FVRQuickBeltSlot slot = GM.CurrentPlayerBody.QBSlots_Internal[index];
                if (!CanStore(slot, physicalObject) || !IntersectsSlot(slot, physicalObject))
                {
                    continue;
                }

                float distance = (slot.HoverGeo.transform.position - physicalObject.transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = slot;
                    closestDistance = distance;
                }
            }

            if (closest != null)
            {
                __instance.CurrentHoveredQuickbeltSlot = closest;
                __instance.CurrentHoveredQuickbeltSlotDirty = closest;
            }
            else if (IsHandQuickbelt(__instance.CurrentHoveredQuickbeltSlot))
            {
                __instance.CurrentHoveredQuickbeltSlot = null;
                __instance.CurrentHoveredQuickbeltSlotDirty = null;
            }
        }

        private static bool CanStore(FVRQuickBeltSlot slot, FVRPhysicalObject physicalObject)
        {
            return IsHandQuickbelt(slot)
                && slot.IsSelectable
                && slot.CurObject == null
                && slot.HeldObject == null
                && slot.SizeLimit >= physicalObject.Size
                && slot.Type == physicalObject.QBSlotType;
        }

        private static bool IsHandQuickbelt(FVRQuickBeltSlot slot)
        {
            return slot != null && slot.GetComponent<HandQuickbeltSlotMarker>() != null;
        }

        private static bool IntersectsSlot(FVRQuickBeltSlot slot, FVRPhysicalObject physicalObject)
        {
            if (slot.HoverGeo == null)
            {
                return false;
            }

            Vector3 center = slot.HoverGeo.transform.position;
            Vector3 scale = slot.HoverGeo.transform.lossyScale;
            float radius = 0.5f * Mathf.Min(Mathf.Abs(scale.x), Mathf.Min(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float radiusSquared = radius * radius;
            Collider[] colliders = physicalObject.GetComponentsInChildren<Collider>(true);
            bool foundCollider = false;
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                {
                    continue;
                }

                foundCollider = true;
                if (collider.bounds.SqrDistance(center) <= radiusSquared)
                {
                    return true;
                }
            }

            if (foundCollider)
            {
                return false;
            }
            Transform point = physicalObject.PoseOverride != null ? physicalObject.PoseOverride : physicalObject.transform;
            return slot.HoverGeo.transform.InverseTransformPoint(point.position).magnitude < 0.5f;
        }
    }

    [HarmonyPatch]
    internal static class StoredObjectScalePatch
    {
        [HarmonyPatch(typeof(FVRPhysicalObject), "SetParentage")]
        [HarmonyPrefix]
        private static void BeforeSetParentage(FVRPhysicalObject __instance)
        {
            RestoreBeforeRemoval(__instance);
        }

        [HarmonyPatch(typeof(FVRPhysicalObject), "SetQuickBeltSlot")]
        [HarmonyPrefix]
        private static void BeforeClearQuickbelt(FVRPhysicalObject __instance, FVRQuickBeltSlot slot)
        {
            if (slot == null)
            {
                RestoreBeforeRemoval(__instance);
            }
        }

        private static void RestoreBeforeRemoval(FVRPhysicalObject physicalObject)
        {
            if (physicalObject == null || physicalObject.QuickbeltSlot == null)
            {
                return;
            }
            HandQuickbeltMiniaturizer miniaturizer = physicalObject.QuickbeltSlot.GetComponent<HandQuickbeltMiniaturizer>();
            if (miniaturizer != null)
            {
                miniaturizer.RestoreBeforeRemoval(physicalObject);
            }
        }
    }
}
