// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Two-bone IK solver - given a target joint, orient the upper/middle/lower
    /// joints to reach the target joint with the bend normal toward a pole transform.
    /// </summary>
    public class TwoBoneIK : MonoBehaviour
    {
        /// <summary>
        /// The upper bone transform.
        /// </summary>
        public Transform UpperTransform => _upperTransform;

        /// <summary>
        /// The middle bone transform.
        /// </summary>
        public Transform MiddleTransform => _middleTransform;

        /// <summary>
        /// The lower bone transform.
        /// </summary>
        public Transform LowerTransform => _lowerTransform;

        /// <summary>
        /// The target transform.
        /// </summary>
        public Transform TargetTransform => _targetTransform;

        /// <summary>
        /// The pole transform.
        /// </summary>
        public Transform PoleTransform => _poleTransform;

        /// <summary>
        /// The upper bone transform.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.UpperTransform)]
        protected Transform _upperTransform;

        /// <summary>
        /// The middle bone transform.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.MiddleTransform)]
        protected Transform _middleTransform;

        /// <summary>
        /// The lower bone transform.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.LowerTransform)]
        protected Transform _lowerTransform;

        /// <summary>
        /// The target transform.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.TargetTransform)]
        protected Transform _targetTransform;

        /// <summary>
        /// The pole transform.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.PoleTransform)]
        protected Transform _poleTransform;

        /// <summary>
        /// The target position offset.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.TargetPosOffset)]
        protected Vector3 _targetPosOffset = Vector3.zero;

        /// <summary>
        /// The target rotation offset.
        /// </summary>
        [SerializeField]
        [Tooltip(TwoBoneIKTooltips.TargetRotOffset)]
        protected Vector3 _targetRotOffset = Vector3.zero;

        private const float _epsilon = 0.001f;

        private void LateUpdate()
        {
            SolveTwoBoneIK();
        }

        private void SolveTwoBoneIK()
        {
            // 1. Determine mid angle such that distance between the upper and lower joints
            // matches the distance between the goal and upper joint transforms.
            var upperPos = _upperTransform.position;
            var middlePos = _middleTransform.position;
            var lowerPos = _lowerTransform.position;
            var targetPos = _targetTransform.position + _targetPosOffset;
            var targetRot = _targetTransform.rotation * Quaternion.Euler(_targetRotOffset);

            var upperToMid = middlePos - upperPos;
            var upperToLower = lowerPos - upperPos;
            var midToLower = lowerPos - middlePos;
            var upperToTarget = targetPos - upperPos;

            float upperLength = upperToMid.magnitude;
            float lowerLength = midToLower.magnitude;
            float upperLowerDist = upperToLower.magnitude;
            float upperTargetDist = upperToTarget.magnitude;

            float currentMiddleAngle = AngleOfTriangle(upperLength, lowerLength, upperLowerDist);
            float desiredMiddleAngle = AngleOfTriangle(upperLength, lowerLength, upperTargetDist);

            // If the upper/middle/lower joints are in the same direction,
            // use the pole transform as reference for the bend.
            var poleVector = Vector3.Cross(upperToMid, midToLower);
            if (poleVector.sqrMagnitude <= _epsilon)
            {
                poleVector = Vector3.Cross(_poleTransform.position - upperPos, midToLower);
            }
            poleVector = Vector3.Normalize(poleVector);

            // Determine rotation correction from the angle and pole vector.
            var rotationAmount = currentMiddleAngle - desiredMiddleAngle;
            var deltaRotation = Quaternion.AngleAxis(rotationAmount * 180.0f / Mathf.PI, poleVector);
            _middleTransform.rotation = deltaRotation * _middleTransform.rotation;

            // 2. Fix the rotation in the upper joint such that the lower joint position matches
            // the goal position.
            lowerPos = _lowerTransform.position;
            upperToLower = lowerPos - upperPos;
            _upperTransform.rotation = Quaternion.FromToRotation(upperToLower, upperToTarget) * _upperTransform.rotation;

            // 3. Rotate the upper joint by the difference in rotation.
            middlePos = _middleTransform.position;
            upperToTarget.Normalize();
            var midToUpper = middlePos - upperPos;
            var poleToUpper = _poleTransform.position - upperPos;
            var midToUpperProjected =  midToUpper - upperToTarget * Vector3.Dot(midToUpper, upperToTarget);
            var poleToUpperProjected = poleToUpper - upperToTarget * Vector3.Dot(poleToUpper, upperToTarget);
            if (Mathf.Abs(midToUpperProjected.sqrMagnitude - poleToUpperProjected.sqrMagnitude) > _epsilon)
            {
                _upperTransform.rotation = Quaternion.FromToRotation(midToUpperProjected, poleToUpperProjected) * _upperTransform.rotation;
            }

            // 4. Fix the rotation in the lower joint to match the orientation of the goal transform.
            _lowerTransform.rotation = targetRot;
        }

        /// <summary>
        /// Law of cosines: https://en.wikipedia.org/wiki/Law_of_cosines
        /// </summary>
        /// <returns>The angles of a triangle given three sides.</returns>
        private static float AngleOfTriangle(float a, float b, float c)
        {
            var angle = Mathf.Clamp((a * a + b * b - c * c) / (2.0f * a * b), -1.0f, 1.0f);
            return Mathf.Acos(angle);
        }
    }
}
