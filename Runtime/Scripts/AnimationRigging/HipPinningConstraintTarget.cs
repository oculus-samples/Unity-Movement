// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Specifies a target to be used for hip pinning.
    /// </summary>
    public class HipPinningConstraintTarget : MonoBehaviour
    {
        /// <inheritdoc cref="_hipTargetTransform"/>
        public Transform HipTargetTransform => _hipTargetTransform;

        /// <inheritdoc cref="_chairSeat"/>
        public Transform ChairSeatTransform => _chairSeat;

        /// <inheritdoc cref="_chairObject"/>
        public GameObject ChairObject => _chairObject;

        /// <summary>
        /// Returns the initial local rotation offset of the hips target.
        /// </summary>
        public Quaternion HipTargetInitialRotationOffset => _hipTargetInitialRotationOffset;

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

        private Quaternion _hipTargetInitialRotationOffset;

        private void Start()
        {
            Assert.IsNotNull(_chairObject);
            Assert.IsNotNull(_hipTargetTransform);
            Assert.IsNotNull(_chairSeat);

            CalibratePosition(transform.localPosition);
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

        private void CalibratePosition(Vector3 newPosition)
        {
            var pinningTransform = _chairObject.transform;
            pinningTransform.localPosition = new Vector3(newPosition.x, pinningTransform.position.y, newPosition.z);
            _hipTargetInitialRotationOffset = _hipTargetTransform.localRotation;
        }
    }
}
