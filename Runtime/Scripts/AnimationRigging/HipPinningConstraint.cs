// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
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
        /// Animator component.
        /// </summary>
        public Animator AnimatorComponent { get; }

        /// <summary>
        /// If true, update this job.
        /// </summary>
        public bool ShouldUpdate { get; set; }

        /// <summary>
        /// The array of available hip pinning targets.
        /// </summary>
        public HipPinningConstraintTarget[] HipPinningTargets { get; }

        /// <summary>
        /// Returns the current hip pinning target.
        /// </summary>
        public HipPinningConstraintTarget CurrentHipPinningTarget { get; }

        /// <summary>
        /// Has run calibration or not.
        /// </summary>
        public bool HasCalibrated { get; }

        /// <summary>
        /// The bones that compose the skeleton.
        /// </summary>
        public Transform[] Bones { get; }

        /// <summary>
        /// The root bone of this transform.
        /// </summary>
        public Transform Root { get; }

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
        /// If true, this is the first frame that the newly created job is running on.
        /// </summary>
        public bool IsFirstFrame { get; set; }

        /// <summary>
        /// Indicates if the constraint has been set up or not.
        /// </summary>
        public bool ObtainedProperReferences { get; }

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

        /// <summary>
        /// Returns hip transform.
        /// </summary>
        /// <returns>Transform of the hip.</returns>
        public Transform GetHipTransform();

        /// <summary>
        /// Indicates if bone transforms are valid or not.
        /// </summary>
        /// <returns>True if so, false if not.</returns>
        public bool IsBoneTransformsDataValid();

        /// <summary>
        /// Retrieves index of bone above hips.
        /// </summary>
        /// <returns>Index of bone above hips.</returns>
        public int GetIndexOfFirstBoneAboveHips();

        /// <summary>
        /// Allows storing the initial hip rotation.
        /// </summary>
        public void SetInitialHipRotation();
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
        Animator IHipPinningData.AnimatorComponent => _animator;

        /// <inheritdoc />
        bool IHipPinningData.ShouldUpdate
        {
            get => _shouldUpdate;
            set => _shouldUpdate = value;
        }

        /// <inheritdoc />
        HipPinningConstraintTarget[] IHipPinningData.HipPinningTargets => _hipPinningTargets;

        /// <inheritdoc />
        HipPinningConstraintTarget IHipPinningData.CurrentHipPinningTarget => _currentHipPinningTarget;

        /// <inheritdoc />
        Transform[] IHipPinningData.Bones => _bones;

        /// <inheritdoc />
        Transform IHipPinningData.Root => _root;

        /// <inheritdoc />
        Vector3 IHipPinningData.CalibratedHipPos => _calibratedHipTranslation;

        /// <inheritdoc />
        bool IHipPinningData.HasCalibrated => _hasCalibrated;

        /// <inheritdoc />
        Quaternion IHipPinningData.InitialHipLocalRotation => _initialHipLocalRotation;

        /// <inheritdoc />
        float IHipPinningData.HipPinningLeaveRange => _hipPinningLeaveRange;

        /// <inheritdoc />
        bool IHipPinningData.HipPinningLeave => _enableHipPinningLeave;

        /// <inheritdoc />
        public bool IsFirstFrame
        {
            get => _isFirstFrame;
            set => _isFirstFrame = value;
        }

        /// <inheritdoc />
        public bool ObtainedProperReferences => _obtainedProperReferences;

        /// <inheritdoc cref="IHipPinningData.EnterHipPinningArea"/>
        public event Action<HipPinningConstraintTarget> OnEnterHipPinningArea;

        /// <inheritdoc cref="IHipPinningData.ExitHipPinningArea"/>
        public event Action<HipPinningConstraintTarget> OnExitHipPinningArea;

        /// <inheritdoc cref="IHipPinningData.ConstraintSkeleton"/>
        [Tooltip(HipPinningDataTooltips.Skeleton)]
        [NotKeyable, SerializeField]
        private OVRCustomSkeleton _skeleton;

        /// <inheritdoc cref="IHipPinningData.AnimatorComponent"/>
        [Tooltip(HipPinningDataTooltips.Animator)]
        [NotKeyable, SerializeField]
        private Animator _animator;

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

        /// <summary>
        /// Root transform of skeleton.
        /// </summary>
        [SyncSceneToStream, SerializeField, HideInInspector]
        private Transform _root;

        /// <summary>
        /// Array of skeletal bones.
        /// </summary>
        [SyncSceneToStream, SerializeField, HideInInspector]
        private Transform[] _bones;

        /// <summary>
        /// Indicates if all references have been obtained or not.
        /// </summary>
        [NotKeyable, SerializeField, HideInInspector]
        private bool _obtainedProperReferences;

        /// <summary>
        /// Initial hip rotation, used to calculate how much a person has rotated.
        /// </summary>
        [NotKeyable, SerializeField, HideInInspector]
        private Quaternion _initialHipLocalRotation;

        private Vector3 _calibratedHipTranslation;
        private bool _hasCalibrated;
        private HipPinningConstraintTarget _currentHipPinningTarget;
        private bool _shouldUpdate;
        private bool _isFirstFrame;

        private bool _hasSetUp;

        /// <summary>
        /// Run set-up if necessary. This is necessary for
        /// OVRCustomSkeleton-based characters, since the bones are
        /// re-parented at runtime and they need to be re-queried.
        /// </summary>
        public void SetUp()
        {
            if (_hasSetUp)
            {
                return;
            }
            if (_skeleton == null && _animator != null)
            {
                _hasSetUp = true;
                return;
            }
            _hasSetUp = true;
            _bones = new Transform[_skeleton.CustomBones.Count];
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i] = _skeleton.CustomBones[i];
            }
            _root = _bones[(int)(IsSkeletonFullBody() ?
                OVRSkeleton.BoneId.FullBody_Hips :
                OVRSkeleton.BoneId.Body_Hips)].parent;
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
        /// Assign the animator component.
        /// </summary>
        /// <param name="animator">The animator component to assign.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Sets up bone references to the character.
        /// </summary>
        public void SetUpBoneReferences()
        {
            if (_skeleton != null)
            {
                SetUpBonesOVR();
            }
            else if (_animator != null)
            {
                SetUpBonesAnimator();
            }
            _obtainedProperReferences = true;
        }

        private void SetUpBonesOVR()
        {
            _bones = new Transform[_skeleton.CustomBones.Count];
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i] = _skeleton.CustomBones[i];
            }
            _root = _bones[(int)(IsSkeletonFullBody() ?
                OVRSkeleton.BoneId.FullBody_Hips :
                OVRSkeleton.BoneId.Body_Hips)].parent;
            _initialHipLocalRotation = GetHipTransform().localRotation;
        }

        private void SetUpBonesAnimator()
        {
            var hipTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hipTransform == null)
            {
                throw new Exception("Your character's Humanoid mapping is missing a hip transform.");
            }
            List<Transform> allBones = GetValidAnimatorBones();
            _bones = allBones.ToArray();
            _root = hipTransform.parent;
            _initialHipLocalRotation = GetHipTransform().localRotation;
        }

        private List<Transform> GetValidAnimatorBones()
        {
            List<Transform> allBones = new List<Transform>();
            for (var currBone = HumanBodyBones.Hips; currBone < HumanBodyBones.LastBone; currBone++)
            {
                var boneTransform = _animator.GetBoneTransform(currBone);
                if (boneTransform != null)
                {
                    allBones.Add(boneTransform);
                }
            }
            return allBones;
        }

        /// <summary>
        /// Resets all references.
        /// </summary>
        public void ClearSetupReferences()
        {
            _skeleton = null;
            _animator = null;
            _bones = null;
            _initialHipLocalRotation = Quaternion.identity;
            _root = null;
            _obtainedProperReferences = false;
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
            _calibratedHipTranslation = GetHipTransform().position;
            _hasCalibrated = true;
            if (_enableHipPinningHeightAdjustment)
            {
                _currentHipPinningTarget.UpdateHeight(position.y - _currentHipPinningTarget.HipTargetTransform.position.y);
            }
        }

        /// <inheritdoc />
        public void SetInitialHipRotation()
        {
            _initialHipLocalRotation = GetHipTransform().localRotation;
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

        /// <inheritdoc />
        public bool IsBoneTransformsDataValid()
        {
            return (_skeleton != null && _skeleton.IsDataValid) ||
                (_animator != null);
        }

        private bool IsSkeletonFullBody()
        {
            return _skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;
        }

        /// <inheritdoc />
        public Transform GetHipTransform()
        {
            if (_skeleton != null)
            {
                return _bones[IsSkeletonFullBody() ?
                    (int)OVRSkeleton.BoneId.FullBody_Hips :
                    (int)OVRSkeleton.BoneId.Body_Hips];
            }
            else
            {
                return _bones[(int)HumanBodyBones.Hips];
            }
        }

        /// <inheritdoc />
        public int GetIndexOfFirstBoneAboveHips()
        {
            if (_skeleton != null)
            {
                return IsSkeletonFullBody() ?
                    (int)OVRSkeleton.BoneId.FullBody_SpineLower :
                    (int)OVRSkeleton.BoneId.Body_SpineLower;
            }
            else
            {
                return (int)(HumanBodyBones.Hips + 1);
            }
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
            _animator = null;
            _hipPinningTargets = null;
            _enableHipPinningHeightAdjustment = false;
            _enableHipPinningLeave = false;
            _hipPinningLeaveRange = 0.0f;
            _root = null;
            _bones = null;
            _obtainedProperReferences = false;
            _hasSetUp = false;
            _hasCalibrated = false;
        }
    }

    /// <summary>
    /// Hip Pinning constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Hip Pinning Constraint")]
    public class HipPinningConstraint : RigConstraint<
        HipPinningJob,
        HipPinningData,
        HipPinningJobBinder<HipPinningData>>,
        IOVRSkeletonConstraint
    {
        /// <inheritdoc />
        public void RegenerateData()
        {
            data.SetUp();
        }
    }
}
