using System.Collections.Generic;
using FistVR;
using UnityEngine;

namespace HandQuickbelts
{
    internal sealed class HandQuickbeltController
    {
        private const float MillimetersToMeters = 0.001f;

        private readonly List<FVRQuickBeltSlot> _slots = new List<FVRQuickBeltSlot>();
        private readonly List<FVRQuickBeltSlot> _leftSlots = new List<FVRQuickBeltSlot>();
        private readonly List<FVRQuickBeltSlot> _rightSlots = new List<FVRQuickBeltSlot>();
        private FVRPlayerBody _body;
        private GameObject _leftRoot;
        private GameObject _rightRoot;
        private HandQuickbeltAdjustmentHandle _leftHandle;
        private HandQuickbeltAdjustmentHandle _rightHandle;
        private bool _rebuildRequested;
        private bool _layoutRequested;
        private bool _templateWarningLogged;
        private float _nextTemplateLookupTime;

        internal void Tick()
        {
            FVRPlayerBody body = GM.CurrentPlayerBody;
            if (body == null || body.LeftHand == null || body.RightHand == null)
            {
                return;
            }

            if (_body != body || _leftRoot == null || _rightRoot == null)
            {
                Rebuild(body, false);
            }
            else if (_rebuildRequested)
            {
                _rebuildRequested = false;
                _layoutRequested = false;
                Rebuild(body, true);
            }
            else if (_layoutRequested)
            {
                _layoutRequested = false;
                ApplyLayout();
            }
        }

        internal void RebuildAfterQuickbeltChange(FVRPlayerBody body)
        {
            Rebuild(body, false);
        }

        internal void RequestRebuild()
        {
            _rebuildRequested = true;
        }

        internal void RequestLayout()
        {
            _layoutRequested = true;
        }

        internal void Dispose()
        {
            RemoveSlots(true);
            _body = null;
        }

        private void Rebuild(FVRPlayerBody body, bool dropContents)
        {
            if (body == null || body.LeftHand == null || body.RightHand == null || Time.unscaledTime < _nextTemplateLookupTime)
            {
                return;
            }

            FVRQuickBeltSlot template = FindTemplate(body);
            if (template == null)
            {
                _nextTemplateLookupTime = Time.unscaledTime + 1.0f;
                if (!_templateWarningLogged)
                {
                    _templateWarningLogged = true;
                    Plugin.Logger.LogWarning("Native spherical quickbelt slots are not ready; retrying once per second.");
                }
                return;
            }

            _nextTemplateLookupTime = 0.0f;
            _templateWarningLogged = false;
            RemoveSlots(dropContents);
            _body = body;

            _leftRoot = CreateHandRoot("HQB_LeftHand", body.LeftHand);
            _rightRoot = CreateHandRoot("HQB_RightHand", body.RightHand);
            _leftHandle = CreateAdjustmentHandle(_leftRoot.transform, true);
            _rightHandle = CreateAdjustmentHandle(_rightRoot.transform, false);
            CreateHandSlots(_leftRoot.transform, _leftSlots, template, Plugin.LeftLargeSlots.Value, Plugin.LeftMediumSlots.Value, Plugin.LeftSmallSlots.Value);
            CreateHandSlots(_rightRoot.transform, _rightSlots, template, Plugin.RightLargeSlots.Value, Plugin.RightMediumSlots.Value, Plugin.RightSmallSlots.Value);
            ApplyLayout();
            Plugin.Logger.LogInfo(string.Format("Created {0} hand quickbelt slots.", _slots.Count));
        }

        private static GameObject CreateHandRoot(string name, Transform hand)
        {
            FVRViveHand viveHand = hand.GetComponent<FVRViveHand>();
            Transform palm = viveHand != null && viveHand.PalmTransform != null ? viveHand.PalmTransform : hand;
            GameObject root = new GameObject(name);
            root.transform.SetParent(palm, false);
            return root;
        }

        private static HandQuickbeltAdjustmentHandle CreateAdjustmentHandle(Transform root, bool isLeftHand)
        {
            GameObject handleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handleObject.name = isLeftHand ? "HQB_LeftAnchorHandle" : "HQB_RightAnchorHandle";
            handleObject.transform.SetParent(root, false);
            handleObject.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            handleObject.transform.localScale = Vector3.one * 0.035f;

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                handleObject.layer = interactableLayer;
            }

            Renderer renderer = handleObject.GetComponent<Renderer>();
            Shader shader = Shader.Find("Unlit/Color");
            if (renderer != null && shader != null)
            {
                Material material = new Material(shader);
                material.color = isLeftHand ? new Color(1f, 0.72f, 0.12f, 1f) : new Color(0.35f, 0.9f, 1f, 1f);
                renderer.material = material;
            }

            Rigidbody rigidbody = handleObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            HandQuickbeltAdjustmentHandle handle = handleObject.AddComponent<HandQuickbeltAdjustmentHandle>();
            handle.Configure(root, isLeftHand);
            handleObject.SetActive(false);
            return handle;
        }

        private void CreateHandSlots(Transform root, List<FVRQuickBeltSlot> handSlots, FVRQuickBeltSlot template, int large, int medium, int small)
        {
            FVRPhysicalObject.FVRPhysicalObjectSize[] sizes =
            {
                FVRPhysicalObject.FVRPhysicalObjectSize.Large,
                FVRPhysicalObject.FVRPhysicalObjectSize.Medium,
                FVRPhysicalObject.FVRPhysicalObjectSize.Small
            };
            int[] counts = { large, medium, small };
            int index = 0;
            for (int sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
            {
                for (int created = 0; created < counts[sizeIndex]; created++, index++)
                {
                    FVRQuickBeltSlot slot = CreateSlot(root, template, sizes[sizeIndex], index);
                    if (slot != null)
                    {
                        handSlots.Add(slot);
                    }
                }
            }
        }

        private FVRQuickBeltSlot CreateSlot(Transform root, FVRQuickBeltSlot template, FVRPhysicalObject.FVRPhysicalObjectSize size, int index)
        {
            GameObject slotObject = Object.Instantiate(template.gameObject);
            slotObject.SetActive(false);
            slotObject.name = string.Format("HQB_{0}_{1:00}", size, index + 1);
            slotObject.transform.SetParent(root, false);
            slotObject.transform.localPosition = Vector3.zero;
            slotObject.transform.localRotation = Quaternion.identity;
            slotObject.transform.localScale = Vector3.one;

            FVRQuickBeltSlot slot = slotObject.GetComponent<FVRQuickBeltSlot>();
            if (slot == null || !BuildLocalGeometry(slot, template))
            {
                Plugin.Logger.LogError(string.Format("Could not create {0} from template {1}.", slotObject.name, template.name));
                Object.Destroy(slotObject);
                return null;
            }

            slot.SizeLimit = size;
            slot.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
            slot.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
            slot.IsPlayer = true;
            slot.IsSelectable = true;
            slot.CurObject = null;
            slot.HeldObject = null;
            slot.IsKeepingTrackWithHead = false;
            slot.IsHovered = false;
            slotObject.AddComponent<HandQuickbeltSlotMarker>();
            slotObject.AddComponent<HandQuickbeltMiniaturizer>();
            slotObject.SetActive(true);
            _body.QBSlots_Internal.Add(slot);
            _slots.Add(slot);
            return slot;
        }

        private static bool BuildLocalGeometry(FVRQuickBeltSlot slot, FVRQuickBeltSlot template)
        {
            if (template == null || template.HoverGeo == null)
            {
                return false;
            }

            Renderer[] inheritedRenderers = slot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < inheritedRenderers.Length; index++)
            {
                inheritedRenderers[index].enabled = false;
            }
            for (int index = slot.transform.childCount - 1; index >= 0; index--)
            {
                Object.Destroy(slot.transform.GetChild(index).gameObject);
            }

            GameObject quickbeltRoot = new GameObject("HQB_QuickbeltRoot");
            quickbeltRoot.transform.SetParent(slot.transform, false);

            GameObject hover = Object.Instantiate(template.HoverGeo);
            hover.name = "HQB_HoverGeo";
            hover.transform.SetParent(quickbeltRoot.transform, false);
            hover.transform.localPosition = Vector3.zero;
            hover.transform.localRotation = Quaternion.identity;

            Transform templateGeometryRoot = template.QuickbeltRoot != null ? template.QuickbeltRoot : template.HoverGeo.transform.parent;
            Transform baseTemplate = FindBaseSphere(template, templateGeometryRoot);
            GameObject baseSphere = Object.Instantiate(baseTemplate != null ? baseTemplate.gameObject : template.HoverGeo);
            baseSphere.name = "HQB_BaseSphere";
            baseSphere.transform.SetParent(quickbeltRoot.transform, false);
            baseSphere.transform.localPosition = Vector3.zero;
            baseSphere.transform.localRotation = Quaternion.identity;
            baseSphere.SetActive(true);

            GameObject pose = new GameObject("HQB_PoseOverride");
            pose.transform.SetParent(quickbeltRoot.transform, false);

            Renderer hoverRenderer = FindRenderer(hover.transform);
            Renderer baseRenderer = FindRenderer(baseSphere.transform);
            if (hoverRenderer == null || baseRenderer == null)
            {
                Object.Destroy(quickbeltRoot);
                return false;
            }

            slot.QuickbeltRoot = quickbeltRoot.transform;
            slot.HoverGeo = hover;
            slot.PoseOverride = pose.transform;
            slot.RectBounds = baseSphere.transform;
            slot.m_hoverGeoRend = hoverRenderer;
            hoverRenderer.enabled = true;
            baseRenderer.enabled = true;
            hover.SetActive(false);
            slot.SetBaseRotation(Quaternion.identity);
            ApplyGeometryScale(slot);

            if (baseTemplate == null)
            {
                Plugin.Logger.LogWarning(string.Format("{0} uses a hover-material clone as its fallback base sphere.", slot.name));
            }
            return true;
        }

        private static Transform FindBaseSphere(FVRQuickBeltSlot template, Transform root)
        {
            if (template == null || template.HoverGeo == null || root == null)
            {
                return null;
            }

            Renderer hoverRenderer = FindRenderer(template.HoverGeo.transform);
            MeshFilter hoverMesh = hoverRenderer != null ? hoverRenderer.GetComponent<MeshFilter>() : null;
            Renderer fallback = null;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer candidate = renderers[index];
                if (candidate == null || candidate == hoverRenderer || candidate.transform.IsChildOf(template.HoverGeo.transform))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = candidate;
                }
                MeshFilter candidateMesh = candidate.GetComponent<MeshFilter>();
                if (hoverMesh != null && candidateMesh != null && candidateMesh.sharedMesh == hoverMesh.sharedMesh)
                {
                    return candidate.transform;
                }
            }
            return fallback != null ? fallback.transform : null;
        }

        private static Renderer FindRenderer(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            Renderer renderer = root.GetComponent<Renderer>();
            return renderer != null ? renderer : root.GetComponentInChildren<Renderer>(true);
        }

        private void ApplyLayout()
        {
            if (_leftRoot == null || _rightRoot == null)
            {
                return;
            }

            _leftRoot.transform.localPosition = Plugin.MirrorPosition(Plugin.AnchorPosition.Value);
            _leftRoot.transform.localRotation = Plugin.MirrorRotation(Quaternion.Euler(Plugin.AnchorRotation.Value));
            _rightRoot.transform.localPosition = Plugin.AnchorPosition.Value;
            _rightRoot.transform.localRotation = Quaternion.Euler(Plugin.AnchorRotation.Value);
            ArrangeSlots(_leftSlots);
            ArrangeSlots(_rightSlots);
            SetHandleVisible(_leftHandle);
            SetHandleVisible(_rightHandle);
        }

        private static void ArrangeSlots(List<FVRQuickBeltSlot> slots)
        {
            int columns = Mathf.Max(1, Plugin.GridColumns.Value);
            float spacing = Plugin.SlotSpacingMillimeters.Value * MillimetersToMeters;
            for (int index = 0; index < slots.Count; index++)
            {
                FVRQuickBeltSlot slot = slots[index];
                if (slot == null)
                {
                    continue;
                }

                int row = index / columns;
                int column = index % columns;
                int rowCount = Mathf.Min(columns, slots.Count - row * columns);
                float x = (column - (rowCount - 1) * 0.5f) * spacing;
                float z = -row * spacing;
                Transform parent = slot.transform.parent;
                slot.transform.localPosition = parent != null
                    ? parent.InverseTransformVector(parent.right * x + parent.forward * z)
                    : new Vector3(x, 0f, z);
                ApplyGeometryScale(slot);
            }
        }

        private static void SetHandleVisible(HandQuickbeltAdjustmentHandle handle)
        {
            if (handle == null)
            {
                return;
            }
            bool visible = Plugin.ShowAdjustmentHandles.Value;
            if (!visible && handle.IsHeld)
            {
                handle.ForceBreakInteraction();
            }
            if (handle.gameObject.activeSelf != visible)
            {
                handle.gameObject.SetActive(visible);
            }
        }

        private static void ApplyGeometryScale(FVRQuickBeltSlot slot)
        {
            float diameter = Mathf.Max(0.001f, Mathf.Abs(Plugin.SlotDiameterMillimeters.Value * MillimetersToMeters));
            if (slot.HoverGeo != null)
            {
                SetWorldDiameter(slot.HoverGeo.transform, diameter);
            }
            if (slot.RectBounds != null && slot.HoverGeo != null)
            {
                slot.RectBounds.position = slot.HoverGeo.transform.position;
                slot.RectBounds.rotation = slot.HoverGeo.transform.rotation;
                slot.RectBounds.localScale = slot.HoverGeo.transform.localScale;
            }
        }

        private static void SetWorldDiameter(Transform sphere, float diameter)
        {
            Vector3 scale = sphere.parent != null ? sphere.parent.lossyScale : Vector3.one;
            sphere.localScale = new Vector3(
                diameter / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                diameter / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                diameter / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }

        private FVRQuickBeltSlot FindTemplate(FVRPlayerBody body)
        {
            if (body != null && body.QBSlots_Internal != null)
            {
                for (int index = 0; index < body.QBSlots_Internal.Count; index++)
                {
                    FVRQuickBeltSlot candidate = body.QBSlots_Internal[index];
                    if (IsUsableTemplate(candidate) && candidate.GetComponent<HandQuickbeltSlotMarker>() == null)
                    {
                        return candidate;
                    }
                }
            }

            if (ManagerSingleton<GM>.Instance == null || ManagerSingleton<GM>.Instance.QuickbeltConfigurations == null)
            {
                return null;
            }

            FVRQuickBeltSlot fallback = null;
            foreach (GameObject configuration in ManagerSingleton<GM>.Instance.QuickbeltConfigurations)
            {
                if (configuration == null)
                {
                    continue;
                }
                FVRQuickBeltSlot[] candidates = configuration.GetComponentsInChildren<FVRQuickBeltSlot>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    FVRQuickBeltSlot candidate = candidates[index];
                    if (!IsUsableTemplate(candidate))
                    {
                        continue;
                    }
                    if (fallback == null)
                    {
                        fallback = candidate;
                    }
                    if (candidate.name == "QuickBeltSlot_Harness")
                    {
                        return candidate;
                    }
                }
            }
            return fallback;
        }

        private static bool IsUsableTemplate(FVRQuickBeltSlot slot)
        {
            if (slot == null || slot.Shape != FVRQuickBeltSlot.QuickbeltSlotShape.Sphere || slot.HoverGeo == null)
            {
                return false;
            }
            Transform root = slot.QuickbeltRoot != null ? slot.QuickbeltRoot : slot.HoverGeo.transform.parent;
            return FindBaseSphere(slot, root) != null;
        }

        private void RemoveSlots(bool dropContents)
        {
            for (int index = _slots.Count - 1; index >= 0; index--)
            {
                FVRQuickBeltSlot slot = _slots[index];
                if (slot == null)
                {
                    continue;
                }

                slot.IsSelectable = false;
                HandQuickbeltMiniaturizer miniaturizer = slot.GetComponent<HandQuickbeltMiniaturizer>();
                if (miniaturizer != null)
                {
                    miniaturizer.Restore();
                }
                if (dropContents && slot.CurObject != null)
                {
                    slot.CurObject.ClearQuickbeltState();
                }
                if (_body != null && _body.QBSlots_Internal != null)
                {
                    _body.QBSlots_Internal.Remove(slot);
                }
                Object.Destroy(slot.gameObject);
            }

            _slots.Clear();
            _leftSlots.Clear();
            _rightSlots.Clear();
            _leftHandle = null;
            _rightHandle = null;
            DestroyRoot(ref _leftRoot);
            DestroyRoot(ref _rightRoot);
        }

        private static void DestroyRoot(ref GameObject root)
        {
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }
    }

    internal sealed class HandQuickbeltSlotMarker : MonoBehaviour
    {
    }

    internal sealed class HandQuickbeltAdjustmentHandle : FVRInteractiveObject
    {
        private Transform _anchor;
        private bool _isLeftHand;
        private Vector3 _positionOffset;
        private Quaternion _rotationOffset;

        internal void Configure(Transform anchor, bool isLeftHand)
        {
            _anchor = anchor;
            _isLeftHand = isLeftHand;
            ControlType = FVRInteractionControlType.GrabHold;
            PoseOverride = transform;
            EndInteractionIfDistant = false;
        }

        public override bool IsDistantGrabbable()
        {
            return false;
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);
            if (_anchor != null)
            {
                Quaternion inverseHandRotation = Quaternion.Inverse(m_handRot);
                _positionOffset = inverseHandRotation * (_anchor.position - m_handPos);
                _rotationOffset = inverseHandRotation * _anchor.rotation;
            }
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            if (_anchor != null)
            {
                _anchor.position = m_handPos + m_handRot * _positionOffset;
                _anchor.rotation = m_handRot * _rotationOffset;
            }
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            if (_anchor != null && Plugin.Instance != null)
            {
                Plugin.Instance.SaveAnchorPose(_isLeftHand, _anchor.localPosition, _anchor.localRotation);
            }
            base.EndInteraction(hand);
        }
    }

    internal sealed class HandQuickbeltMiniaturizer : MonoBehaviour
    {
        private readonly HashSet<GameObject> _knownPhysicalObjects = new HashSet<GameObject>();
        private FVRQuickBeltSlot _slot;
        private FVRPhysicalObject _item;
        private Vector3 _originalScale;
        private Vector3 _storedScale;
        private float _scaleFactor = 1.0f;
        private float _targetSize;
        private int _knownTransformCount;

        private void Awake()
        {
            _slot = GetComponent<FVRQuickBeltSlot>();
        }

        private void Update()
        {
            FVRPhysicalObject current = Plugin.MiniaturizeStoredObjects.Value && _slot != null
                ? _slot.HeldObject as FVRPhysicalObject
                : null;
            if (current != _item)
            {
                Restore();
                if (current != null)
                {
                    Track(current);
                }
            }
            if (_item == null)
            {
                return;
            }

            float target = TargetSizeMeters();
            if (!Mathf.Approximately(target, _targetSize))
            {
                RestoreScale();
                Fit(target);
            }
            if (_item.IsHeld)
            {
                RestoreScale();
                return;
            }

            RefitAfterHierarchyChange();
            ApplyScale();
        }

        private void OnDestroy()
        {
            Restore();
        }

        internal void Restore()
        {
            RestoreScale();
            _item = null;
            _storedScale = Vector3.zero;
            _scaleFactor = 1.0f;
            _targetSize = 0.0f;
            _knownTransformCount = 0;
            _knownPhysicalObjects.Clear();
        }

        internal void RestoreBeforeRemoval(FVRPhysicalObject physicalObject)
        {
            if (_item == physicalObject)
            {
                Restore();
            }
        }

        private void Track(FVRPhysicalObject physicalObject)
        {
            _item = physicalObject;
            _originalScale = physicalObject.transform.localScale;
            RecordHierarchy();
            Fit(TargetSizeMeters());
        }

        private void Fit(float targetSize)
        {
            _targetSize = targetSize;
            _scaleFactor = 1.0f;
            _storedScale = _originalScale;

            Bounds bounds;
            if (!TryGetColliderBounds(_item.gameObject, out bounds))
            {
                return;
            }
            Vector3 size = bounds.size;
            float largestAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (largestAxis > targetSize)
            {
                _scaleFactor = targetSize / largestAxis;
                _storedScale = _originalScale * _scaleFactor;
            }
        }

        private void RefitAfterHierarchyChange()
        {
            Transform[] transforms = _item.GetComponentsInChildren<Transform>(true);
            if (transforms.Length == _knownTransformCount)
            {
                return;
            }

            RestoreScale();
            FVRPhysicalObject[] physicalObjects = _item.GetComponentsInChildren<FVRPhysicalObject>(true);
            for (int index = 0; index < physicalObjects.Length; index++)
            {
                FVRPhysicalObject physicalObject = physicalObjects[index];
                if (physicalObject != null && physicalObject != _item && !_knownPhysicalObjects.Contains(physicalObject.gameObject))
                {
                    physicalObject.transform.localScale *= _scaleFactor;
                }
            }
            RecordHierarchy();
            Fit(_targetSize);
        }

        private void RecordHierarchy()
        {
            _knownPhysicalObjects.Clear();
            _knownTransformCount = _item.GetComponentsInChildren<Transform>(true).Length;
            FVRPhysicalObject[] physicalObjects = _item.GetComponentsInChildren<FVRPhysicalObject>(true);
            for (int index = 0; index < physicalObjects.Length; index++)
            {
                if (physicalObjects[index] != null)
                {
                    _knownPhysicalObjects.Add(physicalObjects[index].gameObject);
                }
            }
        }

        private void RestoreScale()
        {
            if (_item != null)
            {
                _item.transform.localScale = _originalScale;
            }
        }

        private void ApplyScale()
        {
            if (_item != null && !_item.IsHeld)
            {
                _item.transform.localScale = _storedScale;
            }
        }

        private static float TargetSizeMeters()
        {
            float diameterMeters = Mathf.Abs(Plugin.SlotDiameterMillimeters.Value) * 0.001f;
            return Mathf.Max(0.001f, diameterMeters * Plugin.StoredSizePercent.Value * 0.01f);
        }

        private static bool TryGetColliderBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                {
                    continue;
                }
                if (!found)
                {
                    bounds = collider.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return found;
        }
    }
}
