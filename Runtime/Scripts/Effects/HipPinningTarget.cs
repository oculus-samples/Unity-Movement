// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Specifies a target to be used for hip pinning.
    /// </summary>
    public class HipPinningTarget : MonoBehaviour
    {
        /// <summary>
        /// The hip pinning target transform that the hips will be constrained to.
        /// </summary>
        public Transform HipTargetTransform => _hipTargetTransform;

        /// <summary>
        /// The chair seat transform that the hip pinning target transform is parented to.
        /// </summary>
        public Transform ChairSeatTransform => _chairSeat;

        /// <summary>
        /// The spine target transforms that the spine will be constrained to.
        /// </summary>
        public List<Transform> SpineTargetTransforms => _spineTargetTransforms;

        /// <summary>
        /// Returns the initial local rotation offset of the hips target.
        /// </summary>
        public Quaternion HipTargetInitialRotationOffset => _hipTargetInitialRotationOffset;

        /// <summary>
        /// The chair object
        /// </summary>
        public GameObject ChairObject => _chairObject;

        /// <summary>
        /// The game object containing the renderers for this hip pinning target.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.ChairObject)]
        protected GameObject _chairObject;

        /// <summary>
        /// The transform that the character's hip is positionally constrained to.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.HipTargetTransform)]
        protected Transform _hipTargetTransform;

        /// <summary>
        /// The chair's seat transform.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.ChairSeat)]
        protected Transform _chairSeat;

        /// <summary>
        /// The chair's cylinder transform.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(HipPinningTargetTooltips.ChairCylinder)]
        protected Transform _chairCylinder;

        /// <summary>
        /// The chair's cylinder scale multiplier.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(HipPinningTargetTooltips.ChairCylinderScaleMultiplier)]
        protected float _chairCylinderScaleMultiplier = 1.0f;

        /// <summary>
        /// The transforms that the character's spine bones are positionally constrained to.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.SpineTargetTransforms)]
        protected List<Transform> _spineTargetTransforms;

        /// <summary>
        /// The amount of constrained movement allowed from this hip pinning target.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.ConstrainedArea)]
        protected Vector3 _constrainedArea;

        /// <summary>
        /// The offset to be applied to the center of where the constrained area will be.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningTargetTooltips.ConstrainedAreaOffset)]
        protected Vector3 _constrainedAreaOffset;

        private Vector3 _startPos;
        private Quaternion _hipTargetInitialRotationOffset;

        private void Start()
        {
            Assert.IsNotNull(_chairObject);
            Assert.IsNotNull(_spineTargetTransforms);
            Assert.IsNotNull(_hipTargetTransform);
            Assert.IsNotNull(_chairSeat);
            _hipTargetInitialRotationOffset = _hipTargetTransform.localRotation;
            CalibratePosition(transform.localPosition);
        }

        private void OnDrawGizmosSelected()
        {
            if (_hipTargetTransform != null)
            {
                Gizmos.DrawWireCube(_hipTargetTransform.position + _constrainedAreaOffset, _constrainedArea * 2);
            }
        }

        /// <summary>
        /// Update the height of the object that the hips will be constrained to.
        /// </summary>
        /// <param name="heightAdjustment">The height that this object will be adjusted to match.</param>
        public void UpdateHeight(float heightAdjustment)
        {
            _chairSeat.localPosition = Vector3.up * heightAdjustment;
            if (_chairCylinder)
            {
                Vector3 chairCylinderScale = _chairCylinder.localScale;
                _chairCylinder.localScale = new Vector3(chairCylinderScale.x,
                    1 - heightAdjustment * _chairCylinderScaleMultiplier, chairCylinderScale.z);
            }
        }

        /// <summary>
        /// Update the position of the hip pinning target transform with its constrained position.
        /// </summary>
        /// <param name="trackedHipTranslation">The tracked hip translation.</param>
        public void UpdateHipTargetTransform(Vector3 trackedHipTranslation)
        {
            _hipTargetTransform.position = GetConstrainedPosition(trackedHipTranslation);
        }

        /// <summary>
        /// Reset the local position of the hip pinning target transform to zero.
        /// </summary>
        public void ResetHipPinningTargetLocalPosition()
        {
            _hipTargetTransform.localPosition = Vector3.zero;
        }

        private void CalibratePosition(Vector3 newPosition)
        {
            var pinningTransform = _chairObject.transform;
            pinningTransform.localPosition = new Vector3(newPosition.x, pinningTransform.position.y, newPosition.z);
            _startPos = _hipTargetTransform.position;
        }

        private Vector3 GetConstrainedPosition(Vector3 position)
        {
            var constrainedPos = _startPos + _constrainedAreaOffset;
            return new Vector3(
                Mathf.Clamp(position.x, constrainedPos.x - _constrainedArea.x, constrainedPos.x + _constrainedArea.x),
                Mathf.Clamp(position.y, constrainedPos.y - _constrainedArea.y, constrainedPos.y + _constrainedArea.y),
                Mathf.Clamp(position.z, constrainedPos.z - _constrainedArea.z, constrainedPos.z + _constrainedArea.z));
        }
    }
}
