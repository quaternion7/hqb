using System;
using System.Collections.Generic;
using FistVR;
using UnityEngine;

namespace HandQuickbelts
{
    internal sealed class HandQuickbeltStoredObjectScaler : MonoBehaviour
    {
        private FVRQuickBeltSlot _slot;
        private FVRPhysicalObject _trackedObject;
        private Vector3 _originalLocalScale;
        private Vector3 _storedLocalScale;
        private Vector3 _measuredBoundsSize;
        private float _scaleFactor = 1.0f;
        private int _targetSizeMillimeters;
        private int _knownTransformCount;
        private readonly HashSet<GameObject> _knownPhysicalObjects = new HashSet<GameObject>();

        internal string DiagnosticSummary
        {
            get
            {
                return string.Format(
                    "tracked={0}; bounds={1}; originalScale={2}; storedScale={3}; factor={4:0.####}; target={5} mm",
                    _trackedObject != null ? _trackedObject.name : "null",
                    _measuredBoundsSize,
                    _originalLocalScale,
                    _storedLocalScale,
                    _scaleFactor,
                    _targetSizeMillimeters);
            }
        }

        private void Awake()
        {
            _slot = GetComponent<FVRQuickBeltSlot>();
        }

        private void Update()
        {
            if (_slot == null)
            {
                _slot = GetComponent<FVRQuickBeltSlot>();
            }

            FVRPhysicalObject current = null;
            if (Plugin.MiniaturizeStoredObjects.Value && _slot != null)
            {
                current = _slot.HeldObject as FVRPhysicalObject;
            }

            if (current != _trackedObject)
            {
                RestoreNow();
                if (current != null)
                {
                    Track(current);
                }
            }

            if (_trackedObject == null)
            {
                return;
            }

            int configuredTarget = Plugin.StoredObjectTargetSizeMillimeters.Value;
            if (configuredTarget != _targetSizeMillimeters)
            {
                RestoreRootScale();
                CalculateStoredScale(configuredTarget);
                ApplyStoredScale();
            }

            if (_trackedObject.IsHeld)
            {
                RestoreRootScale();
                return;
            }

            HandleHierarchyChanges();
            ApplyStoredScale();
        }

        private void OnDestroy()
        {
            RestoreNow();
        }

        internal void RestoreNow()
        {
            RestoreRootScale();
            _trackedObject = null;
            _scaleFactor = 1.0f;
            _storedLocalScale = Vector3.zero;
            _measuredBoundsSize = Vector3.zero;
            _knownTransformCount = 0;
            _knownPhysicalObjects.Clear();
        }

        internal void RestoreBeforeRemoval(FVRPhysicalObject physicalObject)
        {
            if (_trackedObject == physicalObject)
            {
                RestoreNow();
            }
        }

        private void Track(FVRPhysicalObject physicalObject)
        {
            _trackedObject = physicalObject;
            _originalLocalScale = physicalObject.transform.localScale;
            RecordHierarchy();
            CalculateStoredScale(Plugin.StoredObjectTargetSizeMillimeters.Value);
            ApplyStoredScale();
        }

        private void CalculateStoredScale(int targetSizeMillimeters)
        {
            _targetSizeMillimeters = targetSizeMillimeters;
            _scaleFactor = 1.0f;
            _storedLocalScale = _originalLocalScale;

            if (_trackedObject == null)
            {
                return;
            }

            Bounds bounds = CalculateBounds(_trackedObject.gameObject);
            float targetSize = Mathf.Max(0.001f, Mathf.Abs(targetSizeMillimeters * 0.001f));
            Vector3 size = bounds.size;
            _measuredBoundsSize = size;

            if (size.x > targetSize || size.y > targetSize || size.z > targetSize)
            {
                float xFactor = size.x > 0.0001f ? targetSize / size.x : 1.0f;
                float yFactor = size.y > 0.0001f ? targetSize / size.y : 1.0f;
                float zFactor = size.z > 0.0001f ? targetSize / size.z : 1.0f;
                _scaleFactor = Mathf.Min(1.0f, Mathf.Min(xFactor, Mathf.Min(yFactor, zFactor)));
                _storedLocalScale = _originalLocalScale * _scaleFactor;
            }

            if (Plugin.DeveloperDiagnosticsEnabled)
            {
                Plugin.Logger.LogInfo(string.Format(
                    "Stored-object fit for {0}: bounds=({1:0.######}, {2:0.######}, {3:0.######}) m, target={4} mm, scale factor={5:0.####}, local scale {6} -> {7}; parent={8}; parentActive={9}.",
                    _trackedObject.name,
                    _measuredBoundsSize.x,
                    _measuredBoundsSize.y,
                    _measuredBoundsSize.z,
                    _targetSizeMillimeters,
                    _scaleFactor,
                    _originalLocalScale,
                    _storedLocalScale,
                    _trackedObject.transform.parent != null ? _trackedObject.transform.parent.name : "null",
                    _trackedObject.transform.parent != null && _trackedObject.transform.parent.gameObject.activeInHierarchy));
            }
        }

        private void HandleHierarchyChanges()
        {
            if (_trackedObject == null)
            {
                return;
            }

            Transform[] transforms = _trackedObject.GetComponentsInChildren<Transform>(true);
            if (transforms.Length == _knownTransformCount)
            {
                return;
            }

            RestoreRootScale();
            FVRPhysicalObject[] physicalObjects = _trackedObject.GetComponentsInChildren<FVRPhysicalObject>(true);
            for (int index = 0; index < physicalObjects.Length; index++)
            {
                FVRPhysicalObject physicalObject = physicalObjects[index];
                if (physicalObject == null || physicalObject == _trackedObject || _knownPhysicalObjects.Contains(physicalObject.gameObject))
                {
                    continue;
                }

                // Unity preserves an attachment's world scale when it is parented below
                // a miniaturized object. Normalize that compensating local scale before
                // fitting the combined object again, matching SpineHero's attachment fix.
                physicalObject.transform.localScale *= _scaleFactor;
            }

            RecordHierarchy();
            CalculateStoredScale(_targetSizeMillimeters);
        }

        private void RecordHierarchy()
        {
            _knownPhysicalObjects.Clear();
            if (_trackedObject == null)
            {
                _knownTransformCount = 0;
                return;
            }

            Transform[] transforms = _trackedObject.GetComponentsInChildren<Transform>(true);
            _knownTransformCount = transforms.Length;

            FVRPhysicalObject[] physicalObjects = _trackedObject.GetComponentsInChildren<FVRPhysicalObject>(true);
            for (int index = 0; index < physicalObjects.Length; index++)
            {
                if (physicalObjects[index] != null)
                {
                    _knownPhysicalObjects.Add(physicalObjects[index].gameObject);
                }
            }
        }

        private void RestoreRootScale()
        {
            if (_trackedObject != null)
            {
                _trackedObject.transform.localScale = _originalLocalScale;
            }
        }

        private void ApplyStoredScale()
        {
            if (_trackedObject != null && !_trackedObject.IsHeld)
            {
                _trackedObject.transform.localScale = _storedLocalScale;
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));
            bool hasBounds = false;

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return bounds;
        }
    }
}
