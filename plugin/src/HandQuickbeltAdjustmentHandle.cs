using FistVR;
using UnityEngine;

namespace HandQuickbelts
{
    internal sealed class HandQuickbeltAdjustmentHandle : FVRInteractiveObject
    {
        private Transform _anchorRoot;
        private bool _isLeftHand;
        private Vector3 _positionOffset;
        private Quaternion _rotationOffset;

        internal void Configure(Transform anchorRoot, bool isLeftHand)
        {
            _anchorRoot = anchorRoot;
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
            if (_anchorRoot == null)
            {
                return;
            }

            Quaternion inverseHandRotation = Quaternion.Inverse(m_handRot);
            _positionOffset = inverseHandRotation * (_anchorRoot.position - m_handPos);
            _rotationOffset = inverseHandRotation * _anchorRoot.rotation;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            if (_anchorRoot == null)
            {
                return;
            }

            _anchorRoot.position = m_handPos + m_handRot * _positionOffset;
            _anchorRoot.rotation = m_handRot * _rotationOffset;
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            if (_anchorRoot != null && Plugin.Instance != null)
            {
                Plugin.Instance.SaveAnchorPose(_isLeftHand, _anchorRoot.localPosition, _anchorRoot.localRotation);
            }

            base.EndInteraction(hand);
        }
    }
}
