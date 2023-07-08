// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.AnimationRigging.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting class that inherits from OVRUnityHumanoidSkeletonRetargeter and provides
    /// functions that work with animation rigging.
    /// </summary>
    [DefaultExecutionOrder(220)]
    public partial class RetargetingLayer : OVRUnityHumanoidSkeletonRetargeter
    {
        /// <summary>
        /// Callback type, auditable and assignable in the Unity Editor.
        /// Also assignable in code using
        /// <see cref="UnityEditor.Events.UnityEventTools.AddPersistentListener"/>
        /// </summary>
        [System.Serializable]
        public class SkeletonPostProcessingEvent : UnityEngine.Events.UnityEvent<IList<OVRBone>>
        {
        }

        /// <summary>
        /// Joint position adjustment to be applied to corrected positions.
        /// </summary>
        [System.Serializable]
        public class JointPositionAdjustment
        {
            /// <summary>
            /// Joint to adjust.
            /// </summary>
            public HumanBodyBones Joint;

            /// <summary>
            /// The original position, post-retargeting but before any other animation constraints.
            /// </summary>
            public Vector3 OriginalPosition;

            /// <summary>
            /// The final position, post-animation constraints.
            /// </summary>
            public Vector3 FinalPosition;

            /// <summary>
            /// Get the difference between the original and final positions.
            /// </summary>
            /// <returns>Position offset between the original and final positions.</returns>
            public Vector3 GetPositionOffset()
            {
                var targetPositionOffset = FinalPosition - OriginalPosition;
                // The recorded positions will not be finite when we regenerate data for the rig.
                if (!float.IsFinite(FinalPosition.x) ||
                    !float.IsFinite(FinalPosition.y) ||
                    !float.IsFinite(FinalPosition.z) ||
                    !float.IsFinite(OriginalPosition.x) ||
                    !float.IsFinite(OriginalPosition.y) ||
                    !float.IsFinite(OriginalPosition.z))
                {
                    return Vector3.zero;
                }
                return targetPositionOffset;
            }
        }

        private static readonly Dictionary<HumanBodyBones, AvatarMaskBodyPart>
            _humanBoneToAvatarBodyPart = new Dictionary<HumanBodyBones, AvatarMaskBodyPart>()
            {
                { HumanBodyBones.Neck, AvatarMaskBodyPart.Head },

                { HumanBodyBones.Head, AvatarMaskBodyPart.Head },
                { HumanBodyBones.LeftEye, AvatarMaskBodyPart.Head },
                { HumanBodyBones.RightEye, AvatarMaskBodyPart.Head },
                { HumanBodyBones.Jaw, AvatarMaskBodyPart.Head },

                { HumanBodyBones.Hips, AvatarMaskBodyPart.Body },

                { HumanBodyBones.Spine, AvatarMaskBodyPart.Body },
                { HumanBodyBones.Chest, AvatarMaskBodyPart.Body },
                { HumanBodyBones.UpperChest, AvatarMaskBodyPart.Body },

                { HumanBodyBones.RightShoulder, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightUpperArm, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLowerArm, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightHand, AvatarMaskBodyPart.RightArm },

                { HumanBodyBones.LeftShoulder, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftUpperArm, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLowerArm, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftHand, AvatarMaskBodyPart.LeftArm },

                { HumanBodyBones.LeftUpperLeg, AvatarMaskBodyPart.LeftLeg },
                { HumanBodyBones.LeftLowerLeg, AvatarMaskBodyPart.LeftLeg },

                { HumanBodyBones.LeftFoot, AvatarMaskBodyPart.LeftLeg },
                { HumanBodyBones.LeftToes, AvatarMaskBodyPart.LeftLeg },

                { HumanBodyBones.RightUpperLeg, AvatarMaskBodyPart.RightLeg },
                { HumanBodyBones.RightLowerLeg, AvatarMaskBodyPart.RightLeg },

                { HumanBodyBones.RightFoot, AvatarMaskBodyPart.RightLeg },
                { HumanBodyBones.RightToes, AvatarMaskBodyPart.RightLeg },

                { HumanBodyBones.LeftThumbProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftThumbIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftThumbDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleDistal, AvatarMaskBodyPart.LeftArm },

                { HumanBodyBones.RightThumbProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightThumbIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightThumbDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleDistal, AvatarMaskBodyPart.RightArm }
            };

        /// <summary>
        /// The array of joint position adjustments.
        /// </summary>
        public JointPositionAdjustment[] JointPositionAdjustments
        {
            get;
            private set;
        }

        /// <summary>
        /// Disable the target's avatar after building its meta data. There is an
        /// issue in Unity where the positions of all of the retargeted's
        /// bones are *not* set if an avatar is assigned during runtime, even if
        /// the avatar's "Translation DoF" checkbox is checked (specifically the fingers). The drawback of
        /// disabling an avatar is that animations or animation rig constraints
        /// might not run on the character. <see cref="_positionsToCorrectLateUpdate"/>
        /// can be used to keep the avatar and also correct finger positions in LateUpdate.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.DisableAvatar)]
        protected bool _disableAvatar = false;
        /// <summary>
        /// Accessors for disable avatar toggle.
        /// </summary>
        public bool DisableAvatar
        {
            get { return _disableAvatar; }
            set { _disableAvatar = value; }
        }

        /// <summary>
        /// Positions to correct after the fact. Avatar
        /// masks prevent setting positions of the hands precisely.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingLayerTooltips.PositionsToCorrectLateUpdate)]
        protected AvatarMask _positionsToCorrectLateUpdate;

        /// <summary>
        /// Don't allow changing the original field directly, as that
        /// has a side-effect of modifying the original mask object.
        /// </summary>
        private AvatarMask _positionsToCorrectLateUpdateInstance;
        /// <summary>
        /// Positions to correct accessors.
        /// </summary>
        public AvatarMask PositionsToCorrectLateUpdateComp
        {
            get { return _positionsToCorrectLateUpdateInstance; }
            set { _positionsToCorrectLateUpdateInstance = value; }
        }

        /// <summary>
        /// Apply position offsets done by animation rigging constraints for corrected
        /// positions. Due to the limited motion of humanoid avatars, this should be set if any
        /// animation rigging constraints are applied after the retargeting job runs.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.ApplyAnimationConstraintsToCorrectedPositions)]
        protected bool _applyAnimationConstraintsToCorrectedPositions = true;
        /// <inheritdoc cref="_applyAnimationConstraintsToCorrectedPositions"/>
        public bool ApplyAnimationConstraintsToCorrectedPositions
        {
            get { return _applyAnimationConstraintsToCorrectedPositions; }
            set { _applyAnimationConstraintsToCorrectedPositions = value; }
        }

        /// <summary>
        /// Since some bones are not affected by retargeting,
        /// some joints should be reset to t-pose.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingLayerTooltips.MaskToSetToTPose)]
        protected AvatarMask _maskToSetToTPose;

        /// <summary>
        /// Don't allow changing the original field directly, as that
        /// has a side-effect of modifying the original mask object.
        /// </summary>
        private AvatarMask _maskToSetToTPoseInstance;
        /// <summary>
        /// Mask to set to TPose accessors.
        /// </summary>
        public AvatarMask MaskToSetToTPoseComp
        {
            get { return _maskToSetToTPoseInstance; }
            set { _maskToSetToTPoseInstance = value; }
        }

        /// <summary>
        /// Create proxy transforms that track the skeletal bones. If the
        /// skeletal bone transforms change, that won't necessitate creating new
        /// proxy transforms in most cases. This means any Animation jobs
        /// that track the skeletal bone transform can use proxies
        /// instead, which get re-allocated less often. Re-allocation would mean
        /// having to create new animation jobs.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.EnableTrackingByProxy)]
        protected bool _enableTrackingByProxy = false;
        /// <inheritdoc cref="_enableTrackingByProxy"/>
        public bool EnableTrackingByProxy
        {
            get { return _enableTrackingByProxy; }
            set { _enableTrackingByProxy = value; }
        }

        /// <summary>
        /// Triggers methods that can alter bone translations and rotations, before rendering and physics
        /// </summary>
        [SerializeField, Optional]
        protected SkeletonPostProcessingEvent SkeletonPostProcessing;
        public SkeletonPostProcessingEvent SkeletonPostProcessingEv
        {
            get { return SkeletonPostProcessing; }
            set { SkeletonPostProcessing = value; }
        }

        private Pose[] _defaultPoses;
        private IJointConstraint[] _jointConstraints;
        private ProxyTransformLogic _proxyTransformLogic = new ProxyTransformLogic();

        /// <summary>
        /// Triggered if proxy transforms were recreated.
        /// </summary>
        public int ProxyChangeCount => _proxyTransformLogic.ProxyChangeCount;

        /// <summary>
        /// Allows one to specify which positions to correct during late update.
        /// This is ANDed with <see cref="_positionsToCorrectLateUpdateInstance"/>
        /// </summary>
        public AvatarMask CustomPositionsToCorrectLateUpdateMask { get; set; }

        private bool _isFocusedWhileInBuild = true;

        /// <summary>
        /// Gets number of transforms being retargeted currently. This can change during
        /// initialization.
        /// </summary>
        /// <returns>Number of transforms with a valid correction quaternion.</returns>
        public int GetNumberOfTransformsRetargeted()
        {
            int numTransforms = 0;
            // return default case if this is called before initialization.
            if (TargetSkeletonData == null || TargetSkeletonData.BodyToBoneData == null)
            {
                return numTransforms;
            }
            foreach (var boneData in TargetSkeletonData.BodyToBoneData.Values)
            {
                if (boneData.CorrectionQuaternion != null)
                {
                    numTransforms++;
                }
            }
            return numTransforms;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_positionsToCorrectLateUpdateInstance == null)
            {
                CreatePositionsToCorrectLateUpdateMaskInstance();
            }
            if (_maskToSetToTPoseInstance == null)
            {
                CreateTPoseMaskInstance();
            }
        }

        /// <summary>
        /// Allows creating instance of position correction mask used in this class at any time.
        /// Effectively resets animation masks being used to what the corresponding
        /// field <see cref="_positionsToCorrectLateUpdate"/> specify. This is primarily used by
        /// <see cref="RetargetingLayerEditor"/>.
        /// </summary>
        public void CreatePositionsToCorrectLateUpdateMaskInstance()
        {
            if (_positionsToCorrectLateUpdate != null)
            {
                _positionsToCorrectLateUpdateInstance = new AvatarMask();
                _positionsToCorrectLateUpdateInstance.CopyOtherMaskBodyActiveValues(
                    _positionsToCorrectLateUpdate);
            }
            else
            {
                _positionsToCorrectLateUpdateInstance = null;
            }
        }

        /// <summary>
        /// Allows creating instance of T-Pose mask used in this class at any time.
        /// Effectively resets animation masks being used to what the corresponding
        /// field <see cref="_maskToSetToTPose"/> specify. This is primarily used by
        /// <see cref="RetargetingLayerEditor"/>.
        /// </summary>
        public void CreateTPoseMaskInstance()
        {
            if (_maskToSetToTPose != null)
            {
                _maskToSetToTPoseInstance = new AvatarMask();
                _maskToSetToTPoseInstance.CopyOtherMaskBodyActiveValues(_maskToSetToTPose);
            }
            else
            {
                _maskToSetToTPoseInstance = null;
            }
        }

        /// <summary>
        /// Allows creating instances of masks used in this class at any time.
        /// Effectively resets animation masks being used to what the corresponding
        /// fields <see cref="_positionsToCorrectLateUpdate"/> and
        /// <see cref="_maskToSetToTPose"/> specify. This is primarily used by
        /// <see cref="RetargetingLayerEditor"/>.
        /// </summary>
        public void CreateMaskInstances()
        {
            CreatePositionsToCorrectLateUpdateMaskInstance();
            CreateTPoseMaskInstance();
        }

        /// <summary>
        /// Initialize base class and also any variables required by this class,
        /// such as the positions and rotations of the character joints at rest pose.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            ConstructDefaultPoseInformation();
            ConstructBoneAdjustmentInformation();
            CacheJointConstraints();

            ValidateHumanoid();
        }

        private void ConstructDefaultPoseInformation()
        {
            _defaultPoses = new Pose[(int)HumanBodyBones.LastBone];
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var boneTransform = AnimatorTargetSkeleton.GetBoneTransform(i);
                if (boneTransform == null)
                {
                    continue;
                }

                _defaultPoses[(int)i] = new Pose(boneTransform.localPosition,
                    boneTransform.localRotation);
            }
        }

        private void ConstructBoneAdjustmentInformation()
        {
            JointPositionAdjustments = new JointPositionAdjustment[(int)HumanBodyBones.LastBone];
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                JointPositionAdjustments[(int)i] = new JointPositionAdjustment { Joint = i };
            }
        }

        private void CacheJointConstraints()
        {
            var positionConstraints = AnimatorTargetSkeleton.GetComponentsInChildren<PositionConstraint>();
            List<IJointConstraint> jointConstraints = new List<IJointConstraint>();
            for (int i = 0; i < positionConstraints.Length; i++)
            {
                if (positionConstraints[i].constraintActive)
                {
                    var jointConstraint = new PositionalJointConstraint(positionConstraints[i].transform);
                    jointConstraints.Add(jointConstraint);
                }
            }
            _jointConstraints = jointConstraints.ToArray();
        }

        private void ValidateHumanoid()
        {
            bool validHumanoid = true;
            foreach(var bodyBone in CustomBoneIdToHumanBodyBone.Values)
            {
                if (!AnimatorTargetSkeleton.GetBoneTransform(bodyBone))
                {
                    Debug.LogError($"Did not find {bodyBone} in {AnimatorTargetSkeleton}.");
                    validHumanoid = false;
                }
            }

            if (!validHumanoid)
            {
                return;
            }

            // specific checks follow.
            var upperChest = AnimatorTargetSkeleton.GetBoneTransform(HumanBodyBones.UpperChest);
            var leftShoulder = AnimatorTargetSkeleton.GetBoneTransform(HumanBodyBones.LeftShoulder);
            var rightShoulder = AnimatorTargetSkeleton.GetBoneTransform (HumanBodyBones.RightShoulder);

            if (leftShoulder.parent != upperChest)
            {
                Debug.LogWarning($"In the ideal case, the parent of left shoulder ({leftShoulder}) should be the" +
                    $" upper chest ({upperChest}).");
            }
            if (rightShoulder.parent != upperChest)
            {
                Debug.LogWarning($"In the ideal case, the parent of right shoulder ({rightShoulder}) should be the" +
                    $" upper chest ({upperChest}).");
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (Application.isEditor)
            {
                return;
            }
            _isFocusedWhileInBuild = hasFocus;
        }

        /// <inheritdoc />
        protected override void Update()
        {
            DisableAvatarIfNecessary();

            UpdateSkeleton();
            SkeletonPostProcessing?.Invoke(Bones);
            RecomputeSkeletalOffsetsIfNecessary();

            if (_enableTrackingByProxy)
            {
                _proxyTransformLogic.UpdateState(Bones);
            }
        }

        /// <summary>
        /// Allows fixing joints to T-pose. The avatar does not allow
        /// precise finger positions even with translate dof checked.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!_isFocusedWhileInBuild)
            {
                return;
            }
            CorrectPositions();
            FixJointsToTPose();
            // apply constraints on character after fixing positions.
            RunConstraints();
        }

        private void CorrectPositions()
        {
            if (_positionsToCorrectLateUpdateInstance == null)
            {
                return;
            }

            for (var i = 0; i < Bones.Count; i++)
            {
                if (!CustomBoneIdToHumanBodyBone.TryGetValue(Bones[i].Id, out var humanBodyBone))
                {
                    continue;
                }

                if (!TargetSkeletonData.BodyToBoneData.TryGetValue(humanBodyBone, out var targetData))
                {
                    continue;
                }

                // Skip if we cannot map the joint at all.
                if (!targetData.CorrectionQuaternion.HasValue)
                {
                    continue;
                }

                var bodyPart = _humanBoneToAvatarBodyPart[humanBodyBone];
                if (!_positionsToCorrectLateUpdateInstance.GetHumanoidBodyPartActive(bodyPart) ||
                    (CustomPositionsToCorrectLateUpdateMask != null &&
                     !CustomPositionsToCorrectLateUpdateMask.GetHumanoidBodyPartActive(bodyPart))
                   )
                {
                    continue;
                }

                var adjustment = FindAdjustment(humanBodyBone);
                var targetJoint = targetData.OriginalJoint;
                var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
                var shouldUpdatePosition = IsBodySectionInArray(
                    bodySectionOfJoint, BodySectionToPosition);

                if (!shouldUpdatePosition)
                {
                    continue;
                }

                var positionOffset = _applyAnimationConstraintsToCorrectedPositions ?
                    JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset() : Vector3.zero;

                if (adjustment == null)
                {
                    targetJoint.position = Bones[i].Transform.position + positionOffset;
                }
                else
                {
                    if (!adjustment.DisablePositionTransform)
                    {
                        targetJoint.position = Bones[i].Transform.position + positionOffset;
                    }
                }
            }
        }

        private void FixJointsToTPose()
        {
            if (_maskToSetToTPoseInstance == null)
            {
                return;
            }

            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var boneTransform = AnimatorTargetSkeleton.GetBoneTransform(i);
                if (boneTransform == null)
                {
                    continue;
                }
                if (!_maskToSetToTPoseInstance.GetHumanoidBodyPartActive(_humanBoneToAvatarBodyPart[i]))
                {
                    continue;
                }
                var defaultPose = _defaultPoses[(int)i];
                boneTransform.SetLocalPositionAndRotation(defaultPose.position,
                  defaultPose.rotation);
            }
        }

        private void RunConstraints()
        {
            if (_jointConstraints == null || _jointConstraints.Length == 0)
            {
                return;
            }
            for (int i = 0; i < _jointConstraints.Length; i++)
            {
                var constraint = _jointConstraints[i];
                constraint.Update();
            }
        }

        private void DisableAvatarIfNecessary()
        {
            // target meta data not created yet, can't disable avatar
            if (TargetSkeletonData == null)
            {
                return;
            }

            if (_disableAvatar && AnimatorTargetSkeleton.avatar != null)
            {
                AnimatorTargetSkeleton.avatar = null;
            }
        }

        /// <summary>
        /// Fills transform lists with meta data.
        /// </summary>
        /// <param name="sourceTransforms">Source transforms.</param>
        /// <param name="targetTransforms">Target transforms.</param>
        /// <param name="shouldUpdatePositions">If joint positions should be updated or not.</param>
        /// <param name="shouldUpdateRotations">If joint rotations should be updated or not.</param>
        /// <param name="rotationOffsets">Rotation offset per joint.</param>
        /// <param name="rotationAdjustments">Rotation tweak per joint.</param>
        public void FillTransformArrays(List<Transform> sourceTransforms,
            List<Transform> targetTransforms, List<bool> shouldUpdatePositions,
            List<bool> shouldUpdateRotations, List<Quaternion> rotationOffsets,
            List<Quaternion> rotationAdjustments)
        {
            var skeletalBones = Bones;
            int numBones = skeletalBones.Count;
            var targetBoneDataMap = TargetSkeletonData.BodyToBoneData;
            for (int i = 0; i < numBones; i++)
            {
                var currentBone = skeletalBones[i];
                HumanBodyBones targetHumanBodyBone;
                OVRSkeletonMetadata.BoneData targetBoneData;

                (targetBoneData, targetHumanBodyBone) =
                    GetTargetBoneDataFromOVRBone(skeletalBones[i], targetBoneDataMap);
                if (targetBoneData == null)
                {
                    continue;
                }

                // Skip if we can't map the joint at all.
                if (!targetBoneData.CorrectionQuaternion.HasValue)
                {
                    continue;
                }

                sourceTransforms.Add(_enableTrackingByProxy ?
                    _proxyTransformLogic.ProxyTransforms[i].DrivenTransform :
                    currentBone.Transform);
                targetTransforms.Add(targetBoneData.OriginalJoint);
                shouldUpdatePositions.Add(false);
                shouldUpdateRotations.Add(false);
                rotationOffsets.Add(targetBoneData.CorrectionQuaternion.Value);
                rotationAdjustments.Add(Quaternion.identity);
            }
        }

        /// <summary>
        /// Update adjustment arrays.
        /// </summary>
        /// <param name="rotationOffsets">Rotation offset per joint.</param>
        /// <param name="shouldUpdatePositions">If joint positions should be updated or not.</param>
        /// <param name="shouldUpdateRotations">If joint rotations should be updated or not.</param>
        /// <param name="rotationAdjustments">Rotation tweak per joint</param>
        /// <param name="avatarMask">Mask to restrict retargeting.</param>
        public void UpdateAdjustments(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, bool[] shouldUpdateRotations,
            Quaternion[] rotationAdjustments, AvatarMask avatarMask)
        {
            var skeletalBones = Bones;
            int numBones = skeletalBones.Count;
            var targetBoneDataMap = TargetSkeletonData.BodyToBoneData;
            int arrayId = 0;
            for (int i = 0; i < numBones; i++)
            {
                HumanBodyBones targetHumanBodyBone;
                OVRSkeletonMetadata.BoneData targetBoneData;

                var currBone = skeletalBones[i];
                (targetBoneData, targetHumanBodyBone) =
                    GetTargetBoneDataFromOVRBone(currBone, targetBoneDataMap);
                if (targetBoneData == null)
                {
                    continue;
                }

                // Skip if no bones not found between skeletons, which means
                // quaternion is null.
                if (!targetBoneData.CorrectionQuaternion.HasValue)
                {
                    continue;
                }

                // run this code each frame to pick up adjustments made to the editor
                var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[targetHumanBodyBone];

                var adjustment = FindAdjustment(targetHumanBodyBone);
                bool bodySectionInPositionArray = IsBodySectionInArray(
                    bodySectionOfJoint, BodySectionToPosition);

                // Skip if the job arrays are less in number compared to bones.
                // This can happen if the skeleton regenerates its bones during update,
                // however the arrays here have not been recreated yet. Note that the arrays
                // are effectively recreated when AnimationRigSetup disables and re-enables the
                // rig. Since AnimationRigSetup runs after skeletal updates, this edge case
                // arises if this function is called after the bones are updated but before
                // AnimationRigSetup notices.
                if (arrayId >= rotationAdjustments.Length)
                {
                    continue;
                }

                bool jointFailsMask = false;
                if (avatarMask != null)
                {
                    jointFailsMask = !avatarMask.GetHumanoidBodyPartActive(
                        _humanBoneToAvatarBodyPart[targetHumanBodyBone]);
                }

                if (adjustment == null)
                {
                    SetUpDefaultAdjustment(rotationOffsets, shouldUpdatePositions,
                        shouldUpdateRotations, rotationAdjustments, arrayId,
                        targetBoneData, bodySectionInPositionArray,
                        jointFailsMask);
                }
                else
                {
                    SetUpCustomAdjustment(rotationOffsets, shouldUpdatePositions,
                        shouldUpdateRotations,
                        rotationAdjustments, adjustment, arrayId,
                        targetBoneData, bodySectionInPositionArray,
                        jointFailsMask);
                }

                arrayId++;
            }
        }

        private void SetUpDefaultAdjustment(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, bool[] shouldUpdateRotations,
            Quaternion[] rotationAdjustments, int arrayId,
            OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray, bool jointFailsMask)
        {
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value;
            shouldUpdatePositions[arrayId] = !jointFailsMask && bodySectionInPositionArray;
            shouldUpdateRotations[arrayId] = !jointFailsMask;
            rotationAdjustments[arrayId] = Quaternion.identity;
        }

        private void SetUpCustomAdjustment(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, bool[] shouldUpdateRotations,
            Quaternion[] rotationAdjustments,
            JointAdjustment adjustment, int arrayId,
            OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray, bool jointFailsMask)
        {
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value;
            shouldUpdatePositions[arrayId] =
                !adjustment.DisablePositionTransform &&
                bodySectionInPositionArray &&
                !jointFailsMask;
            shouldUpdateRotations[arrayId] =
                !adjustment.DisableRotationTransform &&
                !jointFailsMask;
            rotationAdjustments[arrayId] = adjustment.RotationChange;
        }

        private (OVRSkeletonMetadata.BoneData, HumanBodyBones) GetTargetBoneDataFromOVRBone(OVRBone ovrBone,
            Dictionary<HumanBodyBones, OVRSkeletonMetadata.BoneData> targetBodyToBoneData, bool print = false)
        {
            var skelBoneId = ovrBone.Id;
            if (!CustomBoneIdToHumanBodyBone.TryGetValue(skelBoneId, out var humanBodyBone))
            {
                return (null, HumanBodyBones.LastBone);
            }

            if (!targetBodyToBoneData.TryGetValue(humanBodyBone, out var targetData))
            {
                return (null, HumanBodyBones.LastBone);
            }

            return (targetData, humanBodyBone);
        }
    }
}
