// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using static Oculus.Movement.Utils.JobCommons;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Update target transforms with bone data.
    /// </summary>
    [System.Serializable]
    public class ExternalBoneTargets
    {
        /// <summary>
        /// A retargeted bone target.
        /// </summary>
        [System.Serializable]
        public class BoneTarget
        {
            /// <summary>
            /// The OVRSkeleton.BoneId that must be tracked.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.BoneId)]
            public OVRSkeleton.BoneId BoneId { get; set; }

            /// <summary>
            /// The human body bone representation of this bone.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.HumanBodyBone)]
            public HumanBodyBones HumanBodyBone;

            /// <summary>
            /// The target transform to update with body tracking bone data.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.Target)]
            public Transform Target;
        }

        /// <summary>
        /// The array of bone targets.
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.BoneTargets)]
        protected BoneTarget[] _boneTargets;

        /// <inheritdoc cref="_boneTargets"/>
        public BoneTarget[] BoneTargetsArray
        {
            get => _boneTargets;
            set => _boneTargets = value;
        }

        /// <summary>
        /// Is it full body (or not).
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.IsFullBody)]
        protected bool _fullBody = false;

        /// <inheritdoc cref="_fullBody"/>
        public bool FullBody
        {
            get => _fullBody;
            set => _fullBody = value;
        }

        /// <summary>
        /// Enables or disables functionality.
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.Enabled)]
        protected bool _enabled = false;

        /// <inheritdoc cref="_enabled"/>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Whether to C# jobs or not.
        /// </summary>
        public bool UseJobs { get; set; }

        private bool _initialized;
        private Transform[] _currentTargetBones;
        private TransformAccessArray _targetBones;
        private TransformAccessArray _retargetedBoneTargets;
        private NativeArray<Pose> _boneTargetPoses;
        private GetPosesJob _getPosesJob;
        private WritePosesToTransformsJob _setExternalBoneTargetsJob;
        private JobHandle _getPosesJobHandle;
        private JobHandle _setExternalBoneTargetsJobHandle;

        /// <summary>
        /// Correlate HumanBodyBones to OVRSkeleton.BoneId.
        /// </summary>
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            var humanBodyBoneToOVRBoneId = _fullBody
                ? OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key)
                : OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key);
            foreach (var retargetedBoneTarget in _boneTargets)
            {
                retargetedBoneTarget.BoneId = humanBodyBoneToOVRBoneId[retargetedBoneTarget.HumanBodyBone];
            }

            // Sort by bone id.
            _boneTargets = _boneTargets.OrderBy(x => x.BoneId).ToArray();
            _initialized = true;
        }

        private void InitializeJobs(IList<OVRBone> bones)
        {
            if (!BonesChanged())
            {
                return;
            }

            Complete();
            CleanUp();

            _currentTargetBones = new Transform[_boneTargets.Length];
            var retargetedBoneTargets = new Transform[_boneTargets.Length];
            var retargetedBoneTargetIndex = 0;
            foreach (var bone in bones)
            {
                var retargetedBoneTarget = _boneTargets[retargetedBoneTargetIndex];
                if (bone.Id != retargetedBoneTarget.BoneId)
                {
                    continue;
                }

                var targetBone = bone.Transform;
                if (targetBone == null)
                {
                    continue;
                }

                _currentTargetBones[retargetedBoneTargetIndex] = targetBone;
                retargetedBoneTargets[retargetedBoneTargetIndex] = retargetedBoneTarget.Target;
                retargetedBoneTargetIndex++;
            }

            _targetBones = new TransformAccessArray(_currentTargetBones);
            _retargetedBoneTargets = new TransformAccessArray(retargetedBoneTargets);
            _boneTargetPoses = new NativeArray<Pose>(_retargetedBoneTargets.length, Allocator.Persistent);
            _getPosesJob = new GetPosesJob { Poses = _boneTargetPoses };
            _setExternalBoneTargetsJob = new WritePosesToTransformsJob { SourcePoses = _boneTargetPoses };
        }

        private bool BonesChanged()
        {
            if (_currentTargetBones == null || _currentTargetBones.Length == 0)
            {
                return true;
            }

            // If first element is null, then rest are invalid.
            if (_currentTargetBones[0] == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update the bone targets with the bone transform data.
        /// </summary>
        /// <param name="skeleton"></param>
        public void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (!_enabled)
            {
                return;
            }

            Initialize();
            if (UseJobs)
            {
                InitializeJobs(skeleton.Bones);
                _getPosesJobHandle = _getPosesJob.ScheduleReadOnly(_targetBones, 32);
                _setExternalBoneTargetsJobHandle = _setExternalBoneTargetsJob.Schedule(_retargetedBoneTargets, _getPosesJobHandle);
            }
            else
            {
                var retargetedBoneTargetIndex = 0;
                var bones = skeleton.Bones;
                foreach (var bone in bones)
                {
                    var retargetedBoneTarget = _boneTargets[retargetedBoneTargetIndex];
                    if (bone.Id != retargetedBoneTarget.BoneId)
                    {
                        continue;
                    }

                    retargetedBoneTargetIndex++;
                    var targetBone = bone.Transform;
                    if (targetBone == null)
                    {
                        continue;
                    }

                    retargetedBoneTarget.Target.SetPositionAndRotation(
                        targetBone.position, targetBone.rotation);
                }
            }
        }

        /// <summary>
        /// Complete the job.
        /// </summary>
        public void Complete()
        {
            if (!UseJobs)
            {
                return;
            }
            _getPosesJobHandle.Complete();
            _setExternalBoneTargetsJobHandle.Complete();
        }

        /// <summary>
        /// Cleans up anything that needs to be manually deallocated.
        /// </summary>
        public void CleanUp()
        {
            if (_targetBones.isCreated)
            {
                _targetBones.Dispose();
            }

            if (_retargetedBoneTargets.isCreated)
            {
                _retargetedBoneTargets.Dispose();
            }

            if (_boneTargetPoses.IsCreated)
            {
                _boneTargetPoses.Dispose();
            }
        }
    }
}
