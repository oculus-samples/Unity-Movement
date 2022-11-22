// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Movement.Attributes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Hip pinning logic to pin an user's body to a point.
    /// </summary>
    public class HipPinningLogic : MonoBehaviour
    {
        /// <summary>
        /// Defines hip pinning properties.
        /// </summary>
        [Serializable]
        public class HipPinningProperties
        {
            /// <summary>
            /// The rotation limit axis.
            /// </summary>
            public enum RotationLimit
            {
                NegativeY = 0,
                PositiveY = 1,
                NegativeZ = 2,
                PositiveZ = 3,
                NegativeX = 4,
                PositiveX = 5,
                Count = 6
            }

            /// <summary>
            /// All of the body joint properties for hip pinning.
            /// </summary>
            public List<BodyJointProperties> BodyJointProperties;

            /// <summary>
            /// All of the disabled position constraints for hip pinning.
            /// </summary>
            public List<PositionConstraint> PositionConstraints;

            /// <summary>
            /// The rotation limits for this body.
            /// </summary>
            public float[] RotationLimits;

            /// <summary>
            /// The hips body joint properties.
            /// </summary>
            public BodyJointProperties HipBodyJointProperties => BodyJointProperties.Count > 0 ? BodyJointProperties[0] : null;

            /// <summary>
            /// Sets all of the position constraints active states for this body.
            /// </summary>
            /// <param name="isActive">If true, sets all positionalConstraint.constraintActive to true.</param>
            public void SetPositionConstraintsActive(bool isActive)
            {
                foreach (var positionConstraint in PositionConstraints)
                {
                    positionConstraint.constraintActive = isActive;
                }
            }

            /// <summary>
            /// Returns the joint local rotation limit for an axis.
            /// </summary>
            /// <param name="bodyJointProperties">The body joint properties.</param>
            /// <param name="rotationLimit">The rotation limit axis.</param>
            /// <returns>The joint local rotation limit.</returns>
            public float GetJointLocalRotationLimit(BodyJointProperties bodyJointProperties, RotationLimit rotationLimit)
            {
                switch (rotationLimit)
                {
                    case RotationLimit.NegativeY:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.y - RotationLimits[(int)rotationLimit];
                    case RotationLimit.PositiveY:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.y + RotationLimits[(int)rotationLimit];
                    case RotationLimit.NegativeZ:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.z - RotationLimits[(int)rotationLimit];
                    case RotationLimit.PositiveZ:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.z + RotationLimits[(int)rotationLimit];
                    case RotationLimit.NegativeX:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.x - RotationLimits[(int)rotationLimit];
                    case RotationLimit.PositiveX:
                        return bodyJointProperties.InitialLocalRotation.eulerAngles.x + RotationLimits[(int)rotationLimit];
                    case RotationLimit.Count:
                        return 0.0f;
                }
                return 0.0f;
            }
        }

        /// <summary>
        /// Defines body joint properties.
        /// </summary>
        [Serializable]
        public class BodyJointProperties
        {
            /// <summary>
            /// The body joint.
            /// </summary>
            public GameObject BodyJoint;

            /// <summary>
            /// The initial local rotation of the body joint.
            /// </summary>
            public Quaternion InitialLocalRotation;

            /// <summary>
            /// The positional constraint weight for this body joint.
            /// </summary>
            public float ConstraintWeight = 1.0f;

            /// <summary>
            /// The weight applied for the offsets applied by hip pinning.
            /// </summary>
            public float OffsetWeight = 1.0f;

            /// <summary>
            /// The distance before offsets stop getting applied to this body joint.
            /// </summary>
            public float PositionDistanceThreshold = 0.1f;

            /// <summary>
            /// The weight of the constraint scaling with distance when offsets stop getting applied to this body joint.
            /// </summary>
            public float PositionDistanceWeight = 1.0f;
        }

        /// <summary>
        /// Event when the user enters a hip pinning area.
        /// </summary>
        public event Action<HipPinningTarget> OnEnterHipPinningArea;

        /// <summary>
        /// Event when the user leaves a hip pinning area.
        /// </summary>
        public event Action<HipPinningTarget> OnExitHipPinningArea;

        /// <summary>
        /// Hip pinning properties.
        /// </summary>
        public HipPinningProperties HipPinProperties => _hipPinningProperties;

        /// <summary>
        /// The hip pinning target.
        /// </summary>
        public HipPinningTarget HipPinTarget { get; private set; }

        /// <summary>
        /// If true, enables leg rotation around the hips.
        /// </summary>
        public bool EnableLegRotation
        {
            get => _enableLegRotation;
            set => _enableLegRotation = value;
        }

        /// <summary>
        /// If true, enables constrained movement around the hip pinning surface.
        /// </summary>
        public bool EnableConstrainedMovement
        {
            get => _enableConstrainedMovement;
            set
            {
                HipPinTarget.ResetHipPinningTargetLocalPosition();
                _enableConstrainedMovement = value;
            }
        }

        /// <summary>
        /// If true, applies transformations to offset from the hip pinning target.
        /// </summary>
        public bool EnableApplyTransformations
        {
            get => _enableApplyTransformations;
            set => _enableApplyTransformations = value;
        }

        /// <summary>
        /// If true, enables leaving the hip pinning state when too far away from the hip pinning target.
        /// </summary>
        public bool EnableHipPinningLeave
        {
            get => _enableHipPinningLeave;
            set => _enableHipPinningLeave = value;
        }

        /// <summary>
        /// If true, leg rotation will be enabled.
        /// </summary>
        [SerializeField]
        protected bool _enableLegRotation;

        /// <summary>
        /// If true, leg rotation limits will be enabled.
        /// </summary>
        [SerializeField]
        protected bool _enableLegRotationLimits;

        /// <summary>
        /// If true, movement around the constrained surface will be enabled.
        /// </summary>
        [SerializeField]
        protected bool _enableConstrainedMovement;

        /// <summary>
        /// If true, the entire body will be transformed to undo the offset applied by the hip pinning position constraint.
        /// </summary>
        [SerializeField]
        protected bool _enableApplyTransformations = true;

        /// <summary>
        /// If true, hip pinning will be disabled when the character leaves a certain range.
        /// </summary>
        [SerializeField]
        protected bool _enableHipPinningLeave = true;

        /// <summary>
        /// If true, hip pinning will adjust the height of the seat to match the tracked position.
        /// </summary>
        [SerializeField]
        protected bool _enableHipPinningHeightAdjustment;

        /// <summary>
        /// If true, hip pinning is currently active.
        /// </summary>
        [SerializeField]
        protected bool _hipPinningActive;

        /// <summary>
        /// The range from the hip pinning target before hip pinning is disabled.
        /// </summary>
        [SerializeField]
        protected float _hipPinningLeaveRange = 1.0f;

        /// <summary>
        /// The OVR Skeleton component.
        /// </summary>
        [SerializeField]
        protected OVRCustomSkeleton _skeleton;

        /// <summary>
        /// The Mirror Skeleton component.
        /// </summary>
        [SerializeField]
        protected MirrorSkeleton _mirrorSkeleton;

        /// <summary>
        /// The list of hip pinning targets in the scene.
        /// </summary>
        [SerializeField]
        protected HipPinningTarget[] _hipPinningTargets;

        /// <summary>
        /// The hip pinning properties.
        /// </summary>
        [SerializeField]
        protected HipPinningProperties _hipPinningProperties;

        private OVRCustomSkeleton _currentSkeleton;
        private OVRCustomSkeleton _originalSkeleton;
        private Vector3 _trackedHipTranslation;
        private Vector3 _calibratedHipTranslation;
        private bool _shouldFlipRotationLimit;
        private bool _shouldUpdate = true;

        private void Start()
        {
            Assert.IsTrue(_skeleton != null || _mirrorSkeleton != null);
            if (_mirrorSkeleton != null)
            {
                _currentSkeleton = _mirrorSkeleton.MirroredSkeleton;
                _originalSkeleton = _mirrorSkeleton.OriginalSkeleton;
            }
            else
            {
                _currentSkeleton = _skeleton;
                _originalSkeleton = _skeleton;
            }
            Assert.IsNotNull(_hipPinningProperties.HipBodyJointProperties);
            Assert.IsTrue(_hipPinningTargets.Length > 0);

            SetHipPinningActive(_hipPinningActive);
            AssignClosestHipPinningTarget(_hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.position);
        }

        /// <summary>
        /// Find and assign the closest hip pinning target to be the current hip pinning target.
        /// </summary>
        public void AssignClosestHipPinningTarget(Vector3 position)
        {
            AssignHipPinningTarget(GetClosestHipPinningTarget(position));
        }

        /// <summary>
        /// Sets the hip pinning state.
        /// </summary>
        /// <param name="isActive">If true, hip pinning is enabled.</param>
        public void SetHipPinningActive(bool isActive)
        {
            _hipPinningActive = isActive;

            // Disable all position constraints
            foreach (var positionConstraint in _hipPinningProperties.PositionConstraints)
            {
                if (positionConstraint != null)
                {
                    positionConstraint.enabled = false;
                }
            }
            if (isActive && _enableApplyTransformations)
            {
                _hipPinningProperties.SetPositionConstraintsActive(false);
                _hipPinningProperties.PositionConstraints[0].constraintActive = true;
            }
            else
            {
                _hipPinningProperties.SetPositionConstraintsActive(isActive);
            }
        }

        /// <summary>
        /// Calibrates the height of the hip pinning target to match the character's height.
        /// </summary>
        /// <param name="position">The position of the character's hips.</param>
        public void CalibrateInitialHipHeight(Vector3 position)
        {
            _calibratedHipTranslation = _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.position;
            if (_enableHipPinningHeightAdjustment)
            {
                HipPinTarget.UpdateHeight(position.y - HipPinTarget.HipTargetTransform.position.y);
            }
        }

        /// <summary>
        /// Initializes hip pinning position constraints based on the hip pinning target.
        /// </summary>
        /// <param name="hipPinningTarget">The hip pinning targe to initialize hip pinning for.</param>
        public void InitializeHipPinning(HipPinningTarget hipPinningTarget)
        {
            var constraintSources = new List<ConstraintSource>();

            if (HipPinProperties.BodyJointProperties.Count > 0)
            {
                for (int i = 0; i < hipPinningTarget.SpineTargetTransforms.Count; i++)
                {
                    var bodyJointProperties = HipPinProperties.BodyJointProperties[i];
                    constraintSources.Clear();
                    constraintSources.Add(
                        new ConstraintSource()
                        {
                            sourceTransform = hipPinningTarget.SpineTargetTransforms[i],
                            weight = bodyJointProperties.ConstraintWeight
                        });
                    constraintSources.Add(
                        new ConstraintSource()
                        {
                            sourceTransform = HipPinProperties.BodyJointProperties[i].BodyJoint.transform,
                            weight = 1.0f - bodyJointProperties.ConstraintWeight
                        });
                    GameObject bodyJoint = bodyJointProperties.BodyJoint;
                    var positionConstraint = bodyJoint.GetComponent<PositionConstraint>();
                    if (positionConstraint == null)
                    {
                        positionConstraint = bodyJoint.AddComponent<PositionConstraint>();
                    }
                    positionConstraint.constraintActive = true;
                    positionConstraint.SetSources(constraintSources);
                    positionConstraint.translationAtRest = Vector3.zero;
                    positionConstraint.translationOffset = Vector3.zero;
                    positionConstraint.locked = true;
                    positionConstraint.enabled = false;
                    HipPinProperties.PositionConstraints[i] = positionConstraint;
                }
            }
        }

        /// <summary>
        /// Apply hip pinning logic.
        /// </summary>
        public void ApplyHipPinning()
        {
            if (!_hipPinningActive)
            {
                return;
            }

            if (_enableLegRotation)
            {
                RestrictHipPinTargetRotation();
            }

            if (_originalSkeleton.IsDataValid)
            {
                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                if (_enableConstrainedMovement)
                {
                    AllowHipMovementWithinConstrainedArea();
                }

                if (_hipPinningActive)
                {
                    RestrictAllBonesBasedOnHip();
                }

                if (_enableHipPinningLeave && _originalSkeleton.IsDataValid)
                {
                    CheckIfHipPinningIsValid();
                }

                ApplyConstraints();

                if (!_originalSkeleton.IsDataValid)
                {
                    _shouldUpdate = false;
                }
            }
        }

        private void ApplyConstraints()
        {
            var sources = new List<ConstraintSource>();
            foreach (var positionConstraint in _hipPinningProperties.PositionConstraints)
            {
                if (positionConstraint.constraintActive)
                {
                    sources.Clear();
                    positionConstraint.GetSources(sources);
                    Vector3 constrainedPosition = Vector3.zero;
                    foreach (var source in sources)
                    {
                        constrainedPosition += source.sourceTransform.position * source.weight;
                    }
                    positionConstraint.transform.position = constrainedPosition;
                }
            }
        }

        private void CheckIfHipPinningIsValid()
        {
            Vector3 trackedHipTranslation = _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.position;
            float dist = Vector3.Distance(_calibratedHipTranslation, trackedHipTranslation);
            if (dist > _hipPinningLeaveRange)
            {
                RemoveHipPinningTarget(HipPinTarget);
                SetHipPinningActive(false);
            }
        }

        private HipPinningTarget GetClosestHipPinningTarget(Vector3 position)
        {
            HipPinningTarget closestHipPinningTarget = null;
            var lowestDist = float.MaxValue;
            foreach (var target in _hipPinningTargets)
            {
                float dist = Vector3.Distance(position, target.HipTargetTransform.position);
                if (dist < lowestDist)
                {
                    closestHipPinningTarget = target;
                    lowestDist = dist;
                }
            }
            return closestHipPinningTarget;
        }

        private void AssignHipPinningTarget(HipPinningTarget target)
        {
            HipPinTarget = target;
            OnEnterHipPinningArea?.Invoke(target);
        }

        private void RemoveHipPinningTarget(HipPinningTarget target)
        {
            OnExitHipPinningArea?.Invoke(target);
            HipPinTarget = null;
        }

        private void RestrictHipPinTargetRotation()
        {
            Vector3 hipJointLocalEulerAngles = _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.localEulerAngles;
            float constrainedHipEulerY = hipJointLocalEulerAngles.y;
            if (_enableLegRotationLimits)
            {
                constrainedHipEulerY = GetConstrainedHipEulerY(constrainedHipEulerY);
            }

            _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.localRotation = Quaternion.Euler(
                hipJointLocalEulerAngles.x,
                constrainedHipEulerY,
                hipJointLocalEulerAngles.z);

            HipPinTarget.ChairSeatTransform.localRotation = _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.localRotation *
                Quaternion.Inverse(_hipPinningProperties.HipBodyJointProperties.InitialLocalRotation) *
                HipPinTarget.HipTargetInitialRotationOffset;
        }

        private float GetConstrainedHipEulerY(float constrainedHipEulerY)
        {
            float negativeRotationLimitY = _hipPinningProperties.GetJointLocalRotationLimit(
                HipPinProperties.HipBodyJointProperties,
                HipPinningProperties.RotationLimit.NegativeY);
            float positiveRotationLimitY = _hipPinningProperties.GetJointLocalRotationLimit(
                HipPinProperties.HipBodyJointProperties,
                HipPinningProperties.RotationLimit.PositiveY);
            if (constrainedHipEulerY < -180.0f)
            {
                constrainedHipEulerY += _shouldFlipRotationLimit ? 360.0f : -360.0f;
            }
            if (constrainedHipEulerY > 0.0f && constrainedHipEulerY < 180.0f)
            {
                constrainedHipEulerY += _shouldFlipRotationLimit ? -360.0f : 360.0f;
            }
            if (constrainedHipEulerY < 0.0f)
            {
                if (negativeRotationLimitY > 0.0f)
                {
                    negativeRotationLimitY -= 360.0f;
                }
                if (positiveRotationLimitY > 0.0f)
                {
                    positiveRotationLimitY -= 360.0f;
                }
            }
            if (constrainedHipEulerY > 0.0f)
            {
                if (negativeRotationLimitY < 0.0f)
                {
                    negativeRotationLimitY += 360.0f;
                }
                if (positiveRotationLimitY < 0.0f)
                {
                    positiveRotationLimitY += 360.0f;
                }
            }

            if (constrainedHipEulerY >= negativeRotationLimitY)
            {
                _shouldFlipRotationLimit = false;
            }
            if (constrainedHipEulerY <= positiveRotationLimitY)
            {
                _shouldFlipRotationLimit = true;
            }

            constrainedHipEulerY =
                Mathf.Clamp(constrainedHipEulerY,  negativeRotationLimitY, positiveRotationLimitY);

            return constrainedHipEulerY;
        }

        private void AllowHipMovementWithinConstrainedArea()
        {
            HipPinTarget.UpdateHipTargetTransform(_hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.position);
        }

        private void RestrictAllBonesBasedOnHip()
        {
            _trackedHipTranslation = _hipPinningProperties.HipBodyJointProperties.BodyJoint.transform.localPosition;
            Vector3 hipPinningPosition = transform.InverseTransformPoint(HipPinTarget.HipTargetTransform.position);
            Vector3 toInitialHipPositionDelta = hipPinningPosition - _trackedHipTranslation;

            // Apply the offset transformation to all joints
            if (_enableApplyTransformations)
            {
                ApplyOffsetTransformationBasedOnHip(toInitialHipPositionDelta);
            }
            else
            {
                ApplyDecreasingTransformationBasedOnHip(toInitialHipPositionDelta);
            }
        }

        private void ApplyOffsetTransformationBasedOnHip(Vector3 toInitialHipPositionDelta)
        {
            for (int i = (int)OVRSkeleton.BoneId.Body_Hips + 1; i < _currentSkeleton.Bones.Count; i++)
            {
                var bone = _currentSkeleton.Bones[i];
                if (bone != null)
                {
                    bone.Transform.localPosition += toInitialHipPositionDelta;
                }
            }
        }

        private void ApplyDecreasingTransformationBasedOnHip(Vector3 toInitialHipPositionDelta)
        {
            // Apply a decreasing offset from hips to the specified joint
            for (int i = 1; i < _hipPinningProperties.BodyJointProperties.Count; i++)
            {
                var boneId = OVRSkeleton.BoneId.Body_Hips + i;
                var bone = _currentSkeleton.Bones[(int)boneId];
                if (bone.Transform != null)
                {
                    var bodyJointProperties = _hipPinningProperties.BodyJointProperties[i];
                    Vector3 positionDeltaToApply = toInitialHipPositionDelta * bodyJointProperties.OffsetWeight;
                    bone.Transform.localPosition += positionDeltaToApply;

                    var constraintWeightDistance = Vector3.Distance(bone.Transform.position, HipPinTarget.SpineTargetTransforms[i].position);
                    if (constraintWeightDistance >= bodyJointProperties.PositionDistanceThreshold)
                    {
                        // The first constraint source will always be the position constraint located on the hip pinning target
                        // This position constraint given an initial weight in the configuration
                        // but we increase the weight proportional to the distance the user's bone translation is from
                        // the constrained position to prevent the body from stretching as much
                        var constraintSource = _hipPinningProperties.PositionConstraints[i].GetSource(0);
                        constraintSource.weight = bodyJointProperties.ConstraintWeight + constraintWeightDistance * bodyJointProperties.PositionDistanceWeight;
                        _hipPinningProperties.PositionConstraints[i].SetSource(0, constraintSource);
                    }
                }
            }
        }
    }
}
