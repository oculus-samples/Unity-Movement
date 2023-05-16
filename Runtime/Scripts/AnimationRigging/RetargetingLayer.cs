// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
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
        /// Since some bones are not affected by retargeting,
        /// some joints should be reset to t-pose.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingLayerTooltips.MaskToSetToTPose)]
        protected AvatarMask _maskToSetToTPose;

        private Pose[] _defaultPoses;
        private IJointConstraint[] _jointConstraints;

        /// <summary>
        /// Gets number of transforms being retargeted currently.
        /// </summary>
        /// <returns>Number of transforms with a valid correction quaternion.</returns>
        public int GetNumberOfTransformsRetargeted()
        {
            int numTransforms = 0;
            foreach (var boneData in TargetSkeletonData.BodyToBoneData.Values)
            {
                if (boneData.CorrectionQuaternion != null)
                {
                    numTransforms++;
                }
            }
            return numTransforms;
        }

        /// <summary>
        /// Initialize base class and also any variables required by this class,
        /// such as the positions and rotations of the character joints at rest pose.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            ConstructDefaultPoseInformation();
            CacheJointConstraints();
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

        /// <inheritdoc />
        protected override void Update()
        {
            DisableAvatarIfNecessary();

            UpdateSkeleton();

            RecomputeSkeletalOffsetsIfNecessary();
        }

        /// <summary>
        /// Allows fixing joints to T-pose. The avatar does not allow
        /// precise finger positions even with translate dof checked.
        /// </summary>
        protected virtual void LateUpdate()
        {
            CorrectPositions();
            FixJointsToTPose();
            // apply constraints on character after fixing positions.
            RunConstraints();
        }

        private void CorrectPositions()
        {
            if (_positionsToCorrectLateUpdate == null)
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
                if (_positionsToCorrectLateUpdate.GetHumanoidBodyPartActive(bodyPart))
                {
                    var adjustment = FindAdjustment(humanBodyBone);
                    var targetJoint = targetData.OriginalJoint;
                    var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
                    var shouldUpdatePosition = IsBodySectionInArray(
                        bodySectionOfJoint, BodySectionToPosition);

                    if (adjustment == null)
                    {
                        if (shouldUpdatePosition)
                        {
                            targetJoint.position = Bones[i].Transform.position;
                        }
                    }
                    else
                    {
                        if (!adjustment.DisablePositionTransform && shouldUpdatePosition)
                        {
                            targetJoint.position = Bones[i].Transform.position;
                        }
                    }
                }
            }
        }

        private void FixJointsToTPose()
        {
            if (_maskToSetToTPose == null)
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
                if (!_maskToSetToTPose.GetHumanoidBodyPartActive(_humanBoneToAvatarBodyPart[i]))
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
            for(int i = 0; i < _jointConstraints.Length; i++)
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
        /// <param name="rotationOffsets">Rotation offset per joint.</param>
        /// <param name="rotationAdjustments">Rotation tweak per joint.</param>
        /// <param name="avatarMask">Mask to restrict retargeting.</param>
        public void FillTransformArrays(List<Transform> sourceTransforms,
            List<Transform> targetTransforms, List<bool> shouldUpdatePositions,
            List<Quaternion> rotationOffsets, List<Quaternion> rotationAdjustments,
            AvatarMask avatarMask)
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

                if (avatarMask != null)
                {
                    // We can support runtime mask changes in future, but for that to work
                    // we must undo any position and rotation updates done to joints that
                    // are retargeted to once a mask change indicates that they must no
                    // longer be retargeted.
                    var jointInMask = avatarMask.GetHumanoidBodyPartActive(
                    _humanBoneToAvatarBodyPart[targetHumanBodyBone]);
                    if (!jointInMask)
                    {
                        continue;
                    }
                }

                sourceTransforms.Add(currentBone.Transform);
                targetTransforms.Add(targetBoneData.OriginalJoint);
                shouldUpdatePositions.Add(false);
                rotationOffsets.Add(targetBoneData.CorrectionQuaternion.Value);
                rotationAdjustments.Add(Quaternion.identity);
            }
        }

        /// <summary>
        /// Update adjustment arrays.
        /// </summary>
        /// <param name="rotationOffsets">Rotation offset per joint.</param>
        /// <param name="shouldUpdatePositions">If joint positions should be updated or not.</param>
        /// <param name="rotationAdjustments">Rotation tweak per joint</param>
        /// <param name="avatarMask">Mask to restrict retargeting.</param>
        public void UpdateAdjustments(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, Quaternion[] rotationAdjustments,
            AvatarMask avatarMask)
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

                if (avatarMask != null)
                {
                    var jointInMask = avatarMask.GetHumanoidBodyPartActive(
                        _humanBoneToAvatarBodyPart[targetHumanBodyBone]);
                    if (!jointInMask)
                    {
                        continue;
                    }
                }

                // run this code each frame to pick up adjustments made to the editor
                var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[targetHumanBodyBone];

                var adjustment = FindAdjustment(targetHumanBodyBone);
                bool bodySectionInPositionArray = IsBodySectionInArray(
                    bodySectionOfJoint, BodySectionToPosition);
                if (adjustment == null)
                {
                    SetUpDefaultAdjustment(rotationOffsets, shouldUpdatePositions,
                        rotationAdjustments, arrayId,
                        targetBoneData, bodySectionInPositionArray);
                }
                else
                {
                    SetUpCustomAdjustment(rotationOffsets, shouldUpdatePositions,
                        rotationAdjustments, adjustment, arrayId,
                        targetBoneData, bodySectionInPositionArray);
                }

                arrayId++;
            }
        }

        private void SetUpDefaultAdjustment(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, Quaternion[] rotationAdjustments,
            int arrayId, OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray)
        {
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value;
            shouldUpdatePositions[arrayId] = bodySectionInPositionArray;
            rotationAdjustments[arrayId] = Quaternion.identity;
        }

        private void SetUpCustomAdjustment(Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, Quaternion[] rotationAdjustments,
            JointAdjustment adjustment, int arrayId,
            OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray)
        {
            rotationOffsets[arrayId] = adjustment.DisableRotationTransform ?
                Quaternion.identity :
                targetBoneData.CorrectionQuaternion.Value;
            shouldUpdatePositions[arrayId] =
                !adjustment.DisablePositionTransform &&
                bodySectionInPositionArray;
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
