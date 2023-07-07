// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for hip pinning data.
    /// </summary>
    public interface IHipPinningData
    {
        /// <summary>
        /// The OVR Skeleton component.
        /// </summary>
        public OVRCustomSkeleton ConstraintSkeleton { get; }

        /// <summary>
        /// The array of available hip pinning targets.
        /// </summary>
        public HipPinningConstraintTarget[] HipPinningTargets { get; }

        /// <summary>
        /// Returns the current hip pinning target.
        /// </summary>
        public HipPinningConstraintTarget CurrentHipPinningTarget { get; }

        /// <summary>
        /// The bones that compose the skeleton.
        /// </summary>
        public Transform[] Bones  { get; }

        /// <summary>
        /// The calibrated hip position.
        /// </summary>
        public Vector3 CalibratedHipPos { get; }

        /// <summary>
        /// The initial hip local rotation.
        /// </summary>
        public Quaternion InitialHipLocalRotation { get; }

        /// <summary>
        /// The range from the hip pinning target before hip pinning is disabled.
        /// </summary>
        public float HipPinningLeaveRange { get; }

        /// <summary>
        /// If true, hip pinning will be disabled when the character leaves a certain range.
        /// </summary>
        public bool HipPinningLeave { get; }

        /// <summary>
        /// Event when the user enters a hip pinning area.
        /// </summary>
        public event Action<HipPinningConstraintTarget> OnEnterHipPinningArea;

        /// <summary>
        /// Event when the user leaves a hip pinning area.
        /// </summary>
        public event Action<HipPinningConstraintTarget> OnExitHipPinningArea;

        /// <summary>
        /// Find and assign the closest hip pinning target to be the current hip pinning target.
        /// </summary>
        /// <param name="position">The position to check against.</param>
        public void AssignClosestHipPinningTarget(Vector3 position);

        /// <summary>
        /// Called when the user enters a hip pinning area.
        /// </summary>
        public void EnterHipPinningArea(HipPinningConstraintTarget target);

        /// <summary>
        /// Called when the user leaves a hip pinning area.
        /// </summary>
        public void ExitHipPinningArea(HipPinningConstraintTarget target);
    }

    /// <summary>
    /// Hip Pinning data used by the hip pinning job.
    /// Implements the hip pinning data interface.
    /// </summary>
    [Serializable]
    public struct HipPinningData : IAnimationJobData, IHipPinningData
    {
        // Interface implementation
        /// <inheritdoc />
        OVRCustomSkeleton IHipPinningData.ConstraintSkeleton => _skeleton;

        /// <inheritdoc />
        HipPinningConstraintTarget[] IHipPinningData.HipPinningTargets => _hipPinningTargets;

        /// <inheritdoc />
        HipPinningConstraintTarget IHipPinningData.CurrentHipPinningTarget => _currentHipPinningTarget;

        /// <inheritdoc />
        Transform[] IHipPinningData.Bones => _bones;

        /// <inheritdoc />
        Vector3 IHipPinningData.CalibratedHipPos => _calibratedHipTranslation;

        /// <inheritdoc />
        Quaternion IHipPinningData.InitialHipLocalRotation => _initialHipLocalRotation;

        /// <inheritdoc />
        float IHipPinningData.HipPinningLeaveRange => _hipPinningLeaveRange;

        /// <inheritdoc />
        bool IHipPinningData.HipPinningLeave => _enableHipPinningLeave;

        /// <inheritdoc cref="IHipPinningData.EnterHipPinningArea"/>
        public event Action<HipPinningConstraintTarget> OnEnterHipPinningArea;

        /// <inheritdoc cref="IHipPinningData.ExitHipPinningArea"/>
        public event Action<HipPinningConstraintTarget> OnExitHipPinningArea;

        /// <inheritdoc cref="IHipPinningData.ConstraintSkeleton"/>
        [Tooltip(HipPinningDataTooltips.Skeleton)]
        [NotKeyable, SerializeField]
        private OVRCustomSkeleton _skeleton;

        /// <inheritdoc cref="IHipPinningData.HipPinningTargets"/>
        [Tooltip(HipPinningDataTooltips.HipPinningTargets)]
        [NotKeyable, SerializeField]
        private HipPinningConstraintTarget[] _hipPinningTargets;

        /// <summary>
        /// If true, hip pinning will adjust the height of the seat to match the tracked position.
        /// </summary>
        [Tooltip(HipPinningDataTooltips.HipPinningHeightAdjustment)]
        [NotKeyable, SerializeField]
        private bool _enableHipPinningHeightAdjustment;

        /// <inheritdoc cref="IHipPinningData.HipPinningLeave"/>
        [Tooltip(HipPinningDataTooltips.HipPinningLeave)]
        [NotKeyable, SerializeField]
        private bool _enableHipPinningLeave;

        /// <inheritdoc cref="IHipPinningData.HipPinningLeaveRange"/>
        [Tooltip(HipPinningDataTooltips.HipPinningLeaveRange)]
        [NotKeyable, SerializeField]
        private float _hipPinningLeaveRange;

        [SyncSceneToStream]
        private Transform[] _bones;
        private Vector3 _calibratedHipTranslation;
        private Quaternion _initialHipLocalRotation;
        private HipPinningConstraintTarget _currentHipPinningTarget;

        private bool _obtainedProperReferences;

        /// <summary>
        /// Setup the hip pinning constraint.
        /// </summary>
        /// <returns>Returns true if has setup runs or has already run.</returns>
        public bool Setup()
        {
            if (_obtainedProperReferences)
            {
                return true;
            }

            if (!_skeleton.IsInitialized || !_skeleton.IsDataValid)
            {
                return false;
            }

            _bones = new Transform[_skeleton.CustomBones.Count];
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i] = _skeleton.CustomBones[i];
            }
            _initialHipLocalRotation = _bones[(int)OVRSkeleton.BoneId.Body_Hips].transform.localRotation;
            _obtainedProperReferences = true;
            return true;
        }

        /// <summary>
        /// Assign the OVR Skeleton.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton</param>
        public void AssignOVRSkeleton(OVRCustomSkeleton skeleton)
        {
            _skeleton = skeleton;
        }

        /// <summary>
        /// Find and assign the closest hip pinning target to be the current hip pinning target.
        /// </summary>
        /// <param name="position">The position to check against.</param>
        public void AssignClosestHipPinningTarget(Vector3 position)
        {
            HipPinningConstraintTarget closestHipPinningTarget = null;
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
            EnterHipPinningArea(closestHipPinningTarget);
        }

        /// <summary>
        /// Calibrates the height of the hip pinning target to match the character's height.
        /// </summary>
        /// <param name="position">The position of the character's hips.</param>
        public void CalibrateInitialHipHeight(Vector3 position)
        {
            _calibratedHipTranslation = _bones[(int)OVRSkeleton.BoneId.Body_Hips].position;
            if (_enableHipPinningHeightAdjustment)
            {
                _currentHipPinningTarget.UpdateHeight(position.y - _currentHipPinningTarget.HipTargetTransform.position.y);
            }
        }

        /// <summary>
        /// Event when the user enters a hip pinning area.
        /// </summary>
        public void EnterHipPinningArea(HipPinningConstraintTarget target)
        {
            _currentHipPinningTarget = target;
            OnEnterHipPinningArea?.Invoke(target);
        }

        /// <summary>
        /// Event when the user leaves a hip pinning area.
        /// </summary>
        public void ExitHipPinningArea(HipPinningConstraintTarget target)
        {
            _currentHipPinningTarget = null;
            OnExitHipPinningArea?.Invoke(target);
        }

        bool IAnimationJobData.IsValid()
        {
            if (_skeleton == null)
            {
                return false;
            }

            if (_hipPinningTargets == null)
            {
                return false;
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _skeleton = null;
            _hipPinningTargets = null;
            _obtainedProperReferences = false;
        }
    }

    /// <summary>
    /// Hip Pinning constraint.
    /// </summary>
    [DisallowMultipleComponent]
    public class HipPinningConstraint : RigConstraint<
        HipPinningJob,
        HipPinningData,
        HipPinningJobBinder<HipPinningData>>,
        IOVRSkeletonConstraint
    {
        private void Start()
        {
            if (data.Setup())
            {
                gameObject.SetActive(true);
            }
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
            if (data.Setup())
            {
                gameObject.SetActive(true);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (gameObject.activeInHierarchy && !Application.isPlaying)
            {
                Debug.LogWarning($"{name} should be disabled initially; it enables itself when ready. Otherwise you" +
                    $" might get an errors regarding invalid sync variables at runtime.");
            }
        }
    }
}
