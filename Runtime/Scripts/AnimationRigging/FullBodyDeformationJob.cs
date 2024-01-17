// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The FullBodyDeformation job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct FullBodyDeformationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// Bone animation data for the FullBodyDeformation job.
        /// </summary>
        public struct BoneAnimationData
        {
            /// <summary>
            /// The distance between the start and end bone transforms.
            /// </summary>
            public float Distance;

            /// <summary>
            /// The proportion of this bone relative to the height.
            /// </summary>
            public float HeightProportion;

            /// <summary>
            /// The proportion of this bone relative to its limb.
            /// </summary>
            public float LimbProportion;
        }

        /// <summary>
        /// The ReadWrite transform handle for the left shoulder bone.
        /// </summary>
        public ReadWriteTransformHandle LeftShoulderBone;

        /// <summary>
        /// The ReadWrite transform handle for the right shoulder bone.
        /// </summary>
        public ReadWriteTransformHandle RightShoulderBone;

        /// <summary>
        /// The ReadWrite transform handle for the left upper arm bone.
        /// </summary>
        public ReadWriteTransformHandle LeftUpperArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the right upper arm bone.
        /// </summary>
        public ReadWriteTransformHandle RightUpperArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the left lower arm bone.
        /// </summary>
        public ReadWriteTransformHandle LeftLowerArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the right lower arm bone.
        /// </summary>
        public ReadWriteTransformHandle RightLowerArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the left hand bone.
        /// </summary>
        public ReadWriteTransformHandle LeftHandBone;

        /// <summary>
        /// The ReadWrite transform handle for the right hand bone.
        /// </summary>
        public ReadWriteTransformHandle RightHandBone;

        /// <summary>
        /// The ReadWrite transform handle for the left upper leg bone.
        /// </summary>
        public ReadWriteTransformHandle LeftUpperLegBone;

        /// <summary>
        /// The ReadWrite transform handle for the right upper leg bone.
        /// </summary>
        public ReadWriteTransformHandle RightUpperLegBone;

        /// <summary>
        /// The ReadWrite transform handle for the left lower leg bone.
        /// </summary>
        public ReadWriteTransformHandle LeftLowerLegBone;

        /// <summary>
        /// The ReadWrite transform handle for the right lower leg bone.
        /// </summary>
        public ReadWriteTransformHandle RightLowerLegBone;

        /// <summary>
        /// The ReadWrite transform handle for the left foot bone.
        /// </summary>
        public ReadWriteTransformHandle LeftFootBone;

        /// <summary>
        /// The ReadWrite transform handle for the right foot bone.
        /// </summary>
        public ReadWriteTransformHandle RightFootBone;

        /// <summary>
        /// The ReadWrite transform handle for the left toes bone.
        /// </summary>
        public ReadWriteTransformHandle LeftToesBone;

        /// <summary>
        /// The ReadWrite transform handle for the right toes bone.
        /// </summary>
        public ReadWriteTransformHandle RightToesBone;

        /// <summary>
        /// The ReadWrite transform handle for the hips bone.
        /// </summary>
        public ReadWriteTransformHandle HipsBone;

        /// <summary>
        /// The ReadWrite transform handle for the head bone.
        /// </summary>
        public ReadWriteTransformHandle HeadBone;

        /// <summary>
        /// The inclusive array of bones from the hips to the head.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> HipsToHeadBones;

        /// <summary>
        /// The inclusive array of bone targets from the hips to the head.
        /// </summary>
        public NativeArray<ReadOnlyTransformHandle> HipsToHeadBoneTargets;

        /// <summary>
        /// The array of upper body offsets.
        /// </summary>
        public NativeArray<Vector3> UpperBodyOffsets;

        /// <summary>
        /// The array of upper body target positions.
        /// </summary>
        public NativeArray<Vector3> UpperBodyTargetPositions;

        /// <summary>
        /// The array of start bones for FullBodyDeformation.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> StartBones;

        /// <summary>
        /// The array of end bones for FullBodyDeformation.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> EndBones;

        /// <summary>
        /// The array of bone animation data for the start and end bone pairs.
        /// </summary>
        public NativeArray<BoneAnimationData> BoneAnimData;

        /// <summary>
        /// The array of directions between the start and end bones.
        /// </summary>
        public NativeArray<Vector3> BoneDirections;

        /// <summary>
        /// The array containing 1 element for the current scale ratio.
        /// </summary>
        public NativeArray<Vector3> ScaleFactor;

        /// <summary>
        /// The spine correction type.
        /// </summary>
        public IntProperty SpineCorrectionType;

        /// <summary>
        /// The hips index in the bone pair data.
        /// </summary>
        public int HipsIndex;

        /// <summary>
        /// The spine index in the bone pair data.
        /// </summary>
        public int SpineLowerIndex;

        /// <summary>
        /// The spine upper index in the bone pair data.
        /// </summary>
        public int SpineUpperIndex;

        /// <summary>
        /// The chest index in the bone pair data.
        /// </summary>
        public int ChestIndex;

        /// <summary>
        /// The index of the spine bone that is the parent of the shoulders.
        /// </summary>
        public int ShouldersParentIndex;

        /// <summary>
        /// The head index in the bone pair data.
        /// </summary>
        public int HeadIndex;

        /// <summary>
        /// The weight for the spine lower fixup.
        /// </summary>
        public FloatProperty SpineLowerAlignmentWeight;

        /// <summary>
        /// The weight for the spine upper fixup.
        /// </summary>
        public FloatProperty SpineUpperAlignmentWeight;

        /// <summary>
        /// The weight for the chest fixup.
        /// </summary>
        public FloatProperty ChestAlignmentWeight;

        /// <summary>
        /// The weight to adjust the height of the shoulders.
        /// </summary>
        public FloatProperty ShouldersHeightAdjustmentWeight;

        /// <summary>
        /// The weight to adjust the width of the shoulders.
        /// </summary>
        public FloatProperty ShouldersWidthAdjustmentWeight;

        /// <summary>
        /// The weight of the left shoulder offset.
        /// </summary>
        public FloatProperty LeftShoulderOffsetWeight;

        /// <summary>
        /// The weight of the right shoulder offset.
        /// </summary>
        public FloatProperty RightShoulderOffsetWeight;

        /// <summary>
        /// The weight to adjust the height of the arms.
        /// </summary>
        public FloatProperty ArmsHeightAdjustmentWeight;

        /// <summary>
        /// The weight for the left arm offset.
        /// </summary>
        public FloatProperty LeftArmOffsetWeight;

        /// <summary>
        /// The weight for the right arm offset.
        /// </summary>
        public FloatProperty RightArmOffsetWeight;

        /// <summary>
        /// The weight for the left hand offset.
        /// </summary>
        public FloatProperty LeftHandOffsetWeight;

        /// <summary>
        /// The weight for the right hand offset.
        /// </summary>
        public FloatProperty RightHandOffsetWeight;

        /// <summary>
        /// The weight for the left leg offset.
        /// </summary>
        public FloatProperty LeftLegOffsetWeight;

        /// <summary>
        /// The weight for the right leg offset.
        /// </summary>
        public FloatProperty RightLegOffsetWeight;

        /// <summary>
        /// The weight for the left toe.
        /// </summary>
        public FloatProperty LeftToesOffsetWeight;

        /// <summary>
        /// The weight for the right toe.
        /// </summary>
        public FloatProperty RightToesOffsetWeight;

        /// <summary>
        /// The weight for aligning the feet.
        /// </summary>
        public FloatProperty AlignFeetWeight;

        /// <summary>
        /// The local position of the left shoulder.
        /// </summary>
        public Vector3 LeftShoulderOriginalLocalPos;

        /// <summary>
        /// The local position of the right shoulder.
        /// </summary>
        public Vector3 RightShoulderOriginalLocalPos;

        /// <summary>
        /// The local position of the left toes.
        /// </summary>
        public Vector3 LeftToesOriginalLocalPos;

        /// <summary>
        /// The local position of the right toes.
        /// </summary>
        public Vector3 RightToesOriginalLocalPos;

        /// <summary>
        /// The left foot local rotation.
        /// </summary>
        public Quaternion LeftFootLocalRot;

        /// <summary>
        /// The right foot local rotation.
        /// </summary>
        public Quaternion RightFootLocalRot;

        private Vector3 _targetHipsPos;
        private Vector3 _targetHeadPos;
        private Vector3 _preDeformationLeftUpperArmPos;
        private Vector3 _preDeformationRightUpperArmPos;
        private Vector3 _preDeformationLeftLowerArmPos;
        private Vector3 _preDeformationRightLowerArmPos;
        private Vector3 _preDeformationLeftHandPos;
        private Vector3 _preDeformationRightHandPos;
        private Vector3 _preDeformationLeftFootPos;
        private Vector3 _preDeformationRightFootPos;
        private Vector3 _preDeformationLeftToesPos;
        private Vector3 _preDeformationRightToesPos;

        private Vector3 _leftFootOffset;
        private Vector3 _rightFootOffset;
        private Vector3 _hipsGroundingOffset;
        private Vector3 _requiredSpineOffset;

        private int _leftShoulderIndex => HipsToHeadBones.Length - 1;
        private int _rightShoulderIndex => _leftShoulderIndex + 1;
        private int _leftUpperLegIndex => BoneAnimData.Length - 6;
        private int _rightUpperLegIndex => _leftUpperLegIndex + 1;
        private int _leftLowerLegIndex => _leftUpperLegIndex + 2;
        private int _rightLowerLegIndex => _leftLowerLegIndex + 1;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        /// <inheritdoc />
        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        /// <inheritdoc />
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            if (weight > 0f)
            {
                if (StartBones.Length == 0 || EndBones.Length == 0)
                {
                    return;
                }

                if (LeftToesBone.IsValid(stream))
                {
                    _preDeformationLeftToesPos = LeftToesBone.GetPosition(stream);
                }

                if (RightToesBone.IsValid(stream))
                {
                    _preDeformationRightToesPos = RightToesBone.GetPosition(stream);
                }

                _preDeformationLeftUpperArmPos = LeftUpperArmBone.GetPosition(stream);
                _preDeformationRightUpperArmPos = RightUpperArmBone.GetPosition(stream);
                _preDeformationLeftLowerArmPos = LeftLowerArmBone.GetPosition(stream);
                _preDeformationRightLowerArmPos = RightLowerArmBone.GetPosition(stream);
                _preDeformationLeftHandPos = LeftHandBone.GetPosition(stream);
                _preDeformationRightHandPos = RightHandBone.GetPosition(stream);
                _preDeformationLeftFootPos = LeftFootBone.GetPosition(stream);
                _preDeformationRightFootPos = RightFootBone.GetPosition(stream);

                _targetHipsPos = HipsToHeadBoneTargets[HipsIndex].GetPosition(stream);
                _targetHeadPos = HipsToHeadBoneTargets[^1].GetPosition(stream);
                if (SpineLowerAlignmentWeight.Get(stream) != 0.0f ||
                    SpineUpperAlignmentWeight.Get(stream) != 0.0f ||
                    ChestAlignmentWeight.Get(stream) != 0.0f)
                {
                    AlignSpine(stream, weight);
                }
                InterpolateShoulders(stream, weight);
                UpdateBoneDirections(stream);
                EnforceOriginalSkeletalProportions(stream, weight);
                InterpolateLegs(stream, weight);
                ApplySpineCorrection(stream, weight);
                ApplyShouldersCorrection(stream, weight);
                ApplyAccurateFeet(stream, weight);
                AlignFeet(stream, weight);
                InterpolateToesY(stream, weight);
                InterpolateArms(stream, weight);
                InterpolateHands(stream, weight);
            }
            else
            {
                for (int i = 0; i < HipsToHeadBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, HipsToHeadBones[i]);
                }

                for (int i = 0; i < StartBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, StartBones[i]);
                }

                for (int i = 0; i < EndBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, EndBones[i]);
                }
            }
        }

        /// <summary>
        /// Align the spine positions with the tracked spine positions,
        /// adding an offset on spine bones to align with the hips for a straight spine.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void AlignSpine(AnimationStream stream, float weight)
        {
            var spineLowerOffset = Vector3.zero;
            var spineUpperOffset = Vector3.zero;
            var chestOffset = Vector3.zero;

            var hipsPosition = HipsToHeadBoneTargets[HipsIndex].GetPosition(stream);
            var neckPosition = HipsToHeadBoneTargets[HeadIndex - 1].GetPosition(stream);
            var accumulatedProportion = 0.0f;

            if (HipsToHeadBoneTargets[SpineLowerIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[SpineLowerIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[SpineLowerIndex - 1].LimbProportion;
                // Ensure that the spine lower bone is straight up from the hips bone.
                spineLowerOffset = hipsPosition - spineTargetPosition;
                spineLowerOffset.y = 0.0f;
                spineLowerOffset = Vector3.Lerp(Vector3.zero, spineLowerOffset,
                    SpineLowerAlignmentWeight.Get(stream) * weight);
            }

            if (SpineUpperIndex > 0 && HipsToHeadBoneTargets[SpineUpperIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[SpineUpperIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[SpineUpperIndex - 1].LimbProportion;
                spineUpperOffset = (hipsPosition - spineTargetPosition) * (1 - accumulatedProportion) +
                                   (neckPosition - spineTargetPosition) * accumulatedProportion;
                spineUpperOffset.y = 0.0f;
                spineUpperOffset = Vector3.Lerp(Vector3.zero, spineUpperOffset,
                    SpineUpperAlignmentWeight.Get(stream) * weight);
            }

            if (ChestIndex > 0 && HipsToHeadBoneTargets[ChestIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[ChestIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[ChestIndex - 1].LimbProportion;
                chestOffset = (hipsPosition - spineTargetPosition) * (1 - accumulatedProportion) +
                              (neckPosition - spineTargetPosition) * accumulatedProportion;
                chestOffset.y = 0.0f;
                chestOffset = Vector3.Lerp(Vector3.zero, chestOffset,
                    ChestAlignmentWeight.Get(stream) * weight);
            }

            for (int i = SpineLowerIndex; i < HeadIndex; i++)
            {
                var targetBone = HipsToHeadBoneTargets[i];
                var originalBone = HipsToHeadBones[i];
                if (targetBone.IsValid(stream))
                {
                    var spineCorrectionWeight = weight;
                    var spineOffset = Vector3.zero;

                    if (i == SpineLowerIndex)
                    {
                        spineOffset = spineLowerOffset;
                    }
                    else if (i == SpineUpperIndex)
                    {
                        spineOffset = spineUpperOffset;
                    }
                    else if (i == ChestIndex)
                    {
                        spineOffset = chestOffset;
                    }

                    var targetPos = targetBone.GetPosition(stream) + spineOffset;
                    var originalPos = originalBone.GetPosition(stream);
                    targetPos.y = originalPos.y;
                    targetPos = Vector3.Lerp(originalPos, targetPos, spineCorrectionWeight);
                    originalBone.SetPosition(stream, targetPos);
                }
                HipsToHeadBoneTargets[i] = targetBone;
                HipsToHeadBones[i] = originalBone;
            }
        }

        /// <summary>
        /// Optionally interpolate from the current position to the original local position of the shoulders,
        /// as the tracked positions may be mismatched.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void InterpolateShoulders(AnimationStream stream, float weight)
        {
            if (LeftShoulderBone.IsValid(stream))
            {
                var leftShoulderLocalPos = LeftShoulderBone.GetLocalPosition(stream);
                var leftShoulderOffset = Vector3.Lerp(Vector3.zero,
                    LeftShoulderOriginalLocalPos - leftShoulderLocalPos, weight * LeftShoulderOffsetWeight.Get(stream));
                LeftShoulderBone.SetLocalPosition(stream, leftShoulderLocalPos + leftShoulderOffset);
            }

            if (RightShoulderBone.IsValid(stream))
            {
                var rightShoulderLocalPos = RightShoulderBone.GetLocalPosition(stream);
                var rightShoulderOffset = Vector3.Lerp(Vector3.zero,
                    RightShoulderOriginalLocalPos - rightShoulderLocalPos, weight * RightShoulderOffsetWeight.Get(stream));
                RightShoulderBone.SetLocalPosition(stream, rightShoulderLocalPos + rightShoulderOffset);
            }
        }

        /// <summary>
        /// Update the bone directions between each bone pair.
        /// </summary>
        /// <param name="stream"></param>
        private void UpdateBoneDirections(AnimationStream stream)
        {
            for (int i = 0; i < BoneDirections.Length; i++)
            {
                var startBone = StartBones[i];
                var endBone = EndBones[i];
                BoneDirections[i] = (endBone.GetPosition(stream) - startBone.GetPosition(stream)).normalized;
                StartBones[i] = startBone;
                EndBones[i] = endBone;
            }
        }

        /// <summary>
        /// Applies spine correction, depending on the type picked.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplySpineCorrection(AnimationStream stream, float weight)
        {
            // This hips offset is the offset required to preserve leg lengths when feet are planted.
            _hipsGroundingOffset = (_leftFootOffset + _rightFootOffset) / 2f;

            // First, adjust the positions of the body based on the correction type.
            var spineCorrectionType = (FullBodyDeformationData.SpineTranslationCorrectionType)
                SpineCorrectionType.Get(stream);

            if (spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.None)
            {
                ApplyNoSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHead)
            {
                ApplyAccurateHeadSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHips ||
                spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                ApplyAccurateHipsSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                ApplyAccurateHipsAndHeadSpineCorrection(stream, weight);
            }

            // Update upper arm bones based on the corrections applied to the spine from the shoulders.
            if (spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHead ||
                spineCorrectionType == FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                var shoulderHeightAdjustment = _requiredSpineOffset.magnitude;
                if (shoulderHeightAdjustment <= float.Epsilon)
                {
                    return;
                }

                weight *= ArmsHeightAdjustmentWeight.Get(stream);
                var leftShoulderPos = LeftShoulderBone.GetPosition(stream);
                var rightShoulderPos = RightShoulderBone.GetPosition(stream);
                var shouldersParentPos = HipsToHeadBones[ShouldersParentIndex].GetPosition(stream);
                var leftShoulderOffset = (shouldersParentPos - leftShoulderPos).normalized *
                                         shoulderHeightAdjustment * BoneAnimData[_leftShoulderIndex].LimbProportion;
                var rightShoulderOffset = (shouldersParentPos - rightShoulderPos).normalized *
                                          shoulderHeightAdjustment * BoneAnimData[_rightShoulderIndex].LimbProportion;

                LeftUpperArmBone.SetPosition(stream, LeftUpperArmBone.GetPosition(stream) +
                                                     Vector3.Lerp(Vector3.zero, leftShoulderOffset, weight));
                RightUpperArmBone.SetPosition(stream, RightUpperArmBone.GetPosition(stream) +
                                                     Vector3.Lerp(Vector3.zero, rightShoulderOffset, weight));
            }
        }

        /// <summary>
        /// Adjust the hips by the foot offset, then adjust the legs afterwards.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyNoSpineCorrection(AnimationStream stream, float weight)
        {
            _requiredSpineOffset = Vector3.zero;

            var targetHipsPos = HipsBone.GetPosition(stream) +
                                Vector3.Lerp(Vector3.zero, _hipsGroundingOffset, weight);
            var targetLeftUpperLegPos = LeftUpperLegBone.GetPosition(stream);
            var targetRightUpperLegPos = RightUpperLegBone.GetPosition(stream);

            HipsBone.SetPosition(stream, targetHipsPos);
            LeftUpperLegBone.SetPosition(stream, targetLeftUpperLegPos);
            RightUpperLegBone.SetPosition(stream, targetRightUpperLegPos);
        }

        /// <summary>
        /// Keep the head accurate, adjusting the proportions of the full body.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHeadSpineCorrection(AnimationStream stream, float weight)
        {
            var headOffset = _targetHeadPos - HeadBone.GetPosition(stream);
            _requiredSpineOffset = headOffset;

            // Separate head offset application to upper and lower body.
            var upperBodyProportion = 0.0f;
            for (int i = HipsIndex; i < HeadIndex; i++)
            {
                upperBodyProportion += BoneAnimData[i].HeightProportion;
            }
            var lowerBodyProportion = 1 - upperBodyProportion;

            // Upper body.
            UpperBodyOffsets[HipsIndex] = headOffset * (lowerBodyProportion + BoneAnimData[HipsIndex].HeightProportion);
            UpperBodyTargetPositions[HipsIndex] = HipsBone.GetPosition(stream) +
                                                  Vector3.Lerp(Vector3.zero, UpperBodyOffsets[HipsIndex], weight);
            for (int i = SpineLowerIndex; i < HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                UpperBodyOffsets[i] = UpperBodyOffsets[i - 1] + headOffset * BoneAnimData[i].LimbProportion;
                UpperBodyTargetPositions[i] = bone.GetPosition(stream) +
                                              Vector3.Lerp(Vector3.zero, UpperBodyOffsets[i], weight);
            }

            // Lower body.
            var leftUpperLegOffset = -_hipsGroundingOffset;
            var rightUpperLegOffset = -_hipsGroundingOffset;
            var leftLowerLegOffset = headOffset *
                                     (BoneAnimData[_leftLowerLegIndex].HeightProportion +
                                      BoneAnimData[_leftUpperLegIndex].HeightProportion);
            var rightLowerLegOffset = headOffset *
                                      (BoneAnimData[_rightLowerLegIndex].HeightProportion +
                                       BoneAnimData[_rightUpperLegIndex].HeightProportion);
            var targetLeftUpperLegPos = LeftUpperLegBone.GetLocalPosition(stream) +
                                        Vector3.Lerp(Vector3.zero, leftUpperLegOffset, weight);
            var targetRightUpperLegPos = RightUpperLegBone.GetLocalPosition(stream) +
                                         Vector3.Lerp(Vector3.zero, rightUpperLegOffset, weight);
            var targetLeftLowerLegPos = LeftLowerLegBone.GetPosition(stream) +
                                        Vector3.Lerp(Vector3.zero, leftLowerLegOffset, weight);
            var targetRightLowerLegPos = RightLowerLegBone.GetPosition(stream) +
                                         Vector3.Lerp(Vector3.zero, rightLowerLegOffset, weight);

            // Set bone positions.
            for (int i = HipsIndex; i < HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                bone.SetPosition(stream, UpperBodyTargetPositions[i]);
                HipsToHeadBones[i] = bone;
            }
            HeadBone.SetPosition(stream, _targetHeadPos);

            LeftUpperLegBone.SetLocalPosition(stream, targetLeftUpperLegPos);
            RightUpperLegBone.SetLocalPosition(stream, targetRightUpperLegPos);
            LeftLowerLegBone.SetPosition(stream, targetLeftLowerLegPos);
            RightLowerLegBone.SetPosition(stream, targetRightLowerLegPos);
        }

        /// <summary>
        /// Keep the hips accurate, adjusting the proportions of the lower body.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHipsSpineCorrection(AnimationStream stream, float weight)
        {
            _requiredSpineOffset = Vector3.zero;

            // The hips are not affected by any bone pairs. However, the hips grounding offset needs to be distributed
            // through the legs. Apply the full hips offset to the upper legs so it looks correct.
            var leftUpperLegOffset = -_hipsGroundingOffset;
            var rightUpperLegOffset = -_hipsGroundingOffset;
            var leftLowerLegOffset = -_hipsGroundingOffset *
                                     (BoneAnimData[_leftLowerLegIndex].LimbProportion +
                                      BoneAnimData[_leftUpperLegIndex].LimbProportion);
            var rightLowerLegOffset = -_hipsGroundingOffset *
                                      (BoneAnimData[_rightLowerLegIndex].LimbProportion +
                                       BoneAnimData[_rightUpperLegIndex].LimbProportion);

            var targetLeftUpperLegPos = LeftUpperLegBone.GetPosition(stream) +
                                        Vector3.Lerp(Vector3.zero, leftUpperLegOffset, weight);
            var targetRightUpperLegPos = RightUpperLegBone.GetPosition(stream) +
                                         Vector3.Lerp(Vector3.zero, rightUpperLegOffset, weight);
            var targetLeftLowerLegPos = LeftLowerLegBone.GetPosition(stream) +
                                        Vector3.Lerp(Vector3.zero, leftLowerLegOffset, weight);
            var targetRightLowerLegPos = RightLowerLegBone.GetPosition(stream) +
                                         Vector3.Lerp(Vector3.zero, rightLowerLegOffset, weight);

            HipsBone.SetPosition(stream, _targetHipsPos);
            LeftUpperLegBone.SetPosition(stream, targetLeftUpperLegPos);
            RightUpperLegBone.SetPosition(stream, targetRightUpperLegPos);
            LeftLowerLegBone.SetPosition(stream, targetLeftLowerLegPos);
            RightLowerLegBone.SetPosition(stream, targetRightLowerLegPos);
        }

        /// <summary>
        /// Keep the hips and head accurate, adjusting the proportions of the upper body.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHipsAndHeadSpineCorrection(AnimationStream stream, float weight)
        {
            // Calculate the offset between the current head and the tracked head to be undone by the hips
            // and the rest of the spine.
            var headOffset = _targetHeadPos - HeadBone.GetPosition(stream);
            _requiredSpineOffset = headOffset;

            UpperBodyOffsets[HipsIndex] = headOffset * BoneAnimData[HipsIndex].LimbProportion;
            for (int i = SpineLowerIndex; i < HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                UpperBodyOffsets[i] = UpperBodyOffsets[i - 1] + headOffset * BoneAnimData[i].LimbProportion;
                UpperBodyTargetPositions[i] = bone.GetPosition(stream) +
                                              Vector3.Lerp(Vector3.zero, UpperBodyOffsets[i], weight);
            }

            HipsBone.SetPosition(stream, _targetHipsPos);
            for (int i = SpineLowerIndex; i < HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                bone.SetPosition(stream, UpperBodyTargetPositions[i]);
                HipsToHeadBones[i] = bone;
            }
            HeadBone.SetPosition(stream, _targetHeadPos);
        }

        /// <summary>
        /// Sets the pre-deformation feet positions.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void ApplyAccurateFeet(AnimationStream stream, float weight)
        {
            var leftFootPos = LeftFootBone.GetPosition(stream);
            var rightFootPos = RightFootBone.GetPosition(stream);
            LeftFootBone.SetPosition(stream,
                Vector3.Lerp(leftFootPos, _preDeformationLeftFootPos, weight));
            RightFootBone.SetPosition(stream,
                Vector3.Lerp(rightFootPos, _preDeformationRightFootPos, weight));
        }

        /// <summary>
        /// Align the feet to toward the toes using the up axis from its original rotation.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void AlignFeet(AnimationStream stream, float weight)
        {
            if (!LeftToesBone.IsValid(stream) || !RightToesBone.IsValid(stream))
            {
                return;
            }
            var footAlignmentWeight = AlignFeetWeight.Get(stream) * weight;
            var originalLeftFootLocalRot = LeftFootBone.GetLocalRotation(stream);
            var originalRightFootLocalRot = RightFootBone.GetLocalRotation(stream);
            LeftFootBone.SetLocalRotation(stream, LeftFootLocalRot);
            RightFootBone.SetLocalRotation(stream, RightFootLocalRot);

            var leftFootTargetRotation = LeftFootLocalRot * GetRotationForFootAlignment(stream,
                                             _preDeformationLeftToesPos,
                                             LeftFootBone.GetRotation(stream) * Vector3.up,
                                             LeftFootBone, LeftToesBone);
            var rightFootTargetRotation = RightFootLocalRot * GetRotationForFootAlignment(stream,
                                             _preDeformationRightToesPos,
                                             RightFootBone.GetRotation(stream) * Vector3.up,
                                             RightFootBone, RightToesBone);

            LeftFootBone.SetLocalRotation(stream,
                Quaternion.Slerp(originalLeftFootLocalRot, leftFootTargetRotation, footAlignmentWeight));
            RightFootBone.SetLocalRotation(stream,
                Quaternion.Slerp(originalRightFootLocalRot, rightFootTargetRotation, footAlignmentWeight));
        }

        /// <summary>
        /// Gets the rotation around an axis for aligning the feet.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="originalToesPos">The original toes position.</param>
        /// <param name="rotateAxis">The axis to rotate around.</param>
        /// <param name="footBone">The foot bone.</param>
        /// <param name="toesBone">The toes bone.</param>
        /// <returns></returns>
        private Quaternion GetRotationForFootAlignment(AnimationStream stream,
            Vector3 originalToesPos, Vector3 rotateAxis,
            ReadWriteTransformHandle footBone, ReadWriteTransformHandle toesBone)
        {
            var footPosition = footBone.GetPosition(stream);
            var originalToesDir = footPosition - originalToesPos;
            // don't do anything if the foot has not been deformed from the original position
            if (originalToesDir.magnitude < Mathf.Epsilon)
            {
                return Quaternion.identity;
            }
            var targetToesDir = footPosition - toesBone.GetPosition(stream);
            var dot = Vector3.Dot(originalToesDir, targetToesDir);
            var cosineValue = dot / (originalToesDir.magnitude * targetToesDir.magnitude);

            var angle = Mathf.Acos(cosineValue);
            if (float.IsNaN(angle))
            {
                return Quaternion.identity;
            }
            return Quaternion.AngleAxis(angle, rotateAxis);
        }

        /// <summary>
        /// For each bone pair, where a bone pair has a start and end bone, enforce its original proportion by using
        /// the tracked direction of the bone, but the original size.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void EnforceOriginalSkeletalProportions(AnimationStream stream, float weight)
        {
            for (int i = 0; i < StartBones.Length; i++)
            {
                var startBone = StartBones[i];
                var endBone = EndBones[i];
                var startPos = startBone.GetPosition(stream);
                var endPos = endBone.GetPosition(stream);
                var data = BoneAnimData[i];

                var targetDir = Vector3.Scale(BoneDirections[i] * data.Distance, ScaleFactor[0]);
                var targetPos = startPos + targetDir;
                endBone.SetPosition(stream, Vector3.Lerp(endPos, targetPos, weight));
                StartBones[i] = startBone;
                EndBones[i] = endBone;
            }
        }

        /// <summary>
        /// Apply shoulders corrections.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyShouldersCorrection(AnimationStream stream, float weight)
        {
            // Adjust the y value of the shoulders.
            UpdateShoulderY(stream, weight, _leftShoulderIndex);
            UpdateShoulderY(stream, weight, _rightShoulderIndex);

            // Adjust the width of the shoulders.
            UpdateShoulderWidth(stream, weight, _leftShoulderIndex);
            UpdateShoulderWidth(stream, weight, _rightShoulderIndex);
        }

        private void UpdateShoulderY(AnimationStream stream, float weight, int index)
        {
            var shoulderBone = EndBones[index];
            var shoulderPos = shoulderBone.GetPosition(stream);
            var shoulderParentPos = HipsToHeadBones[ShouldersParentIndex].GetPosition(stream);
            shoulderParentPos.x = shoulderPos.x;
            shoulderParentPos.z = shoulderPos.z;

            weight *= ShouldersHeightAdjustmentWeight.Get(stream);

            shoulderBone.SetPosition(stream,
                Vector3.Lerp(shoulderPos, shoulderParentPos, weight));
            EndBones[index] = shoulderBone;
        }

        private void UpdateShoulderWidth(AnimationStream stream, float weight, int index)
        {
            var shoulderBone = EndBones[index];
            var shoulderPos = shoulderBone.GetPosition(stream);
            var shoulderParentPos = HipsToHeadBones[ShouldersParentIndex].GetPosition(stream);
            var spineParentPos = HipsToHeadBones[ShouldersParentIndex + 1].GetPosition(stream);

            var spineDir = (spineParentPos - shoulderParentPos).normalized;
            var shoulderToSpineDir = shoulderPos - shoulderParentPos;
            var shoulderSpinePos = shoulderParentPos + spineDir * Vector3.Dot(shoulderToSpineDir, spineDir);

            weight *= ShouldersWidthAdjustmentWeight.Get(stream);

            shoulderBone.SetPosition(stream,
                Vector3.Lerp(shoulderPos, shoulderSpinePos, weight));
            EndBones[index] = shoulderBone;
        }

        /// <summary>
        /// Interpolates the arm positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced. The hand positions can be incorrect after this function.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateArms(AnimationStream stream, float weight)
        {
            var leftLowerArmPos = LeftLowerArmBone.GetPosition(stream);
            var rightLowerArmPos = RightLowerArmBone.GetPosition(stream);
            var leftUpperArmPos = LeftUpperArmBone.GetPosition(stream);
            var rightUpperArmPos = RightUpperArmBone.GetPosition(stream);
            var leftArmOffsetWeight = weight * LeftArmOffsetWeight.Get(stream);
            var rightArmOffsetWeight = weight * RightArmOffsetWeight.Get(stream);

            LeftUpperArmBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationLeftUpperArmPos, leftUpperArmPos, leftArmOffsetWeight));
            RightUpperArmBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationRightUpperArmPos, rightUpperArmPos, rightArmOffsetWeight));
            LeftLowerArmBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationLeftLowerArmPos, leftLowerArmPos, leftArmOffsetWeight));
            RightLowerArmBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationRightLowerArmPos, rightLowerArmPos, rightArmOffsetWeight));
        }

        /// <summary>
        /// Interpolates the hand positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateHands(AnimationStream stream, float weight)
        {
            var leftHandPos = LeftHandBone.GetPosition(stream);
            var rightHandPos = RightHandBone.GetPosition(stream);
            var leftHandOffsetWeight = weight * LeftHandOffsetWeight.Get(stream);
            var rightHandOffsetWeight = weight * RightHandOffsetWeight.Get(stream);
            LeftHandBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationLeftHandPos, leftHandPos, leftHandOffsetWeight));
            RightHandBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationRightHandPos, rightHandPos, rightHandOffsetWeight));
        }

        /// <summary>
        /// Interpolates the leg positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced. The feet positions can be incorrect after this function
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateLegs(AnimationStream stream, float weight)
        {
            var leftLegOffsetWeight = weight * LeftLegOffsetWeight.Get(stream);
            var rightLegOffsetWeight = weight * RightLegOffsetWeight.Get(stream);
            _leftFootOffset = ApplyScaleAndWeight(
                _preDeformationLeftFootPos - LeftFootBone.GetPosition(stream), leftLegOffsetWeight);
            _rightFootOffset = ApplyScaleAndWeight(
                _preDeformationRightFootPos - RightFootBone.GetPosition(stream), rightLegOffsetWeight);

            var targetLeftUpperLegPos = LeftUpperLegBone.GetPosition(stream) + _leftFootOffset;
            var targetRightUpperLegPos = RightUpperLegBone.GetPosition(stream) + _rightFootOffset;
            var targetLeftLowerLegPos = LeftLowerLegBone.GetPosition(stream) + _leftFootOffset;
            var targetRightLowerLegPos = RightLowerLegBone.GetPosition(stream) + _rightFootOffset;
            var targetLeftFootPos = LeftFootBone.GetPosition(stream) + _leftFootOffset;
            var targetRightFootPos = RightFootBone.GetPosition(stream) + _rightFootOffset;

            LeftUpperLegBone.SetPosition(stream, targetLeftUpperLegPos);
            RightUpperLegBone.SetPosition(stream, targetRightUpperLegPos);
            LeftLowerLegBone.SetPosition(stream, targetLeftLowerLegPos);
            RightLowerLegBone.SetPosition(stream, targetRightLowerLegPos);
            LeftFootBone.SetPosition(stream, targetLeftFootPos);
            RightFootBone.SetPosition(stream, targetRightFootPos);
        }

        /// <summary>
        /// Interpolates the toes Y position from the pre-deformation positions to the original local positions after
        /// skeletal proportions are enforced.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateToesY(AnimationStream stream, float weight)
        {
            var leftToesOffsetWeight = weight * LeftToesOffsetWeight.Get(stream);
            var rightToesOffsetWeight = weight * RightToesOffsetWeight.Get(stream);

            // Modify only the y component of the toes.
            if (LeftToesBone.IsValid(stream))
            {
                var leftToesOffsetVector = ApplyScaleAndWeight(
                    Vector3.up * (LeftToesOriginalLocalPos.y - LeftToesBone.GetLocalPosition(stream).y),
                    leftToesOffsetWeight);
                var targetLeftToesPos = LeftToesBone.GetPosition(stream) + leftToesOffsetVector;
                LeftToesBone.SetPosition(stream, targetLeftToesPos);
            }

            if (RightToesBone.IsValid(stream))
            {
                var rightToesOffsetVector = ApplyScaleAndWeight(
                    Vector3.up * (RightToesOriginalLocalPos.y - RightToesBone.GetLocalPosition(stream).y),
                    rightToesOffsetWeight);
                var targetRightToesPos = RightToesBone.GetPosition(stream) + rightToesOffsetVector;
                RightToesBone.SetPosition(stream, targetRightToesPos);
            }
        }

        private Vector3 ApplyScaleAndWeight(Vector3 target, float weight)
        {
            return Vector3.Scale(Vector3.Lerp(Vector3.zero, target, weight), ScaleFactor[0]);
        }
    }

    /// <summary>
    /// The FullBodyDeformation job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class FullBodyDeformationJobBinder<T> : AnimationJobBinder<FullBodyDeformationJob, T>
        where T : struct, IAnimationJobData, IFullBodyDeformationData
    {
        /// <inheritdoc />
        public override FullBodyDeformationJob Create(Animator animator, ref T data, Component component)
        {
            var job = new FullBodyDeformationJob();

            job.HipsBone = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[0]);
            job.HeadBone = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[^1]);
            job.LeftShoulderBone = data.LeftArm.ShoulderBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.LeftArm.ShoulderBone) : new ReadWriteTransformHandle();
            job.RightShoulderBone = data.RightArm.ShoulderBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.RightArm.ShoulderBone) : new ReadWriteTransformHandle();
            job.LeftUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.UpperArmBone);
            job.LeftLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.LowerArmBone);
            job.RightUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.UpperArmBone);
            job.RightLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.LowerArmBone);
            job.LeftHandBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.HandBone);
            job.RightHandBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.HandBone);
            job.LeftUpperLegBone = ReadWriteTransformHandle.Bind(animator, data.LeftLeg.UpperLegBone);
            job.LeftLowerLegBone = ReadWriteTransformHandle.Bind(animator, data.LeftLeg.LowerLegBone);
            job.RightUpperLegBone = ReadWriteTransformHandle.Bind(animator, data.RightLeg.UpperLegBone);
            job.RightLowerLegBone = ReadWriteTransformHandle.Bind(animator, data.RightLeg.LowerLegBone);
            job.LeftFootBone = ReadWriteTransformHandle.Bind(animator, data.LeftLeg.FootBone);
            job.RightFootBone = ReadWriteTransformHandle.Bind(animator, data.RightLeg.FootBone);
            job.LeftToesBone = data.LeftLeg.ToesBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.LeftLeg.ToesBone) : new ReadWriteTransformHandle();
            job.RightToesBone = data.RightLeg.ToesBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.RightLeg.ToesBone) : new ReadWriteTransformHandle();

            job.StartBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.EndBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.HipsToHeadBones = new NativeArray<ReadWriteTransformHandle>(data.HipsToHeadBones.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.HipsToHeadBoneTargets = new NativeArray<ReadOnlyTransformHandle>(data.HipsToHeadBoneTargets.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyOffsets = new NativeArray<Vector3>(data.HipsToHeadBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyTargetPositions = new NativeArray<Vector3>(data.HipsToHeadBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneAnimData = new NativeArray<FullBodyDeformationJob.BoneAnimationData>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneDirections = new NativeArray<Vector3>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.ScaleFactor = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < data.HipsToHeadBones.Length; i++)
            {
                job.HipsToHeadBones[i] = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[i]);
                job.UpperBodyOffsets[i] = Vector3.zero;
                job.UpperBodyTargetPositions[i] = Vector3.zero;
            }

            for (int i = 0; i < data.HipsToHeadBoneTargets.Length; i++)
            {
                job.HipsToHeadBoneTargets[i] = ReadOnlyTransformHandle.Bind(animator, data.HipsToHeadBoneTargets[i]);
            }

            for (int i = 0; i < data.BonePairs.Length; i++)
            {
                var boneAnimData = new FullBodyDeformationJob.BoneAnimationData
                {
                    Distance = data.BonePairs[i].Distance,
                    HeightProportion = data.BonePairs[i].HeightProportion,
                    LimbProportion = data.BonePairs[i].LimbProportion
                };
                job.StartBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].StartBone);
                job.EndBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].EndBone);
                job.BoneAnimData[i] = boneAnimData;
            }

            job.SpineCorrectionType = IntProperty.Bind(animator, component, data.SpineCorrectionTypeIntProperty);
            job.SpineLowerAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineLowerAlignmentWeightFloatProperty);
            job.SpineUpperAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineUpperAlignmentWeightFloatProperty);
            job.ChestAlignmentWeight =
                FloatProperty.Bind(animator, component, data.ChestAlignmentWeightFloatProperty);
            job.ShouldersHeightAdjustmentWeight =
                FloatProperty.Bind(animator, component, data.ShouldersHeightAdjustmentWeightFloatProperty);
            job.ShouldersWidthAdjustmentWeight =
                FloatProperty.Bind(animator, component, data.ShouldersWidthAdjustmentWeightFloatProperty);
            job.ArmsHeightAdjustmentWeight =
                FloatProperty.Bind(animator, component, data.ArmsHeightAdjustmentWeightFloatProperty);
            job.LeftShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.LeftShoulderWeightFloatProperty);
            job.RightShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.RightShoulderWeightFloatProperty);
            job.LeftArmOffsetWeight = FloatProperty.Bind(animator, component, data.LeftArmWeightFloatProperty);
            job.RightArmOffsetWeight = FloatProperty.Bind(animator, component, data.RightArmWeightFloatProperty);
            job.LeftHandOffsetWeight = FloatProperty.Bind(animator, component, data.LeftHandWeightFloatProperty);
            job.RightHandOffsetWeight = FloatProperty.Bind(animator, component, data.RightHandWeightFloatProperty);
            job.LeftLegOffsetWeight = FloatProperty.Bind(animator, component, data.LeftLegWeightFloatProperty);
            job.RightLegOffsetWeight = FloatProperty.Bind(animator, component, data.RightLegWeightFloatProperty);
            job.LeftToesOffsetWeight = FloatProperty.Bind(animator, component, data.LeftToesWeightFloatProperty);
            job.RightToesOffsetWeight = FloatProperty.Bind(animator, component, data.RightToesWeightFloatProperty);
            job.AlignFeetWeight = FloatProperty.Bind(animator, component, data.AlignFeetWeightFloatProperty);

            job.HipsIndex = (int)HumanBodyBones.Hips;
            job.SpineLowerIndex = job.HipsIndex + 1;
            if (RiggingUtilities.IsHumanoidAnimator(animator))
            {
                job.SpineUpperIndex = animator.GetBoneTransform(HumanBodyBones.Chest) != null ?
                    job.SpineLowerIndex + 1 : -1;
                job.ChestIndex = animator.GetBoneTransform(HumanBodyBones.UpperChest) != null ?
                    job.SpineUpperIndex + 1 : -1;
            }
            else
            {
                job.SpineUpperIndex = job.SpineLowerIndex + 1;
                job.ChestIndex = job.SpineUpperIndex + 1;
            }
            job.ShouldersParentIndex = job.ChestIndex > 0 ? job.ChestIndex :
                job.SpineUpperIndex > 0 ? job.SpineUpperIndex : job.SpineLowerIndex;
            job.HeadIndex = data.HipsToHeadBones.Length;
            job.LeftToesOriginalLocalPos = data.LeftLeg.ToesLocalPos;
            job.RightToesOriginalLocalPos = data.RightLeg.ToesLocalPos;
            job.LeftShoulderOriginalLocalPos = data.LeftArm.ShoulderLocalPos;
            job.RightShoulderOriginalLocalPos = data.RightArm.ShoulderLocalPos;
            job.LeftFootLocalRot = data.LeftLeg.FootLocalRot;
            job.RightFootLocalRot = data.RightLeg.FootLocalRot;

            return job;
        }

        /// <inheritdoc />
        public override void Update(FullBodyDeformationJob job, ref T data)
        {
            if (data.IsBoneTransformsDataValid())
            {
                data.ShouldUpdate = true;
            }

            var currentScale = data.ConstraintCustomSkeleton != null
                ? data.ConstraintCustomSkeleton.transform.lossyScale
                : data.ConstraintAnimator.transform.lossyScale;
            job.ScaleFactor[0] =
                DivideVector3(currentScale, data.StartingScale);
            base.Update(job, ref data);

            if (!data.IsBoneTransformsDataValid())
            {
                data.ShouldUpdate = false;
            }
        }

        /// <inheritdoc />
        public override void Destroy(FullBodyDeformationJob job)
        {
            job.StartBones.Dispose();
            job.EndBones.Dispose();
            job.BoneAnimData.Dispose();
            job.HipsToHeadBones.Dispose();
            job.HipsToHeadBoneTargets.Dispose();
            job.UpperBodyOffsets.Dispose();
            job.UpperBodyTargetPositions.Dispose();
            job.BoneDirections.Dispose();
            job.ScaleFactor.Dispose();
        }

        private Vector3 DivideVector3(Vector3 dividend, Vector3 divisor)
        {
            Vector3 targetScale = Vector3.one;
            if (IsNonZero(divisor))
            {
                targetScale = new Vector3(
                    dividend.x / divisor.x, dividend.y / divisor.y, dividend.z / divisor.z);
            }

            return targetScale;
        }

        private bool IsNonZero(Vector3 v)
        {
            return v.x != 0 && v.y != 0 && v.z != 0;
        }
    }
}
