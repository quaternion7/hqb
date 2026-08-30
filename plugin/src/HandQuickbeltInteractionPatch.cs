using FistVR;
using HarmonyLib;
using UnityEngine;

namespace HandQuickbelts
{
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
                if (!CanStore(slot, physicalObject) || !IntersectsVisibleSphere(slot, physicalObject))
                {
                    continue;
                }

                float distance = (slot.HoverGeo.transform.position - physicalObject.transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = slot;
                }
            }

            if (closest != null)
            {
                __instance.CurrentHoveredQuickbeltSlotDirty = closest;
                __instance.CurrentHoveredQuickbeltSlot = closest;
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
            return slot != null && slot.GetComponent<HandQuickbeltSingleVisual>() != null;
        }

        private static bool IntersectsVisibleSphere(FVRQuickBeltSlot slot, FVRPhysicalObject physicalObject)
        {
            if (slot.HoverGeo == null)
            {
                return false;
            }

            Vector3 center = slot.HoverGeo.transform.position;
            Vector3 worldScale = slot.HoverGeo.transform.lossyScale;
            float radius = 0.5f * Mathf.Min(Mathf.Abs(worldScale.x), Mathf.Min(Mathf.Abs(worldScale.y), Mathf.Abs(worldScale.z)));
            float radiusSquared = radius * radius;
            Collider[] colliders = physicalObject.GetComponentsInChildren<Collider>(true);
            bool foundUsableCollider = false;

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                {
                    continue;
                }

                foundUsableCollider = true;
                if (collider.bounds.SqrDistance(center) <= radiusSquared)
                {
                    return true;
                }
            }

            if (!foundUsableCollider)
            {
                Transform testPoint = physicalObject.PoseOverride != null ? physicalObject.PoseOverride : physicalObject.transform;
                return slot.HoverGeo.transform.InverseTransformPoint(testPoint.position).magnitude < 0.5f;
            }

            return false;
        }
    }
}
