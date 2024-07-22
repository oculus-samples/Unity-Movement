// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Jobs;
using static Oculus.Movement.AnimationRigging.IRetargetingProcessor;
using static Oculus.Movement.Utils.JobCommons;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting processor used to apply corrected bone positions from the retargeted animation job output.
    /// </summary>
    [CreateAssetMenu(fileName = "Correct Bones", menuName = "Movement Samples/Data/Retargeting Processors/Correct Bones", order = 1)]
    public sealed class RetargetingProcessorCorrectBones : RetargetingProcessor
    {
        /// <summary>
        /// Meta data that can be updated without having to regenerate the job
        /// that uses it.
        /// </summary>
        [Serializable]
        private struct CorrectBonesMetadata
        {
            /// <summary>
            /// True if all tests (like masking) pass.
            /// </summary>
            public bool ShouldCorrectBone;

            /// <summary>
            /// Target body bone.
            /// </summary>
            public HumanBodyBones TargetBodyBone;

            /// <summary>
            /// Position weight.
            /// </summary>
            public float PosWeight;

            /// <summary>
            /// Rotation weight.
            /// </summary>
            public float RotWeight;

            /// <summary>
            /// Adjust position or not.
            /// </summary>
            public bool AdjustPosition;

            /// <summary>
            /// Adjust rotation or not.
            /// </summary>
            public bool AdjustRotation;

            /// <summary>
            /// Quaternion correction.
            /// </summary>
            public Quaternion CorrectionQuaternion;

            /// <summary>
            /// Position offset.
            /// </summary>
            public Vector3 PositionAdjustment;

            /// <summary>
            /// Offset related to constraint.
            /// </summary>
            public Vector3 ConstraintOffset;

            /// <summary>
            /// Rotation adjustment.
            /// </summary>
            public Quaternion RotationAdjustment;

            /// <summary>
            /// Is hand joint or not.
            /// </summary>
            public bool IsHandJoint;
        }

        /// <summary>
        /// Job that corrects bones.
        /// </summary>
        [Unity.Burst.BurstCompile]
        private struct CorrectBonesJob : IJobParallelForTransform
        {
            /// <summary>
            /// Body poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<Pose> BodyPoses;

            /// <summary>
            /// Allows updating jobs with new meta data.
            /// </summary>
            [ReadOnly]
            public NativeArray<CorrectBonesMetadata> Metadata;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                if (!transform.isValid)
                {
                    return;
                }
                var sourceBodyPose = BodyPoses[index];
                var metadata = Metadata[index];

                if (!metadata.ShouldCorrectBone)
                {
                    return;
                }

                Vector3 finalPosition = transform.position;
                Vector3 originalPosition = finalPosition;
                Quaternion finalRotation = transform.rotation;
                Quaternion originalRotation = finalRotation;
                if (metadata.AdjustPosition)
                {
                    finalPosition = Vector3.Lerp(originalPosition,
                        sourceBodyPose.position + metadata.PositionAdjustment + metadata.ConstraintOffset,
                        metadata.PosWeight);
                }
                if (metadata.AdjustRotation)
                {
                    var retargetedRotation = sourceBodyPose.rotation * metadata.CorrectionQuaternion;
                    retargetedRotation *= metadata.RotationAdjustment;
                    finalRotation = Quaternion.Slerp(originalRotation,
                        retargetedRotation, metadata.RotWeight);
                }

                transform.SetPositionAndRotation(finalPosition, finalRotation);
            }
        }

        /// <summary>
        /// Whether to use jobs or not.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.ProcessorType)]
        private RetargetingProcessorType _processorType =
            RetargetingProcessorType.Normal;
        /// <inheritdoc/>
        public override RetargetingProcessorType ProcessorType
        {
            get => _processorType;
            set => _processorType = value;
        }

        /// <summary>
        /// Allows correcting positions for accuracy.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.CorrectPositionsLateUpdate)]
        private bool _correctPositionsLateUpdate = true;
        /// <inheritdoc cref="_correctPositionsLateUpdate"/>
        public bool CorrectPositionsLateUpdate
        {
            get => _correctPositionsLateUpdate;
            set => _correctPositionsLateUpdate = value;
        }

        /// <summary>
        /// Allow correcting shoulder transforms in LateUpdate. This can produce more
        /// accurate shoulders, for instance.
        /// </summary>
        [SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(RetargetingLayerTooltips.ShoulderCorrectionWeightLateUpdate)]
        private float _shoulderCorrectionWeightLateUpdate = 1.0f;
        /// <inheritdoc cref="_shoulderCorrectionWeightLateUpdate"/>
        public float ShoulderCorrectionWeightLateUpdate
        {
            get => _shoulderCorrectionWeightLateUpdate;
            set => _shoulderCorrectionWeightLateUpdate = value;
        }

        /// <summary>
        /// Finger position correction weight. For some characters, we might want to correct
        /// all bones but reduce the positional accuracy of the fingers to maintain the
        /// character's original hand shape.
        /// </summary>
        [SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(RetargetingLayerTooltips.FingerPositionCorrectionWeight)]
        private float _fingerPositionCorrectionWeight = 0.0f;
        /// <inheritdoc cref="_fingerPositionCorrectionWeight"/>
        public float FingerPositionCorrectionWeight
        {
            get => _fingerPositionCorrectionWeight;
            set => _fingerPositionCorrectionWeight = value;
        }

        /// <summary>
        /// Regenerate job data.
        /// </summary>
        [SerializeField, InspectorButton("RegenJobDataFlag")]
        [Tooltip(RetargetingLayerTooltips.RegenJobData)]
        private bool _regenJobData;

        private NativeArray<Pose> _bodyPoses;
        private NativeArray<CorrectBonesMetadata> _correctBonesMetaData;
        private int[] _btBoneIndexToNativeIndex;

        private TransformAccessArray _bodyTrackingTransformsArray;
        private TransformAccessArray _animatorTransformsArray;

        private GetPosesJob _getBodyPosesJob;
        private CorrectBonesJob _correctBonesJob;

        private bool _allocatedDataFirstFrame = false;
        private Quaternion _hipsCorrectionQuatLastDataGen = Quaternion.identity;

        private class ShoulderInformation
        {
            /// <summary>
            /// Character shoulder transform to adjust.
            /// </summary>
            public Transform ShoulderTransform;
            /// <summary>
            /// Body tracking transform of shoulder.
            /// </summary>
            public Transform BodyTrackingTransform;
            /// <summary>
            /// Correction quaternion of shoulder.
            /// </summary>
            public Quaternion CorrectionQuaternion;
            /// <summary>
            /// Joint adjustment of shoulder, if any.
            /// </summary>
            public JointAdjustment Adjustment;
        }

        private ShoulderInformation _leftShoulderInfo;
        private ShoulderInformation _rightShoulderInfo;
        private bool[] _hasPositionConstraintOffset;

        /// <inheritdoc />
        public override void CleanUp()
        {
            if (_animatorTransformsArray.isCreated)
            {
                _animatorTransformsArray.Dispose();
            }
            if (_bodyTrackingTransformsArray.isCreated)
            {
                _bodyTrackingTransformsArray.Dispose();
            }
            if (_bodyPoses.IsCreated)
            {
                _bodyPoses.Dispose();
            }
            if (_correctBonesMetaData.IsCreated)
            {
                _correctBonesMetaData.Dispose();
            }
        }

        /// <inheritdoc />
        public override void RespondToCalibration(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            // Avoid regenerating data due to calibration event if bone counts have not changed.
            if (_bodyTrackingTransformsArray.isCreated &&
                GetNumValidBones(retargetingLayer, ovrBones) == _bodyTrackingTransformsArray.length)
            {
                return;
            }
            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            int numCurrentBones = ovrBones.Count;
            List<Transform> ovrBonesList = new List<Transform>();
            List<Transform> animatorBonesList = new List<Transform>();
            _btBoneIndexToNativeIndex = new int[numCurrentBones];
            int nativeIndex = 0;
            for (int i = 0; i < numCurrentBones; i++)
            {
                OVRBone bone = ovrBones[i];
                if (bone == null)
                {
                    continue;
                }
                HumanBodyBones humanBodyBone = HumanBodyBones.LastBone;
                Quaternion correctionQuaternion = Quaternion.identity;
                if (!GetHumanBoneAndCorrectionQuaternion(i, bone, retargetingLayer,
                    ref humanBodyBone, ref correctionQuaternion))
                {
                    _btBoneIndexToNativeIndex[i] = -1;
                    continue;
                }
                else
                {
                    _btBoneIndexToNativeIndex[i] = nativeIndex;
                }

                ovrBonesList.Add(bone.Transform);
                var animatorBone = retargetingLayer.GetOriginalJoint(humanBodyBone);
                if (animatorBone == null)
                {
                    Debug.LogError($"Animator does not have {humanBodyBone} that is mapped to {bone.Id}!");
                }
                animatorBonesList.Add(animatorBone);
                nativeIndex++;
            }

            if (_bodyTrackingTransformsArray.isCreated)
            {
                _bodyTrackingTransformsArray.Dispose();
            }
            if (_animatorTransformsArray.isCreated)
            {
                _animatorTransformsArray.Dispose();
            }
            _bodyTrackingTransformsArray = new TransformAccessArray(ovrBonesList.ToArray());
            _animatorTransformsArray = new TransformAccessArray(animatorBonesList.ToArray());

            if (_bodyPoses.IsCreated)
            {
                _bodyPoses.Dispose();
            }

            int numPoses = _bodyTrackingTransformsArray.length;
            _bodyPoses = new NativeArray<Pose>(numPoses, Allocator.Persistent);
            _getBodyPosesJob = new GetPosesJob()
            {
                Poses = _bodyPoses
            };

            if (_correctBonesMetaData.IsCreated)
            {
                _correctBonesMetaData.Dispose();
            }

            _correctBonesMetaData = new NativeArray<CorrectBonesMetadata>(numPoses, Allocator.Persistent);
            for (int i = 0; i < numPoses; i++)
            {
                _correctBonesMetaData[i] = new CorrectBonesMetadata();
            }
            _correctBonesJob = new CorrectBonesJob()
            {
                BodyPoses = _bodyPoses,
                Metadata = _correctBonesMetaData
            };
            _hasPositionConstraintOffset = new bool[_correctBonesMetaData.Length];
            RegenJobData(retargetingLayer, ovrBones);
        }

        int GetNumValidBones(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            int numCurrentBones = ovrBones.Count;
            int numValidBonesFound = 0;
            for (int i = 0; i < numCurrentBones; i++)
            {
                OVRBone bone = ovrBones[i];
                if (bone == null)
                {
                    continue;
                }
                HumanBodyBones humanBodyBone = HumanBodyBones.LastBone;
                Quaternion correctionQuaternion = Quaternion.identity;
                if (!GetHumanBoneAndCorrectionQuaternion(i, bone, retargetingLayer,
                    ref humanBodyBone, ref correctionQuaternion))
                {
                    continue;
                }
                numValidBonesFound++;
            }
            return numValidBonesFound;
        }

        private bool GetHumanBoneAndCorrectionQuaternion(int i, OVRBone ovrBone, RetargetingLayer retargetingLayer,
            ref HumanBodyBones humanBodyBone, ref Quaternion targetCorrectionQuaternion)
        {
            humanBodyBone = HumanBodyBones.LastBone;
            targetCorrectionQuaternion = Quaternion.identity;
            if (ovrBone == null)
            {
                return false;
            }

            // Check if there's a valid bone mapping to the HumanBodyBone.
            var nullableHumanBodyBone = retargetingLayer.GetCustomBoneIdToHumanBodyBone(ovrBone.Id);
            if (nullableHumanBodyBone == null)
            {
                return false;
            }
            humanBodyBone = (HumanBodyBones)nullableHumanBodyBone;

            // Check if there's a retargeting correction quaternion for the HumanBodyBone.
            var nullableTargetCorrectionQuaternion = retargetingLayer.GetCorrectionQuaternion(humanBodyBone);
            if (nullableTargetCorrectionQuaternion == null)
            {
                return false;
            }
            targetCorrectionQuaternion = (Quaternion)nullableTargetCorrectionQuaternion;

            return true;
        }

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            base.CopyData(source);
            var sourceCorrectBones = source as RetargetingProcessorCorrectBones;
            if (sourceCorrectBones == null)
            {
                Debug.LogError($"Failed to copy properties from {source.name} processor to {name} processor");
                return;
            }
            _correctPositionsLateUpdate = sourceCorrectBones.CorrectPositionsLateUpdate;
            _shoulderCorrectionWeightLateUpdate = sourceCorrectBones.ShoulderCorrectionWeightLateUpdate;
            _fingerPositionCorrectionWeight = sourceCorrectBones.FingerPositionCorrectionWeight;
        }

        /// <inheritdoc />
        public override void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (Weight <= 0.0f)
            {
                return;
            }

            if (ovrBones == null)
            {
                return;
            }

            if (_processorType == RetargetingProcessorType.Jobs)
            {
                return;
            }

            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var startingLeftHandPos = leftHand.localPosition;
            var startingRightHandPos = rightHand.localPosition;

            CorrectBones(retargetingLayer, ovrBones);

            // Revert to the original hand position, as this should be used to only correct the fingers.
            leftHand.localPosition = startingLeftHandPos;
            rightHand.localPosition = startingRightHandPos;
        }

        private void CorrectBones(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            for (var i = 0; i < ovrBones.Count; i++)
            {
                OVRBone btBone = ovrBones[i];
                HumanBodyBones humanBodyBone = HumanBodyBones.LastBone;
                Quaternion targetCorrectionQuaternion = Quaternion.identity;
                if (!GetHumanBoneAndCorrectionQuaternion(i, btBone, retargetingLayer,
                    ref humanBodyBone, ref targetCorrectionQuaternion))
                {
                    continue;
                }

                // Make sure body part passes mask, and bone's position should be updated.
                bool isInvalidBodyHips = false;
                Transform targetJoint = null;
                bool shouldRetarget =
                    ShouldRetargetHumanBodyBone(i, retargetingLayer, humanBodyBone, ref isInvalidBodyHips, ref targetJoint);
                if (isInvalidBodyHips)
                {
                    // invalid hips? bail.
                    return;
                }
                if (!shouldRetarget)
                {
                    continue;
                }
                var rtWeight = Weight * retargetingLayer.RetargetingConstraint.weight;
                bool isHandJoint = false, isShoulderJoint = false, isAFingerJoint = false;
                OVRHumanBodyBonesMappings.BodySection bodySectionOfJoint = OVRHumanBodyBonesMappings.BodySection.Back;
                GetJointInformation(retargetingLayer, humanBodyBone, ref isHandJoint, ref isShoulderJoint, ref bodySectionOfJoint,
                    ref isAFingerJoint);

                Vector3 constraintsPositionOffset = Vector3.zero;
                JointAdjustment adjustment = null;
                GetOffsetAndAdjustment(retargetingLayer, humanBodyBone, isHandJoint,
                    bodySectionOfJoint,
                    ref constraintsPositionOffset, ref adjustment);

                // Remove muscle space restrictions for the shoulders.
                if (isShoulderJoint)
                {
                    CorrectShoulders(targetJoint,
                        btBone.Transform.rotation,
                        targetCorrectionQuaternion,
                        adjustment);
                }

                if (adjustment == null)
                {
                    if (isHandJoint)
                    {
                        targetJoint.rotation = Quaternion.Slerp(targetJoint.rotation,
                            btBone.Transform.rotation * targetCorrectionQuaternion, rtWeight);
                    }

                    if (CorrectPositionsLateUpdate)
                    {
                        targetJoint.position =
                            Vector3.Lerp(targetJoint.position,
                                btBone.Transform.position + constraintsPositionOffset,
                                isAFingerJoint ? rtWeight * _fingerPositionCorrectionWeight : rtWeight);
                    }
                }
                else
                {
                    if (isHandJoint)
                    {
                        if (!adjustment.DisableRotationTransform)
                        {
                            var finalRotation = btBone.Transform.rotation * targetCorrectionQuaternion;
                            finalRotation *= adjustment.RotationChange * adjustment.PrecomputedRotationTweaks;

                            targetJoint.rotation = Quaternion.Slerp(targetJoint.rotation, finalRotation, rtWeight);
                        }
                    }

                    if (CorrectPositionsLateUpdate && !adjustment.DisablePositionTransform)
                    {
                        targetJoint.position =
                            Vector3.Lerp(targetJoint.position,
                                btBone.Transform.position + constraintsPositionOffset + adjustment.PositionChange,
                                isAFingerJoint ? rtWeight * _fingerPositionCorrectionWeight : rtWeight);
                    }
                }
            }
        }

        private void GetOffsetAndAdjustment(RetargetingLayer retargetingLayer, HumanBodyBones humanBodyBone, bool isHandJoint,
            OVRHumanBodyBonesMappings.BodySection bodySectionOfJoint,
            ref Vector3 constraintsPositionOffset, ref JointAdjustment jointAdjustment)
        {
            constraintsPositionOffset = retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions ?
                retargetingLayer.JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset() : Vector3.zero;

            // Move the hand and fingers to the tracked positions by setting the position offset to zero.
            if (isHandJoint)
            {
                constraintsPositionOffset = Vector3.zero;
            }

            jointAdjustment = retargetingLayer.GetFindAdjustment(humanBodyBone);
        }

        private void GetJointInformation(RetargetingLayer retargetingLayer, HumanBodyBones humanBodyBone,
            ref bool isHandJoint, ref bool isShoulderJoint, ref OVRHumanBodyBonesMappings.BodySection bodySectionOfJoint,
            ref bool isAFingerJoint)
        {
            bodySectionOfJoint = retargetingLayer.GetHumanBodyBoneToBodySection(humanBodyBone);
            bool isLeftHandFingersOrWrist =
                bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.LeftHand ||
                humanBodyBone == HumanBodyBones.LeftHand;
            bool isRightHandFingersOrWrist =
                bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.RightHand ||
                humanBodyBone == HumanBodyBones.RightHand;
            isHandJoint = isLeftHandFingersOrWrist || isRightHandFingersOrWrist;
            isShoulderJoint = humanBodyBone == HumanBodyBones.LeftShoulder ||
                humanBodyBone == HumanBodyBones.RightShoulder;
            // Used to dial back finger position correction, if required.
            isAFingerJoint =
                bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.LeftHand ||
                bodySectionOfJoint == OVRHumanBodyBonesMappings.BodySection.RightHand;
        }

        private bool ShouldRetargetHumanBodyBone(int boneIndex, RetargetingLayer retargetingLayer, HumanBodyBones humanBodyBone,
            ref bool isInvalidBodyHips, ref Transform targetJoint)
        {
            var bodyPart = BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)humanBodyBone];
            targetJoint = retargetingLayer.GetOriginalJoint(humanBodyBone);
            if (retargetingLayer.CustomPositionsToCorrectLateUpdateMask != null &&
                !retargetingLayer.CustomPositionsToCorrectLateUpdateMask.GetHumanoidBodyPartActive(bodyPart))
            {
                return false;
            }
            if (!retargetingLayer.GetShouldUpdatePositionOfBone(humanBodyBone))
            {
                return false;
            }

            // If this is the first target position, check its validity.
            // If it's valid, assume that the remaining positions are valid as well.
            if (boneIndex == 0 && !RiggingUtilities.IsFiniteVector3(targetJoint.position))
            {
                isInvalidBodyHips = false;
                return false;
            }
            isInvalidBodyHips = false;
            return true;
        }

        public void RegenJobData(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            // Synchronize any user settings with the metadata that the job uses to retarget.
            bool isInvalidBodyHips = false;
            for (var i = 0; i < ovrBones.Count; i++)
            {
                OVRBone btBone = ovrBones[i];
                var nativeIndex = _btBoneIndexToNativeIndex[i];
                if (nativeIndex == -1)
                {
                    continue;
                }
                _hasPositionConstraintOffset[nativeIndex] = false;

                // if hips are found to be invalid, mark as every other bone as "do not update"
                if (isInvalidBodyHips)
                {
                    _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                    {
                        ShouldCorrectBone = false,
                        TargetBodyBone = HumanBodyBones.LastBone
                    };
                    continue;
                }

                HumanBodyBones humanBodyBone = HumanBodyBones.LastBone;
                Quaternion targetCorrectionQuaternion = Quaternion.identity;
                if (!GetHumanBoneAndCorrectionQuaternion(i, btBone, retargetingLayer,
                    ref humanBodyBone, ref targetCorrectionQuaternion))
                {
                    _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                    {
                        ShouldCorrectBone = false,
                        TargetBodyBone = humanBodyBone,
                    };
                    continue;
                }
                if (humanBodyBone == HumanBodyBones.Hips)
                {
                    _hipsCorrectionQuatLastDataGen = targetCorrectionQuaternion;
                }

                // Make sure body part passes mask, and bone's position should be updated.
                Transform targetJoint = null;
                bool shouldRetarget =
                    ShouldRetargetHumanBodyBone(i, retargetingLayer, humanBodyBone,
                        ref isInvalidBodyHips, ref targetJoint);
                if (isInvalidBodyHips)
                {
                    // invalid hips? bail.
                    _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                    {
                        ShouldCorrectBone = false,
                        TargetBodyBone = humanBodyBone,
                    };
                    continue;
                }
                if (!shouldRetarget)
                {
                    _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                    {
                        ShouldCorrectBone = false,
                        TargetBodyBone = humanBodyBone,
                    };
                    continue;
                }

                bool isHandJoint = false, isShoulderJoint = false, isAFingerJoint = false;
                OVRHumanBodyBonesMappings.BodySection bodySectionOfJoint = OVRHumanBodyBonesMappings.BodySection.Back;
                GetJointInformation(retargetingLayer, humanBodyBone, ref isHandJoint,
                    ref isShoulderJoint, ref bodySectionOfJoint, ref isAFingerJoint);

                Vector3 constraintsPositionOffset = Vector3.zero;
                JointAdjustment adjustment = null;
                GetOffsetAndAdjustment(retargetingLayer, humanBodyBone, isHandJoint,
                    bodySectionOfJoint,
                    ref constraintsPositionOffset, ref adjustment);

                // NOTE: serial correction done serially after job is done.
                if (isShoulderJoint)
                {
                    if (humanBodyBone == HumanBodyBones.LeftShoulder)
                    {
                        _leftShoulderInfo = new ShoulderInformation()
                        {
                            ShoulderTransform = targetJoint,
                            BodyTrackingTransform = btBone.Transform,
                            CorrectionQuaternion = targetCorrectionQuaternion,
                            Adjustment = adjustment
                        };
                    }
                    else
                    {
                        _rightShoulderInfo = new ShoulderInformation()
                        {
                            ShoulderTransform = targetJoint,
                            BodyTrackingTransform = btBone.Transform,
                            CorrectionQuaternion = targetCorrectionQuaternion,
                            Adjustment = adjustment
                        };
                    }
                }

                var posWeight = Weight * retargetingLayer.RetargetingConstraint.weight;
                var rotWeight = posWeight;
                Quaternion finalCorrectionQuaternion = Quaternion.identity;
                Quaternion rotationTweak = Quaternion.identity;
                Vector3 positionOffset = Vector3.zero;
                Vector3 constraintOffset = Vector3.zero;
                bool applyRotation = false, applyPosition = false;
                if (adjustment == null)
                {
                    if (isHandJoint)
                    {
                        finalCorrectionQuaternion = targetCorrectionQuaternion;
                        applyRotation = true;
                    }

                    if (CorrectPositionsLateUpdate)
                    {
                        constraintOffset = constraintsPositionOffset;
                        posWeight = isAFingerJoint ? posWeight * _fingerPositionCorrectionWeight : posWeight;
                        applyPosition = true;
                        _hasPositionConstraintOffset[nativeIndex] = true;
                    }
                }
                else
                {
                    if (isHandJoint && !adjustment.DisableRotationTransform)
                    {
                        finalCorrectionQuaternion = targetCorrectionQuaternion;
                        rotationTweak = adjustment.RotationChange * adjustment.PrecomputedRotationTweaks;
                        applyRotation = true;
                    }

                    if (CorrectPositionsLateUpdate && !adjustment.DisablePositionTransform)
                    {
                        _hasPositionConstraintOffset[nativeIndex] = true;
                        constraintOffset = constraintsPositionOffset;
                        positionOffset = adjustment.PositionChange;
                        applyPosition = true;
                        posWeight = isAFingerJoint ? posWeight * _fingerPositionCorrectionWeight : posWeight;
                    }
                }
                _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                {
                    ShouldCorrectBone = true,
                    TargetBodyBone = humanBodyBone,
                    PosWeight = posWeight,
                    RotWeight = rotWeight,
                    AdjustPosition = applyPosition,
                    AdjustRotation = applyRotation,
                    CorrectionQuaternion = finalCorrectionQuaternion,
                    PositionAdjustment = positionOffset,
                    ConstraintOffset = constraintOffset,
                    RotationAdjustment = rotationTweak,
                    IsHandJoint = isHandJoint,
                };
            }
        }

        private void UpdatePerFrameCorrectBonesMetadata(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            // Only updates constraints. Bail if not necessary.
            if (!retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions)
            {
                return;
            }
            for (var i = 0; i < ovrBones.Count; i++)
            {
                var nativeIndex = _btBoneIndexToNativeIndex[i];
                if (nativeIndex == -1)
                {
                    continue;
                }
                if (!_hasPositionConstraintOffset[nativeIndex])
                {
                    continue;
                }

                var oldMetaData = _correctBonesMetaData[nativeIndex];

                var humanBodyBone = oldMetaData.TargetBodyBone;
                Vector3 constraintsPositionOffset = Vector3.zero;
                constraintsPositionOffset =
                    retargetingLayer.JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset();
                // Move the hand and fingers to the tracked positions by setting the position offset to zero.
                if (oldMetaData.IsHandJoint)
                {
                    constraintsPositionOffset = Vector3.zero;
                }

                _correctBonesMetaData[nativeIndex] = new CorrectBonesMetadata()
                {
                    ShouldCorrectBone = oldMetaData.ShouldCorrectBone,
                    TargetBodyBone = oldMetaData.TargetBodyBone,
                    PosWeight = oldMetaData.PosWeight,
                    RotWeight = oldMetaData.RotWeight,
                    AdjustPosition = oldMetaData.AdjustPosition,
                    AdjustRotation = oldMetaData.AdjustRotation,
                    CorrectionQuaternion = oldMetaData.CorrectionQuaternion,
                    PositionAdjustment = oldMetaData.PositionAdjustment,
                    ConstraintOffset = constraintsPositionOffset,
                    RotationAdjustment = oldMetaData.RotationAdjustment,
                    IsHandJoint = oldMetaData.IsHandJoint
                };
            }
        }

        public void RegenJobDataFlag()
        {
            _regenJobData = true;
        }

        public override JobHandle ProcessRetargetingLayerJob(JobHandle? previousJob, RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (_processorType == RetargetingProcessorType.Normal)
            {
                return new JobHandle();
            }

            if (!_correctBonesMetaData.IsCreated || _correctBonesMetaData.Length == 0)
            {
                return new JobHandle();
            }

            if (Weight <= 0.0f)
            {
                return new JobHandle();
            }

            var hipsCorrection = retargetingLayer.GetCorrectionQuaternion(HumanBodyBones.Hips);
            var referenceCorrection = _hipsCorrectionQuatLastDataGen;
            bool jobHipDataOffsetDiffers = hipsCorrection != referenceCorrection;

            if (_regenJobData || !_allocatedDataFirstFrame ||
                jobHipDataOffsetDiffers)
            {
                RegenJobData(retargetingLayer, ovrBones);
                _regenJobData = false;
                _allocatedDataFirstFrame = true;
            }
            else
            {
                UpdatePerFrameCorrectBonesMetadata(retargetingLayer, ovrBones);
            }

            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var startingLeftHandPos = leftHand.localPosition;
            var startingRightHandPos = rightHand.localPosition;

            JobHandle bodyPosesHandle =
                previousJob.HasValue ?
                    _getBodyPosesJob.ScheduleReadOnly(_bodyTrackingTransformsArray, 32, previousJob.Value) :
                    _getBodyPosesJob.ScheduleReadOnly(_bodyTrackingTransformsArray, 32);
            JobHandle correctBonesJob = _correctBonesJob.Schedule(_animatorTransformsArray, bodyPosesHandle);
            correctBonesJob.Complete();

            CorrectShoulders(_leftShoulderInfo.ShoulderTransform,
                _leftShoulderInfo.BodyTrackingTransform.rotation,
                _leftShoulderInfo.CorrectionQuaternion,
                _leftShoulderInfo.Adjustment);
            CorrectShoulders(_rightShoulderInfo.ShoulderTransform,
                _rightShoulderInfo.BodyTrackingTransform.rotation,
                _rightShoulderInfo.CorrectionQuaternion,
                _rightShoulderInfo.Adjustment);

            // Revert to the original hand position, as this should be used to only correct the fingers.
            leftHand.localPosition = startingLeftHandPos;
            rightHand.localPosition = startingRightHandPos;

            return correctBonesJob;
        }

        /// <summary>
        /// Correct the shoulders rotation to not be restricted by muscle space.
        /// </summary>
        private void CorrectShoulders(Transform targetJoint,
            Quaternion boneRotation,
            Quaternion correctionQuaternion,
            JointAdjustment adjustment)
        {
            // Restore the child transform when correcting shoulders.
            var childJoint = targetJoint.GetChild(0);
            var childAffineTransform = new AffineTransform
            {
                rotation = childJoint != null ? childJoint.rotation : Quaternion.identity,
                translation = childJoint != null ? childJoint.position : Vector3.zero
            };
            var targetWeight = adjustment != null && adjustment.DisableRotationTransform ?
                0.0f : ShoulderCorrectionWeightLateUpdate * Weight;
            var rotationChange = adjustment?.PrecomputedRotationTweaks ?? Quaternion.identity;
            targetJoint.rotation =
                Quaternion.Slerp(targetJoint.rotation,
                    boneRotation * correctionQuaternion * rotationChange,
                    targetWeight);

            if (childJoint != null)
            {
                childJoint.SetPositionAndRotation(childAffineTransform.translation,
                    childAffineTransform.rotation);
            }
        }
    }
}
