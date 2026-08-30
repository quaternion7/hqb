using FistVR;
using UnityEngine;

namespace HandQuickbelts
{
    // Marker component for HQB slots and owner of the native visual references.
    // FVRQuickBeltSlot itself controls HoverGeo activation and its standard rim
    // colors for hover, spawnlock, and hardened states.
    internal sealed class HandQuickbeltSingleVisual : MonoBehaviour
    {
        private FVRQuickBeltSlot _slot;
        private Renderer _baseRenderer;
        private Renderer _hoverRenderer;
        private bool _lastHovered;
        private bool _postTrackingDiagnosticsLogged;
        private float _diagnosticsReadyTime;

        internal string DiagnosticSummary
        {
            get
            {
                return string.Format(
                    "native base={0}; native hover={1}",
                    RendererSummary(_baseRenderer),
                    RendererSummary(_hoverRenderer));
            }
        }

        internal void Configure(FVRQuickBeltSlot slot)
        {
            _slot = slot;
            _baseRenderer = FindRenderer(slot != null ? slot.RectBounds : null);
            _hoverRenderer = slot != null && slot.HoverGeo != null
                ? FindRenderer(slot.HoverGeo.transform)
                : null;

            if (_baseRenderer != null)
            {
                _baseRenderer.enabled = true;
            }

            if (_hoverRenderer != null)
            {
                _hoverRenderer.enabled = true;
                slot.m_hoverGeoRend = _hoverRenderer;
            }

            if (Plugin.DeveloperDiagnosticsEnabled)
            {
                _diagnosticsReadyTime = Time.unscaledTime + 3.0f;
            }
        }

        private void LateUpdate()
        {
            if (!Plugin.DeveloperDiagnosticsEnabled || _slot == null)
            {
                return;
            }

            if (_slot.IsHovered != _lastHovered)
            {
                _lastHovered = _slot.IsHovered;
                Plugin.Logger.LogInfo(string.Format("Slot {0} hover={1}.", _slot.name, _lastHovered));
            }

            if (!_postTrackingDiagnosticsLogged && Time.unscaledTime >= _diagnosticsReadyTime)
            {
                LogPostTrackingDiagnostics();
            }
        }

        private void LogPostTrackingDiagnostics()
        {
            if (_slot == null || _slot.HoverGeo == null || GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.Head == null)
            {
                return;
            }

            _postTrackingDiagnosticsLogged = true;
            Transform head = GM.CurrentPlayerBody.Head;
            Transform handRoot = _slot.transform.parent;
            Transform palm = handRoot != null ? handRoot.parent : null;
            Vector3 center = _slot.HoverGeo.transform.position;
            Vector3 scale = _slot.HoverGeo.transform.lossyScale;
            float diameter = Mathf.Min(Mathf.Abs(scale.x), Mathf.Min(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float headDistance = Vector3.Distance(head.position, center);
            float palmDistance = palm != null ? Vector3.Distance(palm.position, center) : -1.0f;
            float angularDiameter = headDistance > 0.0001f
                ? 2.0f * Mathf.Atan2(diameter * 0.5f, headDistance) * Mathf.Rad2Deg
                : 180.0f;

            Plugin.Logger.LogInfo(string.Format(
                "Post-tracking geometry {0}: center={1}; headDistance={2:0.####} m; palmDistance={3:0.####} m; diameter={4:0.####} m; angularDiameter={5:0.##} deg; visual={6}.",
                _slot.name,
                center,
                headDistance,
                palmDistance,
                diameter,
                angularDiameter,
                DiagnosticSummary));
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

        private static string RendererSummary(Renderer renderer)
        {
            if (renderer == null)
            {
                return "null";
            }

            return string.Format(
                "{0} active={1} enabled={2} bounds={3}",
                renderer.name,
                renderer.gameObject.activeInHierarchy,
                renderer.enabled,
                renderer.bounds.size);
        }
    }
}
