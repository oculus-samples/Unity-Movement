// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.AnimationRigging.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting class that inherits from OVRUnityHumanoidSkeletonRetargeter and provides
    /// functions that work with animation rigging.
    /// </summary>
    [DefaultExecutionOrder(220)]
    public partial class RetargetingLayer : OVRUnityHumanoidSkeletonRetargeter,
        IOVRSkeletonProcessorAggregator
    {
        /// <summary>
        /// Callback that can adjust a given skeleton. Is the functional backend that implements
        /// <see cref="IOVRSkeletonProcessorAggregator"/>
        /// </summary>
        /// <param name="skeleton"></param>
        public delegate void OVRSkeletonProcessor(OVRSkeleton skeleton);

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
                if (!RiggingUtilities.IsFiniteVector3(FinalPosition) ||
                    !RiggingUtilities.IsFiniteVector3(OriginalPosition))
                {
                    return Vector3.zero;
                }
                return targetPositionOffset;
            }
        }

        /// <summary>
        /// Allows one to adjust the per-joint rotation offsets computed between
        /// source (OVRBody) and target characters. To avoid gimbal lock,
        /// a series of rotations are permitted.
        /// </summary>
        [System.Serializable]
        public class JointRotationTweaks
        {
            /// <summary>
            /// Joint to affect.
            /// </summary>
            public HumanBodyBones Joint;

            /// <summary>
            /// A series of rotation tweaks.
            /// </summary>
            public Quaternion[] RotationTweaks;
        }

        /// <summary>
        /// The array of joint position adjustments.
        /// </summary>
        public JointPositionAdjustment[] JointPositionAdjustments
        {
            get;
            private set;
        }

        /// <summary>
        /// Allows correcting positions in LateUpdate for accuracy.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.CorrectPositionsLateUpdate)]
        protected bool _correctPositionsLateUpdate = true;

        /// <summary>
        /// Allow correcting rotations in LateUpdate. This can produce more
        /// accurate hands, for instance.
        /// </summary>
        [Tooltip(RetargetingLayerTooltips.LeftHandCorrectionWeightLateUpdate)]
        [SerializeField, Range(0.0f, 1.0f)]
        protected float _leftHandCorrectionWeightLateUpdate = 1.0f;
        public float LeftHandCorrectionWeightLateUpdate
        {
            get => _leftHandCorrectionWeightLateUpdate;
            set => _leftHandCorrectionWeightLateUpdate = value;
        }

        /// <summary>
        /// Allow correcting rotations in LateUpdate. This can produce more
        /// accurate hands, for instance.
        /// </summary>
        [Tooltip(RetargetingLayerTooltips.RightHandCorrectionWeightLateUpdate)]
        [SerializeField, Range(0.0f, 1.0f)]
        protected float _rightHandCorrectionWeightLateUpdate = 1.0f;
        public float RightHandCorrectionWeightLateUpdate
        {
            get => _rightHandCorrectionWeightLateUpdate;
            set => _rightHandCorrectionWeightLateUpdate = value;
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
        protected OVRSkeletonProcessor SkeletonPostProcessing;
        public OVRSkeletonProcessor SkeletonPostProcessingEv
        {
            get { return SkeletonPostProcessing; }
            set { SkeletonPostProcessing = value; }
        }

        /// <summary>
        /// Related retargeting constraint.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.RetargetingAnimationContraint)]
        protected RetargetingAnimationConstraint _retargetingAnimationConstraint;
        public RetargetingAnimationConstraint RetargetingConstraint
        {
            get { return _retargetingAnimationConstraint; }
            set { _retargetingAnimationConstraint = value; }
        }

        /// <summary>
        /// Joint rotation tweaks array.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.JointRotationTweaks)]
        protected JointRotationTweaks[] _jointRotationTweaks;
        public JointRotationTweaks[] JointRotationTweaksArray
        {
            get => _jointRotationTweaks;
            set => _jointRotationTweaks = value;
        }
        /// <summary>
        /// Pre-compute these values each time the editor changes for the purposes
        /// of efficiency.
        /// </summary>
        private Dictionary<HumanBodyBones, Quaternion> _humanBoneToAccumulatedRotationTweaks =
            new Dictionary<HumanBodyBones, Quaternion>();
        private List<HumanBodyBones> _bonesToRemove = new List<HumanBodyBones> ();

        private Pose[] _defaultPoses;
        private IJointConstraint[] _jointConstraints;
        private ProxyTransformLogic _proxyTransformLogic = new ProxyTransformLogic();

        /// <summary>
        /// Triggered if proxy transforms were recreated.
        /// </summary>
        public int ProxyChangeCount => _proxyTransformLogic.ProxyChangeCount;

        /// <summary>
        /// Allows one to specify which positions to correct during late update.
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

            Assert.IsNotNull(_retargetingAnimationConstraint,
                "Please assign the retargeting constraint to RetargetingLayer.");
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

            PrecomputeJointRotationTweaks();
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
            foreach (var bodyBone in CustomBoneIdToHumanBodyBone.Values)
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
            var rightShoulder = AnimatorTargetSkeleton.GetBoneTransform(HumanBodyBones.RightShoulder);

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

        protected virtual void OnValidate()
        {
            PrecomputeJointRotationTweaks();
        }

        /// <summary>
        /// When the object's properties are modified, accumulate joint rotations
        /// and cache those values. This saves on computation when the tweaks field is used.
        /// </summary>
        protected void PrecomputeJointRotationTweaks()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif
            if (_jointRotationTweaks == null || _jointRotationTweaks.Length == 0)
            {
                return;
            }

            foreach (var rotationTweak in _jointRotationTweaks)
            {
                var allRotations = rotationTweak.RotationTweaks;
                var joint = rotationTweak.Joint;

                Quaternion accumulatedRotation = Quaternion.identity;
                foreach (var rotationValue in allRotations)
                {
                    // Make sure the quaternion is valid. Quaternions are initialized to all
                    // zeroes by default, which makes them invalid.
                    if (rotationValue.w < Mathf.Epsilon && rotationValue.x < Mathf.Epsilon &&
                        rotationValue.y < Mathf.Epsilon && rotationValue.z < Mathf.Epsilon)
                    {
                        continue;
                    }

                    accumulatedRotation *= rotationValue;
                }

                if (!_humanBoneToAccumulatedRotationTweaks.ContainsKey(joint))
                {
                    _humanBoneToAccumulatedRotationTweaks.Add(joint, accumulatedRotation);
                }
                else
                {
                    _humanBoneToAccumulatedRotationTweaks[joint] = accumulatedRotation;
                }
            }

            _bonesToRemove.Clear();
            // If the user removed bones from the UI, remove those from the dictionary.
            foreach (var bone in _humanBoneToAccumulatedRotationTweaks.Keys)
            {
                bool boneFound = false;
                foreach (var rotationTweak in _jointRotationTweaks)
                {
                    if (rotationTweak.Joint == bone)
                    {
                        boneFound = true;
                        break;
                    }
                }

                if (!boneFound)
                {
                    _bonesToRemove.Add(bone);
                }
            }
            foreach(var boneToRemove in _bonesToRemove)
            {
                _humanBoneToAccumulatedRotationTweaks.Remove(boneToRemove);
            }
        }

        /// <inheritdoc />
        protected override void Update()
        {
            UpdateSkeleton();
            SkeletonPostProcessing?.Invoke(this);
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
            CorrectBones();
            // apply constraints on character after fixing positions.
            RunConstraints();
        }

        private void CorrectBones()
        {
            bool handCorrectionTurnedOn = (_leftHandCorrectionWeightLateUpdate > Mathf.Epsilon) ||
                (_rightHandCorrectionWeightLateUpdate > Mathf.Epsilon);
            if (!_correctPositionsLateUpdate &&
                !handCorrectionTurnedOn)
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

                var bodyPart = CustomMappings.HumanBoneToAvatarBodyPart[humanBodyBone];
                var targetJoint = targetData.OriginalJoint;

                // Make sure body part passes mask, and bone's position should be updated.
                if (CustomPositionsToCorrectLateUpdateMask != null &&
                    !CustomPositionsToCorrectLateUpdateMask.GetHumanoidBodyPartActive(bodyPart))
                {
                    continue;
                }
                var adjustment = FindAdjustment(humanBodyBone);
                if (!ShouldUpdatePositionOfBone(humanBodyBone))
                {
                    continue;
                }

                var currentTargetPosition = targetJoint.position;
                // Make sure the joint position is valid before fixing it.
                if (!RiggingUtilities.IsFiniteVector3(currentTargetPosition))
                {
                    continue;
                }

                var positionOffset = _applyAnimationConstraintsToCorrectedPositions ?
                    JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset() : Vector3.zero;
                var currentOVRBonePosition = Bones[i].Transform.position;
                var errorRelativeToBodyTracking = (currentOVRBonePosition - currentTargetPosition).sqrMagnitude;

                // If generally correcting positions only and applying hand correction,
                // skip positional fix a) if the error relative to body tracking is low,
                // b) and the position influence due to IK fixes is small.
                if ((_correctPositionsLateUpdate && !handCorrectionTurnedOn) &&
                    errorRelativeToBodyTracking < Mathf.Epsilon &&
                    positionOffset.sqrMagnitude < Mathf.Epsilon)
                {
                    continue;
                }

                var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
                bool isLeftHandFingersOrWrist =
                    bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.LeftHand ||
                    humanBodyBone == HumanBodyBones.LeftHand;
                bool isRightHandFingersOrWrist =
                    bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.RightHand ||
                    humanBodyBone == HumanBodyBones.RightHand;
                bool isHandJoint = isLeftHandFingersOrWrist || isRightHandFingersOrWrist;

                // Pick the correct hand correction weight based on handedness.
                float handWeight = isLeftHandFingersOrWrist ?
                    _leftHandCorrectionWeightLateUpdate :
                    _rightHandCorrectionWeightLateUpdate;
                // Exclude any position offsets from IK if correcting hand joints to
                // what body tracking indicates they are.
                if (isHandJoint)
                {
                    positionOffset = Vector3.Lerp(positionOffset, Vector3.zero,
                        handWeight);
                }

                float rtWeight = _retargetingAnimationConstraint.weight;
                if (adjustment == null)
                {
                    if (handCorrectionTurnedOn && isHandJoint)
                    {
                        targetJoint.rotation =
                            Quaternion.Slerp(targetJoint.rotation,
                                Bones[i].Transform.rotation *
                                targetData.CorrectionQuaternion.Value * GetRotationTweak(humanBodyBone),
                                handWeight);
                    }
                    if (_correctPositionsLateUpdate)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + positionOffset, rtWeight);
                    }
                }
                else
                {
                    if (handCorrectionTurnedOn && isHandJoint)
                    {
                        if (!adjustment.DisableRotationTransform)
                        {
                            targetJoint.rotation =
                                Quaternion.Slerp(targetJoint.rotation,
                                    Bones[i].Transform.rotation *
                                    targetData.CorrectionQuaternion.Value * GetRotationTweak(humanBodyBone),
                                    handWeight);
                        }

                        targetJoint.rotation *= adjustment.RotationChange;
                    }
                    if (!adjustment.DisablePositionTransform &&
                        _correctPositionsLateUpdate)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + positionOffset, rtWeight);
                    }
                }
            }
        }

        protected Quaternion GetRotationTweak(HumanBodyBones humanBody)
        {
            Quaternion rotation;

            if (!_humanBoneToAccumulatedRotationTweaks.TryGetValue(humanBody, out rotation))
            {
                rotation = Quaternion.identity;
            }

            return rotation;
        }

        protected virtual bool ShouldUpdatePositionOfBone(HumanBodyBones humanBodyBone)
        {
            var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
            return IsBodySectionInArray(bodySectionOfJoint, BodySectionToPosition);
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
                rotationOffsets.Add(targetBoneData.CorrectionQuaternion.Value *
                    GetRotationTweak(targetHumanBodyBone));
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
                var adjustment = FindAdjustment(targetHumanBodyBone);
                bool bodySectionInPositionArray = ShouldUpdatePositionOfBone(targetHumanBodyBone);

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
                        CustomMappings.HumanBoneToAvatarBodyPart[targetHumanBodyBone]);
                }

                if (adjustment == null)
                {
                    SetUpDefaultAdjustment(targetHumanBodyBone,
                        rotationOffsets, shouldUpdatePositions,
                        shouldUpdateRotations, rotationAdjustments, arrayId,
                        targetBoneData, bodySectionInPositionArray,
                        jointFailsMask);
                }
                else
                {
                    SetUpCustomAdjustment(targetHumanBodyBone,
                        rotationOffsets, shouldUpdatePositions,
                        shouldUpdateRotations,
                        rotationAdjustments, adjustment, arrayId,
                        targetBoneData, bodySectionInPositionArray,
                        jointFailsMask);
                }

                arrayId++;
            }
        }

        private void SetUpDefaultAdjustment(HumanBodyBones humanBodyBone, Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, bool[] shouldUpdateRotations,
            Quaternion[] rotationAdjustments, int arrayId,
            OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray, bool jointFailsMask)
        {
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value *
                GetRotationTweak(humanBodyBone);
            shouldUpdatePositions[arrayId] = !jointFailsMask && bodySectionInPositionArray;
            shouldUpdateRotations[arrayId] = !jointFailsMask;
            rotationAdjustments[arrayId] = Quaternion.identity;
        }

        private void SetUpCustomAdjustment(HumanBodyBones humanBodyBone, Quaternion[] rotationOffsets,
            bool[] shouldUpdatePositions, bool[] shouldUpdateRotations,
            Quaternion[] rotationAdjustments,
            JointAdjustment adjustment, int arrayId,
            OVRSkeletonMetadata.BoneData targetBoneData,
            bool bodySectionInPositionArray, bool jointFailsMask)
        {
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value *
                GetRotationTweak(humanBodyBone);
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

        /// <inheritdoc/>
        public void AddProcessor(IOVRSkeletonProcessor processor)
        {
            SkeletonPostProcessing += processor.ProcessSkeleton;
        }

        /// <inheritdoc/>
        public void RemoveProcessor(IOVRSkeletonProcessor processor)
        {
            SkeletonPostProcessing -= processor.ProcessSkeleton;
        }
    }
}
