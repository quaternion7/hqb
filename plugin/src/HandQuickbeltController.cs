using System.Collections.Generic;
using System.Text;
using FistVR;
using UnityEngine;

namespace HandQuickbelts
{
    internal sealed class HandQuickbeltController
    {
        private readonly List<FVRQuickBeltSlot> _slots = new List<FVRQuickBeltSlot>();
        private readonly List<FVRQuickBeltSlot> _leftSlots = new List<FVRQuickBeltSlot>();
        private readonly List<FVRQuickBeltSlot> _rightSlots = new List<FVRQuickBeltSlot>();
        private FVRPlayerBody _body;
        private GameObject _leftRoot;
        private GameObject _rightRoot;
        private HandQuickbeltAdjustmentHandle _leftAdjustmentHandle;
        private HandQuickbeltAdjustmentHandle _rightAdjustmentHandle;
        private bool _rebuildRequested;
        private bool _layoutRefreshRequested;
        private bool _templateWarningLogged;
        private float _nextTemplateLookupTime;

        internal void Update()
        {
            FVRPlayerBody currentBody = GM.CurrentPlayerBody;
            if (currentBody == null || currentBody.LeftHand == null || currentBody.RightHand == null)
            {
                return;
            }

            if (_body != currentBody || _leftRoot == null || _rightRoot == null)
            {
                Rebuild(currentBody, false);
                return;
            }

            if (_rebuildRequested)
            {
                _rebuildRequested = false;
                _layoutRefreshRequested = false;
                Rebuild(currentBody, true);
                return;
            }

            if (_layoutRefreshRequested)
            {
                _layoutRefreshRequested = false;
                ApplyLayout();
            }
        }

        internal void OnQuickbeltConfigured(FVRPlayerBody body)
        {
            Rebuild(body, false);
        }

        internal void RequestRebuild()
        {
            _rebuildRequested = true;
        }

        internal void RequestLayoutRefresh()
        {
            _layoutRefreshRequested = true;
        }

        internal void Dispose()
        {
            RemoveSlots(true);
            _body = null;
        }

        private void Rebuild(FVRPlayerBody body, bool dropContents)
        {
            if (body == null || body.LeftHand == null || body.RightHand == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextTemplateLookupTime)
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
                    Plugin.Logger.LogWarning("Native quickbelt prefabs are not ready yet; HQB will retry once per second.");
                }
                return;
            }

            _nextTemplateLookupTime = 0.0f;
            _templateWarningLogged = false;

            RemoveSlots(dropContents);
            _body = body;

            _leftRoot = CreateHandRoot("HQB_LeftHand", GetPalmAnchor(body.LeftHand));
            _rightRoot = CreateHandRoot("HQB_RightHand", GetPalmAnchor(body.RightHand));
            _leftAdjustmentHandle = CreateAdjustmentHandle(_leftRoot.transform, true);
            _rightAdjustmentHandle = CreateAdjustmentHandle(_rightRoot.transform, false);

            CreateHandSlots(_leftRoot.transform, _leftSlots, Plugin.LeftLargeSlots.Value, Plugin.LeftMediumSlots.Value, Plugin.LeftSmallSlots.Value, template);
            CreateHandSlots(_rightRoot.transform, _rightSlots, Plugin.RightLargeSlots.Value, Plugin.RightMediumSlots.Value, Plugin.RightSmallSlots.Value, template);

            ApplyLayout();
            if (Plugin.DeveloperDiagnosticsEnabled)
            {
                LogGeometryValidation();
            }
            Plugin.Logger.LogInfo(string.Format("Created {0} hand quickbelt slots using palm-space anchors.", _slots.Count));
        }

        private static Transform GetPalmAnchor(Transform hand)
        {
            FVRViveHand viveHand = hand.GetComponent<FVRViveHand>();
            return viveHand != null && viveHand.PalmTransform != null ? viveHand.PalmTransform : hand;
        }

        private static GameObject CreateHandRoot(string name, Transform anchor)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(anchor, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            return root;
        }

        private static HandQuickbeltAdjustmentHandle CreateAdjustmentHandle(Transform root, bool isLeftHand)
        {
            GameObject handleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handleObject.name = isLeftHand ? "HQB_LeftAnchorHandle" : "HQB_RightAnchorHandle";
            handleObject.transform.SetParent(root, false);
            handleObject.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            handleObject.transform.localRotation = Quaternion.identity;
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

        private void CreateHandSlots(
            Transform root,
            List<FVRQuickBeltSlot> handSlots,
            int largeCount,
            int mediumCount,
            int smallCount,
            FVRQuickBeltSlot template)
        {
            List<FVRPhysicalObject.FVRPhysicalObjectSize> sizes = new List<FVRPhysicalObject.FVRPhysicalObjectSize>();
            AddSizes(sizes, FVRPhysicalObject.FVRPhysicalObjectSize.Large, largeCount);
            AddSizes(sizes, FVRPhysicalObject.FVRPhysicalObjectSize.Medium, mediumCount);
            AddSizes(sizes, FVRPhysicalObject.FVRPhysicalObjectSize.Small, smallCount);

            for (int index = 0; index < sizes.Count; index++)
            {
                FVRQuickBeltSlot slot = CreateSlot(root, sizes[index], index, template);
                if (slot != null)
                {
                    handSlots.Add(slot);
                }
            }
        }

        private FVRQuickBeltSlot CreateSlot(
            Transform root,
            FVRPhysicalObject.FVRPhysicalObjectSize size,
            int index,
            FVRQuickBeltSlot template)
        {
            if (template == null)
            {
                return null;
            }

            GameObject slotObject = UnityEngine.Object.Instantiate(template.gameObject);
            slotObject.SetActive(false);
            slotObject.name = string.Format("HQB_{0}_{1:00}", size, index + 1);
            slotObject.transform.SetParent(root, false);
            slotObject.transform.localPosition = Vector3.zero;
            slotObject.transform.localRotation = Quaternion.identity;
            slotObject.transform.localScale = Vector3.one;

            FVRQuickBeltSlot slot = slotObject.GetComponent<FVRQuickBeltSlot>();
            slot.SizeLimit = size;
            slot.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
            slot.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
            slot.IsPlayer = true;
            slot.IsSelectable = true;
            slot.CurObject = null;
            slot.HeldObject = null;
            slot.IsKeepingTrackWithHead = false;

            ConfigureSelfContainedGeometry(slot, template);
            slot.IsHovered = false;
            slotObject.AddComponent<HandQuickbeltSingleVisual>().Configure(slot);
            slotObject.AddComponent<HandQuickbeltStoredObjectScaler>();
            slotObject.SetActive(true);
            _body.QBSlots_Internal.Add(slot);
            _slots.Add(slot);
            return slot;
        }

        private static void ConfigureSelfContainedGeometry(FVRQuickBeltSlot slot, FVRQuickBeltSlot template)
        {
            if (slot == null || template == null || template.HoverGeo == null)
            {
                Plugin.Logger.LogError(string.Format(
                    "Cannot create self-contained geometry for slot {0}: the native template has no HoverGeo.",
                    slot != null ? slot.name : "null"));
                return;
            }

            // A live quickbelt configuration can keep QuickbeltRoot, HoverGeo,
            // and PoseOverride outside the GameObject carrying FVRQuickBeltSlot.
            // Instantiating only template.gameObject then leaves those fields
            // pointing into the original, inactive configuration. Build and
            // bind a complete local hierarchy instead of retaining any of those
            // external references.
            Renderer[] inheritedRenderers = slot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < inheritedRenderers.Length; index++)
            {
                if (inheritedRenderers[index] != null)
                {
                    inheritedRenderers[index].enabled = false;
                }
            }

            GameObject quickbeltRootObject = new GameObject("HQB_QuickbeltRoot");
            quickbeltRootObject.transform.SetParent(slot.transform, false);
            quickbeltRootObject.transform.localPosition = Vector3.zero;
            quickbeltRootObject.transform.localRotation = Quaternion.identity;
            quickbeltRootObject.transform.localScale = Vector3.one;

            GameObject hoverObject = UnityEngine.Object.Instantiate(template.HoverGeo);
            hoverObject.name = "HQB_HoverGeo";
            hoverObject.transform.SetParent(quickbeltRootObject.transform, false);
            hoverObject.transform.localPosition = Vector3.zero;
            hoverObject.transform.localRotation = Quaternion.identity;

            GameObject poseObject = new GameObject("HQB_PoseOverride");
            poseObject.transform.SetParent(quickbeltRootObject.transform, false);
            poseObject.transform.localPosition = Vector3.zero;
            poseObject.transform.localRotation = Quaternion.identity;

            Transform templateGeometryRoot = template.QuickbeltRoot != null
                ? template.QuickbeltRoot
                : template.HoverGeo.transform.parent;
            Transform nativeBaseTemplate = FindNativeBaseSphere(template, templateGeometryRoot);
            GameObject baseObject = nativeBaseTemplate != null
                ? UnityEngine.Object.Instantiate(nativeBaseTemplate.gameObject)
                : UnityEngine.Object.Instantiate(template.HoverGeo);
            baseObject.name = "HQB_BaseSphere";
            baseObject.transform.SetParent(quickbeltRootObject.transform, false);
            baseObject.transform.localPosition = Vector3.zero;
            baseObject.transform.localRotation = Quaternion.identity;
            baseObject.SetActive(true);

            if (nativeBaseTemplate == null)
            {
                Plugin.Logger.LogWarning(string.Format(
                    "Slot {0} had no native constant sphere; using a HoverGeo clone as the base visual.",
                    slot.name));
            }

            slot.QuickbeltRoot = quickbeltRootObject.transform;
            slot.HoverGeo = hoverObject;
            slot.PoseOverride = poseObject.transform;
            slot.RectBounds = baseObject.transform;

            Renderer hoverRenderer = FindRenderer(hoverObject.transform);
            if (hoverRenderer == null)
            {
                Plugin.Logger.LogError(string.Format("The cloned HoverGeo for {0} has no renderer.", slot.name));
                slot.IsSelectable = false;
                return;
            }

            // FVRQuickBeltSlot.Update changes this material even when the native
            // renderer is inactive, so its cached reference must follow HoverGeo.
            slot.m_hoverGeoRend = hoverRenderer;
            hoverRenderer.enabled = true;
            Renderer baseRenderer = FindRenderer(baseObject.transform);
            if (baseRenderer != null)
            {
                baseRenderer.enabled = true;
            }
            hoverObject.SetActive(false);
            slot.SetBaseRotation(Quaternion.identity);

            ApplySlotGeometryScales(slot);

            if (Plugin.DeveloperDiagnosticsEnabled)
            {
                bool rootOwned = slot.QuickbeltRoot.IsChildOf(slot.transform);
                bool hoverOwned = slot.HoverGeo.transform.IsChildOf(slot.transform);
                bool poseOwned = slot.PoseOverride.IsChildOf(slot.transform);
                Plugin.Logger.LogInfo(string.Format(
                    "Slot ownership {0}: QuickbeltRoot child={1} active={2}; HoverGeo child={3}; PoseOverride child={4}.",
                    slot.name,
                    rootOwned,
                    slot.QuickbeltRoot.gameObject.activeInHierarchy,
                    hoverOwned,
                    poseOwned));
            }
        }

        private static Transform FindNativeBaseSphere(FVRQuickBeltSlot template, Transform geometryRoot)
        {
            if (template == null || template.HoverGeo == null || geometryRoot == null)
            {
                return null;
            }

            Renderer hoverRenderer = FindRenderer(template.HoverGeo.transform);
            MeshFilter hoverMeshFilter = hoverRenderer != null ? hoverRenderer.GetComponent<MeshFilter>() : null;
            Renderer fallback = null;
            Renderer[] renderers = geometryRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || renderer == hoverRenderer || renderer.transform.IsChildOf(template.HoverGeo.transform))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = renderer;
                }

                MeshFilter candidateMeshFilter = renderer.GetComponent<MeshFilter>();
                if (hoverMeshFilter != null && candidateMeshFilter != null && candidateMeshFilter.sharedMesh == hoverMeshFilter.sharedMesh)
                {
                    return renderer.transform;
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

            ApplyRootTransform(
                _leftRoot.transform,
                Plugin.MirrorPositionAcrossX(Plugin.AnchorPosition.Value),
                Plugin.MirrorRotationAcrossX(Quaternion.Euler(Plugin.AnchorRotation.Value)));
            ApplyRootTransform(_rightRoot.transform, Plugin.AnchorPosition.Value, Plugin.AnchorRotation.Value);

            ArrangeSlots(_leftSlots);
            ArrangeSlots(_rightSlots);
            SetAdjustmentHandleState(_leftAdjustmentHandle);
            SetAdjustmentHandleState(_rightAdjustmentHandle);
        }

        private static void SetAdjustmentHandleState(HandQuickbeltAdjustmentHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            bool enabled = Plugin.EnableAdjustmentHandles.Value;
            if (!enabled && handle.IsHeld)
            {
                handle.ForceBreakInteraction();
            }

            if (handle.gameObject.activeSelf != enabled)
            {
                handle.gameObject.SetActive(enabled);
            }
        }

        private static void ApplyRootTransform(Transform root, Vector3 position, Quaternion rotation)
        {
            root.localPosition = position;
            root.localRotation = rotation;
        }

        private static void ApplyRootTransform(Transform root, Vector3 position, Vector3 rotation)
        {
            ApplyRootTransform(root, position, Quaternion.Euler(rotation));
        }

        private static void ArrangeSlots(List<FVRQuickBeltSlot> slots)
        {
            int columns = Mathf.Max(1, Plugin.LayoutColumns.Value);
            for (int index = 0; index < slots.Count; index++)
            {
                FVRQuickBeltSlot slot = slots[index];
                if (slot == null)
                {
                    continue;
                }

                int row = index / columns;
                int column = index % columns;
                int columnsInThisRow = Mathf.Min(columns, slots.Count - row * columns);
                float x = (column - (columnsInThisRow - 1) * 0.5f) * MillimetersToMeters(Plugin.ColumnSpacingMillimeters.Value);
                float z = -row * MillimetersToMeters(Plugin.RowSpacingMillimeters.Value);
                Transform parent = slot.transform.parent;
                if (parent != null)
                {
                    // The tracked palm hierarchy uses a 0.1 scale. Configured
                    // spacing is a world-space measurement, so convert the
                    // desired offset back into this parent's local space.
                    Vector3 worldOffset = parent.right * x + parent.forward * z;
                    slot.transform.localPosition = parent.InverseTransformVector(worldOffset);
                }
                else
                {
                    slot.transform.localPosition = new Vector3(x, 0f, z);
                }

                ApplySlotGeometryScales(slot);
            }
        }

        private static void ApplySlotGeometryScales(FVRQuickBeltSlot slot)
        {
            float diameter = SlotDiameterMeters();
            if (slot.HoverGeo != null)
            {
                SetWorldUnitSphereDiameter(slot.HoverGeo.transform, diameter);
            }

            if (slot.RectBounds != null)
            {
                // Use the exact same center, orientation, and compensated world
                // scale as HoverGeo. This makes the displayed base sphere,
                // highlighted sphere, and H3VR hit volume one coincident volume.
                if (slot.HoverGeo != null)
                {
                    slot.RectBounds.position = slot.HoverGeo.transform.position;
                    slot.RectBounds.rotation = slot.HoverGeo.transform.rotation;
                    slot.RectBounds.localScale = slot.HoverGeo.transform.localScale;
                }
                else
                {
                    SetWorldUnitSphereDiameter(slot.RectBounds, diameter);
                }
            }
        }

        private static void SetWorldUnitSphereDiameter(Transform sphere, float diameter)
        {
            Vector3 parentScale = sphere.parent != null ? sphere.parent.lossyScale : Vector3.one;
            sphere.localScale = new Vector3(
                diameter / NonZeroAbsolute(parentScale.x),
                diameter / NonZeroAbsolute(parentScale.y),
                diameter / NonZeroAbsolute(parentScale.z));
        }

        private static float NonZeroAbsolute(float value)
        {
            return Mathf.Max(0.0001f, Mathf.Abs(value));
        }

        private static float SlotDiameterMeters()
        {
            return Mathf.Max(0.001f, Mathf.Abs(MillimetersToMeters(Plugin.SlotDiameterMillimeters.Value)));
        }

        private static float MillimetersToMeters(int millimeters)
        {
            return millimeters * 0.001f;
        }

        internal void DumpDiagnostics()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("===== Hand Quickbelts live diagnostics =====");
            report.AppendFormat("Body: {0}; slots tracked: {1}; QBSlots_Internal: {2}\n", ObjectName(_body), _slots.Count, _body != null && _body.QBSlots_Internal != null ? _body.QBSlots_Internal.Count : -1);
            report.AppendFormat(
                "Configured native slot diameter: {0} mm; miniaturize stored objects: {1}; target size: {2} mm\n",
                Plugin.SlotDiameterMillimeters.Value,
                Plugin.MiniaturizeStoredObjects.Value,
                Plugin.StoredObjectTargetSizeMillimeters.Value);

            for (int index = 0; index < _slots.Count; index++)
            {
                FVRQuickBeltSlot slot = _slots[index];
                if (slot == null)
                {
                    report.AppendFormat("Slot {0}: destroyed\n", index);
                    continue;
                }

                bool registered = _body != null && _body.QBSlots_Internal != null && _body.QBSlots_Internal.Contains(slot);
                report.AppendFormat(
                    "Slot {0}: {1}; registered={2}; selectable={3}; shape={4}; type={5}; size={6}; held={7}\n",
                    index,
                    slot.name,
                    registered,
                    slot.IsSelectable,
                    slot.Shape,
                    slot.Type,
                    slot.SizeLimit,
                    ObjectName(slot.HeldObject));
                report.AppendFormat("  QuickbeltRoot: {0}\n", TransformSummary(slot.QuickbeltRoot));
                report.AppendFormat("  PoseOverride: {0}\n", TransformSummary(slot.PoseOverride));
                report.AppendFormat("  HoverGeo: {0}\n", slot.HoverGeo != null ? TransformSummary(slot.HoverGeo.transform) : "null");
                report.AppendFormat("  Hover renderer: {0}\n", slot.m_hoverGeoRend != null ? TransformSummary(slot.m_hoverGeoRend.transform) : "null");
                report.AppendFormat("  RectBounds/base marker: {0}\n", TransformSummary(slot.RectBounds));
                report.AppendFormat(
                    "  Geometry world centers: hover={0}; base={1}; delta={2}; requestedDiameter={3:0.###} m\n",
                    slot.HoverGeo != null ? slot.HoverGeo.transform.position.ToString() : "null",
                    slot.RectBounds != null ? slot.RectBounds.position.ToString() : "null",
                    slot.HoverGeo != null && slot.RectBounds != null ? Vector3.Distance(slot.HoverGeo.transform.position, slot.RectBounds.position).ToString("0.######") : "n/a",
                    SlotDiameterMeters());
                report.AppendFormat(
                    "  Renderer world bounds: hover={0}; base={1}\n",
                    RendererBoundsSummary(slot.m_hoverGeoRend),
                    RendererBoundsSummary(slot.RectBounds != null ? slot.RectBounds.GetComponent<Renderer>() : null));
                HandQuickbeltStoredObjectScaler scaler = slot.GetComponent<HandQuickbeltStoredObjectScaler>();
                report.AppendFormat("  Stored-object scaler: {0}\n", scaler != null ? scaler.DiagnosticSummary : "missing");
                HandQuickbeltSingleVisual visual = slot.GetComponent<HandQuickbeltSingleVisual>();
                report.AppendFormat("  Visible indicator: {0}\n", visual != null ? visual.DiagnosticSummary : "missing");
                AppendHierarchy(report, slot.transform, 1);
            }

            report.AppendLine("===== End Hand Quickbelts live diagnostics =====");
            Plugin.Logger.LogInfo(report.ToString());
        }

        private void LogGeometryValidation()
        {
            for (int index = 0; index < _slots.Count; index++)
            {
                FVRQuickBeltSlot slot = _slots[index];
                if (slot == null || slot.HoverGeo == null || slot.RectBounds == null)
                {
                    continue;
                }

                Renderer baseRenderer = slot.RectBounds.GetComponent<Renderer>();
                HandQuickbeltSingleVisual visual = slot.GetComponent<HandQuickbeltSingleVisual>();
                Plugin.Logger.LogInfo(string.Format(
                    "Geometry check {0}: hitCenter={1}, visualCenter={2}, centerDelta={3:0.######} m, hitWorldScale={4}, nativeBounds={5}, indicator={6}.",
                    slot.name,
                    slot.HoverGeo.transform.position,
                    slot.RectBounds.position,
                    Vector3.Distance(slot.HoverGeo.transform.position, slot.RectBounds.position),
                    slot.HoverGeo.transform.lossyScale,
                    RendererBoundsSummary(baseRenderer),
                    visual != null ? visual.DiagnosticSummary : "missing"));
            }
        }

        private static void AppendHierarchy(StringBuilder report, Transform transform, int depth)
        {
            if (transform == null)
            {
                return;
            }

            report.Append(' ', depth * 2);
            report.Append("- ");
            report.Append(TransformSummary(transform));
            report.Append(" components=[");
            Component[] components = transform.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                if (index > 0)
                {
                    report.Append(", ");
                }
                report.Append(components[index] != null ? components[index].GetType().FullName : "destroyed");
            }
            report.AppendLine("]");

            for (int index = 0; index < transform.childCount; index++)
            {
                AppendHierarchy(report, transform.GetChild(index), depth + 1);
            }
        }

        private static string TransformSummary(Transform transform)
        {
            if (transform == null)
            {
                return "null";
            }

            return string.Format(
                "{0} active={1} localPos={2} localRot={3} localScale={4} lossyScale={5}",
                transform.name,
                transform.gameObject.activeInHierarchy,
                transform.localPosition,
                transform.localEulerAngles,
                transform.localScale,
                transform.lossyScale);
        }

        private static string ObjectName(UnityEngine.Object value)
        {
            return value != null ? value.name : "null";
        }

        private static string RendererBoundsSummary(Renderer renderer)
        {
            if (renderer == null)
            {
                return "null";
            }

            return string.Format("center={0} size={1} enabled={2}", renderer.bounds.center, renderer.bounds.size, renderer.enabled);
        }

        private FVRQuickBeltSlot FindTemplate(FVRPlayerBody body)
        {
            FVRQuickBeltSlot fallback = null;

            // Prefer a spherical slot from the player's active quickbelt so the
            // clones match the preset the user is actually seeing, including
            // any custom material overrides. Stock Harness and regular slots
            // both use QuickSlotGlow/QuickSlotGlowConstant.
            if (body != null && body.QBSlots_Internal != null)
            {
                for (int index = 0; index < body.QBSlots_Internal.Count; index++)
                {
                    FVRQuickBeltSlot activeSlot = body.QBSlots_Internal[index];
                    if (activeSlot == null
                        || activeSlot.GetComponent<HandQuickbeltSingleVisual>() != null
                        || activeSlot.Shape != FVRQuickBeltSlot.QuickbeltSlotShape.Sphere
                        || activeSlot.HoverGeo == null)
                    {
                        continue;
                    }

                    Transform geometryRoot = activeSlot.QuickbeltRoot != null
                        ? activeSlot.QuickbeltRoot
                        : activeSlot.HoverGeo.transform.parent;
                    if (FindNativeBaseSphere(activeSlot, geometryRoot) == null)
                    {
                        continue;
                    }

                    Plugin.Logger.LogInfo(string.Format(
                        "Using active quickbelt slot {0} as the HQB visual template.",
                        activeSlot.name));
                    return activeSlot;
                }
            }

            if (ManagerSingleton<GM>.Instance == null || ManagerSingleton<GM>.Instance.QuickbeltConfigurations == null)
            {
                return null;
            }

            foreach (GameObject configuration in ManagerSingleton<GM>.Instance.QuickbeltConfigurations)
            {
                if (configuration == null)
                {
                    continue;
                }

                FVRQuickBeltSlot[] candidates = configuration.GetComponentsInChildren<FVRQuickBeltSlot>(true);
                foreach (FVRQuickBeltSlot candidate in candidates)
                {
                    if (candidate == null || candidate.HoverGeo == null)
                    {
                        continue;
                    }

                    if (fallback == null)
                    {
                        fallback = candidate;
                    }

                    // The vanilla harness slot is already a 20 cm coincident
                    // sphere pair, making it the cleanest prefab for every HQB
                    // size category. SizeLimit is assigned after cloning.
                    if (candidate.name == "QuickBeltSlot_Harness")
                    {
                        Plugin.Logger.LogInfo("Using vanilla QuickBeltSlot_Harness as the HQB slot prefab.");
                        return candidate;
                    }
                }
            }

            if (fallback != null)
            {
                Plugin.Logger.LogWarning(string.Format("QuickBeltSlot_Harness was unavailable; using native fallback prefab {0}.", fallback.name));
            }

            return fallback;
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
                HandQuickbeltStoredObjectScaler scaler = slot.GetComponent<HandQuickbeltStoredObjectScaler>();
                if (scaler != null)
                {
                    scaler.RestoreNow();
                }

                if (dropContents && slot.CurObject != null)
                {
                    slot.CurObject.ClearQuickbeltState();
                }

                if (_body != null && _body.QBSlots_Internal != null)
                {
                    _body.QBSlots_Internal.Remove(slot);
                }

                UnityEngine.Object.Destroy(slot.gameObject);
            }

            _slots.Clear();
            _leftSlots.Clear();
            _rightSlots.Clear();
            _leftAdjustmentHandle = null;
            _rightAdjustmentHandle = null;
            DestroyRoot(ref _leftRoot);
            DestroyRoot(ref _rightRoot);
        }

        private static void DestroyRoot(ref GameObject root)
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                root = null;
            }
        }

        private static void AddSizes(List<FVRPhysicalObject.FVRPhysicalObjectSize> sizes, FVRPhysicalObject.FVRPhysicalObjectSize size, int count)
        {
            for (int index = 0; index < count; index++)
            {
                sizes.Add(size);
            }
        }
    }
}
