// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.FullBodyDeformationData;

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
        public struct BoneDeformationAnimationData
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
        /// Bone adjustment data for the FullBodyDeformation job.
        /// </summary>
        public struct BoneDeformationAdjustmentData
        {
            /// <summary>
            /// The main bone to be adjusted.
            /// </summary>
            public ReadWriteTransformHandle MainBone;

            /// <summary>
            /// The rotation adjustment amount.
            /// </summary>
            public Quaternion RotationAdjustment;

            /// <summary>
            /// The first child bone transform handle if present.
            /// </summary>
            public ReadWriteTransformHandle ChildBone1;

            /// <summary>
            /// The second child bone transform handle if present.
            /// </summary>
            public ReadWriteTransformHandle ChildBone2;

            /// <summary>
            /// The third child bone transform handle if present.
            /// </summary>
            public ReadWriteTransformHandle ChildBone3;

            /// <summary>
            /// True if the position should also be set when restoring the first child transform data.
            /// </summary>
            public bool SetPosition1;

            /// <summary>
            /// True if the position should also be set when restoring the second child transform data.
            /// </summary>
            public bool SetPosition2;

            /// <summary>
            /// True if the position should also be set when restoring the third child transform data.
            /// </summary>
            public bool SetPosition3;
        }

        /// <summary>
        /// The deformation body type.
        /// </summary>
        public IntProperty BodyType;

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
        /// The inclusive array of bone targets from the feet to the toes.
        /// </summary>
        public NativeArray<ReadOnlyTransformHandle> FeetToToesBoneTargets;

        /// <summary>
        /// The array of upper body offsets.
        /// </summary>
        public NativeArray<Vector3> UpperBodyOffsets;

        /// <summary>
        /// The array of upper body target positions.
        /// </summary>
        public NativeArray<Vector3> UpperBodyTargetPositions;

        /// <summary>
        /// The array of lower body target positions.
        /// </summary>
        public NativeArray<Vector3> LowerBodyTargetPositions;

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
        public NativeArray<BoneDeformationAnimationData> BoneAnimData;

        /// <summary>
        /// The array of bone adjustment data.
        /// </summary>
        public NativeArray<BoneDeformationAdjustmentData> BoneAdjustData;

        /// <summary>
        /// The array of directions between the start and end bones.
        /// </summary>
        public NativeArray<Vector3> BoneDirections;

        /// <summary>
        /// Original bone directions of the character.
        /// </summary>
        public NativeArray<Vector3> EndBoneOffsetsLocal;

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
        /// The weight to reduce the height of the shoulders.
        /// </summary>
        public FloatProperty ShouldersHeightReductionWeight;

        /// <summary>
        /// The weight to reduce the width of the shoulders.
        /// </summary>
        public FloatProperty ShouldersWidthReductionWeight;

        /// <summary>
        /// The weight of the left shoulder offset.
        /// </summary>
        public FloatProperty LeftShoulderOffsetWeight;

        /// <summary>
        /// The weight of the right shoulder offset.
        /// </summary>
        public FloatProperty RightShoulderOffsetWeight;

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
        /// The limit for squashing characters.
        /// </summary>
        public FloatProperty SquashLimit;

        /// <summary>
        /// The limit for stretching characters.
        /// </summary>
        public FloatProperty StretchLimit;

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
        /// Original spine positions weight.
        /// </summary>
        public FloatProperty OriginalSpinePositionsWeight;

        /// <summary>
        /// Shoulder roll weight.
        /// </summary>
        public FloatProperty ShoulderRollWeight;

        /// <summary>
        /// Number of bones to fix when straightening the spine.
        /// </summary>
        public IntProperty OriginalSpineBoneCount;

        /// <summary>
        /// Use the current hips to head to scale original spine positions.
        /// </summary>
        public BoolProperty OriginalSpineUseHipsToHeadToScale;

        /// <summary>
        /// True if the arms should be affected by spine correction.
        /// </summary>
        public BoolProperty AffectArmsBySpineCorrection;

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

        /// <summary>
        /// The lower body proportion.
        /// </summary>
        public float LowerBodyProportion;

        /// <summary>
        /// Hips to head distance on original character.
        /// </summary>
        public float OriginalHipsToHeadDistance;

        private Vector3 _targetHipsPos;
        private Vector3 _targetHeadPos;
        private Vector3 _preDeformationLeftUpperArmLocalPos;
        private Vector3 _preDeformationRightUpperArmLocalPos;
        private Vector3 _preDeformationLeftLowerArmLocalPos;
        private Vector3 _preDeformationRightLowerArmLocalPos;
        private Vector3 _preDeformationLeftHandLocalPos;
        private Vector3 _preDeformationRightHandLocalPos;

        private Vector3 _leftFootOffset;
        private Vector3 _rightFootOffset;
        private Vector3 _hipsGroundingOffset;
        private Vector3 _requiredSpineOffset;

        private int _leftShoulderEndBoneAnimIndex => HipsToHeadBones.Length - 1;
        private int _rightShoulderEndBoneAnimIndex => _leftShoulderEndBoneAnimIndex + 1;
        private int _leftUpperLegBoneAnimIndex => BoneAnimData.Length - 6;
        private int _rightUpperLegBoneAnimIndex => _leftUpperLegBoneAnimIndex + 1;
        private int _leftLowerLegBoneAnimIndex => _leftUpperLegBoneAnimIndex + 2;
        private int _rightLowerLegBoneAnimIndex => _leftLowerLegBoneAnimIndex + 1;

        private Vector3 _leftUpperLegTargetPos
        {
            get => LowerBodyTargetPositions[0];
            set => LowerBodyTargetPositions[0] = value;
        }
        private Vector3 _rightUpperLegTargetPos
        {
            get => LowerBodyTargetPositions[1];
            set => LowerBodyTargetPositions[1] = value;
        }
        private Vector3 _leftLowerLegTargetPos
        {
            get => LowerBodyTargetPositions[2];
            set => LowerBodyTargetPositions[2] = value;
        }
        private Vector3 _rightLowerLegTargetPos
        {
            get => LowerBodyTargetPositions[3];
            set => LowerBodyTargetPositions[3] = value;
        }
        private Vector3 _leftFootTargetPos
        {
            get => LowerBodyTargetPositions[4];
            set => LowerBodyTargetPositions[4] = value;
        }
        private Vector3 _rightFootTargetPos
        {
            get => LowerBodyTargetPositions[5];
            set => LowerBodyTargetPositions[5] = value;
        }

        private bool _isFullBody;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        public Quaternion RightShoulderOriginalLocalRot { get; set; }
        public Quaternion LeftShoulderOriginalLocalRot { get; set; }

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

                _isFullBody = (DeformationBodyType)BodyType.Get(stream) == DeformationBodyType.FullBody;
                if (_isFullBody)
                {
                    CacheDeformationUpperBodyTargetPositions(stream);
                    CacheDeformationLowerBodyTargetPositions(stream);
                    CachePreDeformationPositions(stream);
                    AlignSpine(stream, weight);
                    ApplyAdjustments(stream, weight);
                    InterpolateShoulders(stream, weight);
                    UpdateBoneDirections(stream);
                    EnforceOriginalSkeletalProportions(stream, weight);
                    CalculateOffsets(stream, weight);
                    InterpolateLegs(stream, weight);
                    ApplySpineCorrection(stream, weight);
                    ApplyOriginalSpineOffsets(stream, weight);
                    ApplyShouldersCorrection(stream, weight);
                    InterpolateArms(stream, weight);
                    InterpolateHands(stream, weight);
                    ApplyAccurateFeet(stream, weight);
                    AlignFeet(stream, weight);
                    InterpolateAllToes(stream, weight);
                }
                else
                {
                    CacheDeformationUpperBodyTargetPositions(stream);
                    CachePreDeformationPositions(stream);
                    AlignSpine(stream, weight);
                    ApplyAdjustments(stream, weight);
                    InterpolateShoulders(stream, weight);
                    UpdateBoneDirections(stream);
                    EnforceOriginalSkeletalProportions(stream, weight);
                    CalculateOffsets(stream, weight);
                    ApplySpineCorrection(stream, weight);
                    ApplyOriginalSpineOffsets(stream, weight);
                    ApplyShouldersCorrection(stream, weight);
                    InterpolateArms(stream, weight);
                    InterpolateHands(stream, weight);
                }
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
        /// Cache the deformation upper body target positions.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        private void CacheDeformationUpperBodyTargetPositions(AnimationStream stream)
        {
            _targetHipsPos = HipsToHeadBoneTargets[HipsIndex].GetPosition(stream);
            _targetHeadPos = HipsToHeadBoneTargets[^1].GetPosition(stream);
        }

        /// <summary>
        /// Cache the deformation lower body target positions.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        private void CacheDeformationLowerBodyTargetPositions(AnimationStream stream)
        {
            _leftUpperLegTargetPos = LeftUpperLegBone.GetPosition(stream);
            _rightUpperLegTargetPos = RightUpperLegBone.GetPosition(stream);
            _leftLowerLegTargetPos = LeftLowerLegBone.GetPosition(stream);
            _rightLowerLegTargetPos = RightLowerLegBone.GetPosition(stream);
            _leftFootTargetPos = LeftFootBone.GetPosition(stream);
            _rightFootTargetPos = RightFootBone.GetPosition(stream);
        }

        /// <summary>
        /// Cache the pre-deformation positions.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        private void CachePreDeformationPositions(AnimationStream stream)
        {
            _preDeformationLeftUpperArmLocalPos = LeftUpperArmBone.GetLocalPosition(stream);
            _preDeformationRightUpperArmLocalPos = RightUpperArmBone.GetLocalPosition(stream);
            _preDeformationLeftLowerArmLocalPos = LeftLowerArmBone.GetLocalPosition(stream);
            _preDeformationRightLowerArmLocalPos = RightLowerArmBone.GetLocalPosition(stream);
            _preDeformationLeftHandLocalPos = LeftHandBone.GetLocalPosition(stream);
            _preDeformationRightHandLocalPos = RightHandBone.GetLocalPosition(stream);
        }

        /// <summary>
        /// Apply bone adjustments.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void ApplyAdjustments(AnimationStream stream, float weight)
        {
            var spineAdjustmentWeight = weight;

            for (int i = 0; i < BoneAdjustData.Length; i++)
            {
                var data = BoneAdjustData[i];
                var previousRotation = data.MainBone.GetRotation(stream);
                var targetRotation = data.MainBone.GetRotation(stream) *
                    Quaternion.Slerp(Quaternion.identity, data.RotationAdjustment, spineAdjustmentWeight);
                data.MainBone.SetRotation(stream, targetRotation);

                var childAffineTransform1 = data.ChildBone1.IsValid(stream) ?
                    new AffineTransform(data.ChildBone1.GetPosition(stream), data.ChildBone1.GetRotation(stream)) :
                    AffineTransform.identity;
                var childAffineTransform2 = data.ChildBone2.IsValid(stream) ?
                    new AffineTransform(data.ChildBone2.GetPosition(stream), data.ChildBone2.GetRotation(stream)) :
                    AffineTransform.identity;
                var childAffineTransform3 = data.ChildBone3.IsValid(stream) ?
                    new AffineTransform(data.ChildBone3.GetPosition(stream), data.ChildBone3.GetRotation(stream)) :
                    AffineTransform.identity;
                data.MainBone.SetRotation(stream, previousRotation);

                // We want to restore the child positions after previewing what the parent adjustment would be.
                if (data.ChildBone1.IsValid(stream) && data.SetPosition1)
                {
                    data.ChildBone1.SetPosition(stream, childAffineTransform1.translation);
                }
                if (data.ChildBone2.IsValid(stream) && data.SetPosition2)
                {
                    data.ChildBone2.SetPosition(stream, childAffineTransform2.translation);
                }
                if (data.ChildBone3.IsValid(stream) && data.SetPosition3)
                {
                    data.ChildBone3.SetPosition(stream, childAffineTransform3.translation);
                }
            }
        }

        /// <summary>
        /// Align the spine positions with the tracked spine positions,
        /// adding an offset on spine bones to align with the hips for a straight spine.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void AlignSpine(AnimationStream stream, float weight)
        {
            if (SpineLowerAlignmentWeight.Get(stream) <= float.Epsilon &&
                SpineUpperAlignmentWeight.Get(stream) <= float.Epsilon &&
                ChestAlignmentWeight.Get(stream) <= float.Epsilon)
            {
                return;
            }

            var spineLowerOffset = Vector3.zero;
            var spineUpperOffset = Vector3.zero;
            var chestOffset = Vector3.zero;

            var hipsPosition = HipsToHeadBoneTargets[HipsIndex].GetPosition(stream);
            var headPosition = HipsToHeadBoneTargets[HeadIndex].GetPosition(stream);
            var accumulatedProportion = 0.0f;

            if (HipsToHeadBoneTargets[SpineLowerIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[SpineLowerIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[SpineLowerIndex - 1].LimbProportion;
                // Ensure that the spine lower bone is straight up from the hips bone.
                spineLowerOffset = hipsPosition - spineTargetPosition;
                spineLowerOffset.y = 0.0f;
            }

            if (SpineUpperIndex > 0 && HipsToHeadBoneTargets[SpineUpperIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[SpineUpperIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[SpineUpperIndex - 1].LimbProportion;
                spineUpperOffset = (hipsPosition - spineTargetPosition) * (1 - accumulatedProportion) +
                                   (headPosition - spineTargetPosition) * accumulatedProportion;
                spineUpperOffset.y = 0.0f;
            }

            if (ChestIndex > 0 && HipsToHeadBoneTargets[ChestIndex].IsValid(stream))
            {
                var spineTargetPosition = HipsToHeadBoneTargets[ChestIndex].GetPosition(stream);
                accumulatedProportion += BoneAnimData[ChestIndex - 1].LimbProportion;
                chestOffset = (hipsPosition - spineTargetPosition) * (1 - accumulatedProportion) +
                              (headPosition - spineTargetPosition) * accumulatedProportion;
                chestOffset.y = 0.0f;
            }

            for (int i = SpineLowerIndex; i <= HeadIndex; i++)
            {
                var originalBone = HipsToHeadBones[i];
                var targetBone = HipsToHeadBoneTargets[i];
                if (targetBone.IsValid(stream))
                {
                    var spineCorrectionWeight = 0.0f;
                    var spineOffset = Vector3.zero;

                    if (i == SpineLowerIndex)
                    {
                        spineOffset = spineLowerOffset;
                        spineCorrectionWeight = SpineLowerAlignmentWeight.Get(stream) * weight;
                    }
                    else if (i == SpineUpperIndex)
                    {
                        spineOffset = spineUpperOffset;
                        spineCorrectionWeight = SpineUpperAlignmentWeight.Get(stream) * weight;
                    }
                    else if (i == ChestIndex)
                    {
                        spineOffset = chestOffset;
                        spineCorrectionWeight = ChestAlignmentWeight.Get(stream) * weight;
                    }

                    var originalPos = originalBone.GetPosition(stream);
                    var targetPos = targetBone.GetPosition(stream) + spineOffset;
                    targetPos.y = originalPos.y;
                    var endPos = Vector3.Lerp(originalPos, targetPos, spineCorrectionWeight);
                    originalBone.SetPosition(stream, endPos);
                }
                HipsToHeadBoneTargets[i] = targetBone;
                HipsToHeadBones[i] = originalBone;
            }
        }

        /// <summary>
        /// Tries to align the spine bones with the original character's bone directions.
        /// We still need to maintain the original hip rotation, and that the head bone is
        /// not touched since we don't to lose head tracking accuracy.
        /// </summary>
        /// <param name="stream">Animation stream.</param>
        /// <param name="weight">Weight.</param>
        private void ApplyOriginalSpineOffsets(AnimationStream stream, float weight)
        {
            float originalSpinePositionsWeight = OriginalSpinePositionsWeight.Get(stream);
            if (originalSpinePositionsWeight <= float.Epsilon)
            {
                return;
            }
            originalSpinePositionsWeight *= weight;
            bool scaleBasedOnCurrentHeadToHips = OriginalSpineUseHipsToHeadToScale.Get(stream);

            // make sure the requested bone count does not exceed the head index
            int requestedBoneCount = OriginalSpineBoneCount.Get(stream);
            int numBonesToCorrect = (HeadIndex - 1) < requestedBoneCount ?
                HeadIndex - 1 : requestedBoneCount;
            float currentToOriginalSpineScale = ComputeCurrentToOriginalSpineScale(stream);

            for (int i = HipsIndex; i < numBonesToCorrect; i++)
            {
                var boneToAffect = EndBones[i];
                // we need the offset from the bone before to tell us where our
                // current spine bone should be
                var boneOffset = EndBoneOffsetsLocal[i];

                if (scaleBasedOnCurrentHeadToHips)
                {
                    boneOffset *= currentToOriginalSpineScale;
                }

                AffectBoneByFixedLocalPosition(
                    stream,
                    boneToAffect,
                    boneOffset,
                    originalSpinePositionsWeight);

                EndBones[i] = boneToAffect;
            }
        }

        private float ComputeCurrentToOriginalSpineScale(AnimationStream stream)
        {
            // accumulate the distance from hips to head
            float currentTotalHipsToHeadDistance = 0.0f;
            for (int i = HipsIndex; i < HeadIndex; i++)
            {
                var spineVector = EndBones[i].GetPosition(stream) - StartBones[i].GetPosition(stream);
                currentTotalHipsToHeadDistance += spineVector.magnitude;
            }
            return Mathf.Abs(OriginalHipsToHeadDistance) > Mathf.Epsilon ?
                currentTotalHipsToHeadDistance / OriginalHipsToHeadDistance : 0.0f;
        }

        private void AffectBoneByFixedLocalPosition(
            AnimationStream stream,
            ReadWriteTransformHandle boneToAffect,
            Vector3 boneFixedLocalPosition,
            float originalSpinePositionsWeight)
        {
            if (!boneToAffect.IsValid(stream))
            {
                return;
            }

            var boneOriginalLocalPos = boneToAffect.GetLocalPosition(stream);
            var lerpPosition = Vector3.Lerp(boneOriginalLocalPos,
                boneFixedLocalPosition, originalSpinePositionsWeight);
            boneToAffect.SetLocalPosition(stream, lerpPosition);
        }

        /// <summary>
        /// Optionally interpolate from the current position to the original local position of the shoulders,
        /// as the tracked positions may be mismatched.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void InterpolateShoulders(AnimationStream stream, float weight)
        {
            if (LeftShoulderBone.IsValid(stream))
            {
                var leftShoulderLocalPos = LeftShoulderBone.GetLocalPosition(stream);
                var leftShoulderOffset = Vector3.Lerp(Vector3.zero,
                    LeftShoulderOriginalLocalPos - leftShoulderLocalPos, weight * LeftShoulderOffsetWeight.Get(stream));
                LeftShoulderBone.SetLocalPosition(stream, leftShoulderLocalPos + leftShoulderOffset);
            }
            else
            {
                var leftUpperArmLocalPos = LeftUpperArmBone.GetLocalPosition(stream);
                var leftUpperArmOffset = Vector3.Lerp(Vector3.zero,
                    LeftShoulderOriginalLocalPos - leftUpperArmLocalPos, weight * LeftShoulderOffsetWeight.Get(stream));
                LeftUpperArmBone.SetLocalPosition(stream, leftUpperArmLocalPos + leftUpperArmOffset);
            }

            if (RightShoulderBone.IsValid(stream))
            {
                var rightShoulderLocalPos = RightShoulderBone.GetLocalPosition(stream);
                var rightShoulderOffset = Vector3.Lerp(Vector3.zero,
                    RightShoulderOriginalLocalPos - rightShoulderLocalPos, weight * RightShoulderOffsetWeight.Get(stream));
                RightShoulderBone.SetLocalPosition(stream, rightShoulderLocalPos + rightShoulderOffset);
            }
            else
            {
                var rightUpperArmPos = RightUpperArmBone.GetLocalPosition(stream);
                var rightUpperArmOffset = Vector3.Lerp(Vector3.zero,
                    RightShoulderOriginalLocalPos - rightUpperArmPos, weight * RightShoulderOffsetWeight.Get(stream));
                RightUpperArmBone.SetLocalPosition(stream, rightUpperArmPos + rightUpperArmOffset);
            }
        }

        /// <summary>
        /// Update the bone directions between each bone pair.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
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
        /// Calculates deformation offsets.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void CalculateOffsets(AnimationStream stream, float weight)
        {
            // Zero out offsets.
            _leftFootOffset = Vector3.zero;
            _rightFootOffset = Vector3.zero;
            _requiredSpineOffset = Vector3.zero;

            // Calculate the foot offsets to be applied to the legs.
            if (_isFullBody)
            {
                var leftLegOffsetWeight = weight * LeftLegOffsetWeight.Get(stream);
                var rightLegOffsetWeight = weight * RightLegOffsetWeight.Get(stream);

                var constrainedLeftFootTargetPos = DeformationUtilities.GetJointPositionSquashStretch(
                    _leftFootTargetPos, LeftFootBone.GetPosition(stream),
                    HipsToHeadBones[HipsIndex].GetPosition(stream),
                    StretchLimit.Get(stream), SquashLimit.Get(stream));
                _leftFootOffset = ApplyScaleAndWeight(
                    constrainedLeftFootTargetPos - LeftFootBone.GetPosition(stream),
                    leftLegOffsetWeight);

                var constrainedRightFootTargetPos = DeformationUtilities.GetJointPositionSquashStretch(
                    _rightFootTargetPos, RightFootBone.GetPosition(stream),
                    HipsToHeadBones[HipsIndex].GetPosition(stream),
                    StretchLimit.Get(stream), SquashLimit.Get(stream));
                _rightFootOffset = ApplyScaleAndWeight(
                    constrainedRightFootTargetPos - RightFootBone.GetPosition(stream),
                    rightLegOffsetWeight);
            }

            // This hips offset is the offset required to preserve leg lengths when feet are planted.
            _hipsGroundingOffset = (_leftFootOffset + _rightFootOffset) / 2f;
        }

        /// <summary>
        /// Applies spine correction, depending on the type picked.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void ApplySpineCorrection(AnimationStream stream, float weight)
        {
            // First, adjust the positions of the body based on the correction type.
            var spineCorrectionType = (SpineTranslationCorrectionType)
                SpineCorrectionType.Get(stream);

            if (spineCorrectionType == SpineTranslationCorrectionType.None)
            {
                ApplyNoSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == SpineTranslationCorrectionType.AccurateHead)
            {
                var constrainedTargetHeadPos = DeformationUtilities.GetJointPositionSquashStretch(
                    _targetHeadPos, HipsToHeadBones[HeadIndex].GetPosition(stream),
                    HipsToHeadBones[HipsIndex].GetPosition(stream),
                    StretchLimit.Get(stream), SquashLimit.Get(stream));
                ApplyAccurateHeadSpineCorrection(stream, weight, constrainedTargetHeadPos);
            }

            if (spineCorrectionType == SpineTranslationCorrectionType.AccurateHips ||
                spineCorrectionType == SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                ApplyAccurateHipsSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                var constrainedTargetHeadPos = DeformationUtilities.GetJointPositionSquashStretch(
                    _targetHeadPos, HipsToHeadBones[HeadIndex].GetPosition(stream),
                    HipsToHeadBones[HipsIndex].GetPosition(stream),
                    StretchLimit.Get(stream), SquashLimit.Get(stream));
                ApplyAccurateHipsAndHeadSpineCorrection(stream, weight, constrainedTargetHeadPos);
            }

            // Update upper arm bones based on the corrections applied to the spine from the shoulders.
            if (spineCorrectionType == SpineTranslationCorrectionType.AccurateHead ||
                spineCorrectionType == SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                var shoulderHeightAdjustment = _requiredSpineOffset.magnitude;
                if (shoulderHeightAdjustment <= float.Epsilon ||
                    !AffectArmsBySpineCorrection.Get(stream))
                {
                    return;
                }

                var shouldersParentPos = HipsToHeadBones[ShouldersParentIndex].GetPosition(stream);
                if (RightShoulderBone.IsValid(stream))
                {
                    var rightShoulderPos = RightShoulderBone.GetPosition(stream);
                    var rightShoulderOffset = (shouldersParentPos - rightShoulderPos).normalized *
                                              shoulderHeightAdjustment * BoneAnimData[_rightShoulderEndBoneAnimIndex].LimbProportion;
                    RightUpperArmBone.SetPosition(stream, RightUpperArmBone.GetPosition(stream) +
                                                         Vector3.Lerp(Vector3.zero, rightShoulderOffset, weight));
                }
                if (LeftShoulderBone.IsValid(stream))
                {
                    var leftShoulderPos = LeftShoulderBone.GetPosition(stream);
                    var leftShoulderOffset = (shouldersParentPos - leftShoulderPos).normalized *
                                             shoulderHeightAdjustment * BoneAnimData[_leftShoulderEndBoneAnimIndex].LimbProportion;
                    LeftUpperArmBone.SetPosition(stream, LeftUpperArmBone.GetPosition(stream) +
                                                         Vector3.Lerp(Vector3.zero, leftShoulderOffset, weight));
                }
            }
        }

        /// <summary>
        /// Adjust the hips by the foot offset, then adjust the legs afterwards.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void ApplyNoSpineCorrection(AnimationStream stream, float weight)
        {
            if (!_isFullBody)
            {
                return;
            }

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
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        /// <param name="constrainedTargetHeadPos">Constrained target head position.</param>
        private void ApplyAccurateHeadSpineCorrection(
            AnimationStream stream,
            float weight,
            Vector3 constrainedTargetHeadPos)
        {
            if (!_isFullBody)
            {
                CalculateUpperBodyTargetPositions(stream, weight, true, constrainedTargetHeadPos);
                ApplyUpperBodyTargetPositions(stream, HipsIndex, constrainedTargetHeadPos);
                return;
            }

            // Upper body.
            CalculateUpperBodyTargetPositions(stream, weight, false, constrainedTargetHeadPos);

            // Lower body.
            var leftUpperLegOffset = UpperBodyOffsets[HipsIndex] - _hipsGroundingOffset;
            var rightUpperLegOffset = UpperBodyOffsets[HipsIndex] - _hipsGroundingOffset;
            var leftLowerLegOffset = _requiredSpineOffset *
                                     (BoneAnimData[_leftLowerLegBoneAnimIndex].HeightProportion +
                                      BoneAnimData[_leftUpperLegBoneAnimIndex].HeightProportion);
            var rightLowerLegOffset = _requiredSpineOffset *
                                      (BoneAnimData[_rightLowerLegBoneAnimIndex].HeightProportion +
                                       BoneAnimData[_rightUpperLegBoneAnimIndex].HeightProportion);
            _leftUpperLegTargetPos = LeftUpperLegBone.GetPosition(stream) +
                                          Vector3.Lerp(Vector3.zero, leftUpperLegOffset, weight);
            _rightUpperLegTargetPos = RightUpperLegBone.GetPosition(stream) +
                                          Vector3.Lerp(Vector3.zero, rightUpperLegOffset, weight);
            _leftLowerLegTargetPos = LeftLowerLegBone.GetPosition(stream) +
                                          Vector3.Lerp(Vector3.zero, leftLowerLegOffset, weight);
            _rightLowerLegTargetPos = RightLowerLegBone.GetPosition(stream) +
                                          Vector3.Lerp(Vector3.zero, rightLowerLegOffset, weight);

            // Keep the feet grounded vertically but offset by the lower leg offset.
            var targetLeftFootPos = _leftFootTargetPos;
            var targetRightFootPos = _rightFootTargetPos;
            var targetLeftFootY = targetLeftFootPos.y;
            var targetRightFootY = targetRightFootPos.y;
            targetLeftFootPos += Vector3.Lerp(Vector3.zero, leftLowerLegOffset, weight);
            targetRightFootPos += Vector3.Lerp(Vector3.zero, rightLowerLegOffset, weight);
            targetLeftFootPos.y = targetLeftFootY;
            targetRightFootPos.y = targetRightFootY;
            _leftFootTargetPos = targetLeftFootPos;
            _rightFootTargetPos = targetRightFootPos;

            // Set bone positions.
            ApplyUpperBodyTargetPositions(stream, HipsIndex, constrainedTargetHeadPos);
            ApplyLowerBodyTargetPositions(stream, false);
        }

        /// <summary>
        /// Keep the hips accurate, adjusting the proportions of the lower body.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void ApplyAccurateHipsSpineCorrection(AnimationStream stream, float weight)
        {
            if (!_isFullBody)
            {
                HipsBone.SetPosition(stream, _targetHipsPos);
                return;
            }

            // The hips are not affected by any bone pairs. However, the hips grounding offset needs to be distributed
            // through the legs. Apply the full hips offset to the upper legs so it looks correct.
            var leftUpperLegOffset = -_hipsGroundingOffset;
            var rightUpperLegOffset = -_hipsGroundingOffset;
            var leftLowerLegOffset = -_hipsGroundingOffset *
                                     (BoneAnimData[_leftLowerLegBoneAnimIndex].LimbProportion +
                                      BoneAnimData[_leftUpperLegBoneAnimIndex].LimbProportion);
            var rightLowerLegOffset = -_hipsGroundingOffset *
                                      (BoneAnimData[_rightLowerLegBoneAnimIndex].LimbProportion +
                                       BoneAnimData[_rightUpperLegBoneAnimIndex].LimbProportion);

            _leftUpperLegTargetPos = LeftUpperLegBone.GetPosition(stream) +
                                        Vector3.Lerp(Vector3.zero, leftUpperLegOffset, weight);
            _rightUpperLegTargetPos = RightUpperLegBone.GetPosition(stream) +
                                     Vector3.Lerp(Vector3.zero, rightUpperLegOffset, weight);
            _leftLowerLegTargetPos = LeftLowerLegBone.GetPosition(stream) +
                                     Vector3.Lerp(Vector3.zero, leftLowerLegOffset, weight);
            _rightLowerLegTargetPos = RightLowerLegBone.GetPosition(stream) +
                                     Vector3.Lerp(Vector3.zero, rightLowerLegOffset, weight);

            HipsBone.SetPosition(stream, _targetHipsPos);
            ApplyLowerBodyTargetPositions(stream, false);
        }

        /// <summary>
        /// Keep the hips and head accurate, adjusting the proportions of the upper body.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        /// <param name="constrainedTargetHeadPos">Constrained target head position.</param>
        private void ApplyAccurateHipsAndHeadSpineCorrection(
            AnimationStream stream,
            float weight,
            Vector3 constrainedTargetHeadPos)
        {
            CalculateUpperBodyTargetPositions(stream, weight, true, constrainedTargetHeadPos);
            HipsBone.SetPosition(stream, _targetHipsPos);
            ApplyUpperBodyTargetPositions(stream, SpineLowerIndex, constrainedTargetHeadPos);
        }

        /// <summary>
        /// Calculate the upper body target positions.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="useLimbProportion">True if the limb proportions should be used.</param>
        /// <param name="constrainedTargetHeadPosition">Target head, constrained.</param>
        private void CalculateUpperBodyTargetPositions(
            AnimationStream stream,
            float limit,
            bool useLimbProportion,
            Vector3 constrainedTargetHeadPosition)
        {
            // Calculate the offset between the current head and the tracked head to be undone by the hips
            // and the rest of the spine.
            var headOffset = constrainedTargetHeadPosition - HeadBone.GetPosition(stream);
            _requiredSpineOffset = headOffset;

            var hipsProportion = useLimbProportion ?
                BoneAnimData[HipsIndex].LimbProportion :
                BoneAnimData[HipsIndex].HeightProportion;
            // Since this constraint needs to work on flat hierarchy characters (like OVRCustomSkeleton),
            // we need to positions in world space.
            UpperBodyOffsets[HipsIndex] = headOffset * (hipsProportion +
                                                       (useLimbProportion ? 0.0f : LowerBodyProportion));
            UpperBodyTargetPositions[HipsIndex] = HipsBone.GetPosition(stream) +
                                                  Vector3.Lerp(Vector3.zero, UpperBodyOffsets[HipsIndex], limit);
            for (int i = SpineLowerIndex; i <= HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                var proportion = useLimbProportion ?
                    BoneAnimData[i].LimbProportion : BoneAnimData[i].HeightProportion;
                UpperBodyOffsets[i] = UpperBodyOffsets[i - 1] + headOffset * proportion;
                UpperBodyTargetPositions[i] = bone.GetPosition(stream) +
                                              Vector3.Lerp(Vector3.zero, UpperBodyOffsets[i], limit);
            }
        }

        /// <summary>
        /// Sets the positions of the upper body from a starting spine index.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="startSpineIndex">The starting spine index to set the positions from.</param>
        /// <param name="constrainedTargetHeadPos">Target head, constrained.</param>
        private void ApplyUpperBodyTargetPositions(
            AnimationStream stream,
            int startSpineIndex,
            Vector3 constrainedTargetHeadPos)
        {
            for (int i = startSpineIndex; i <= HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                bone.SetPosition(stream, UpperBodyTargetPositions[i]);
                HipsToHeadBones[i] = bone;
            }
            HeadBone.SetPosition(stream, constrainedTargetHeadPos);
        }

        /// <summary>
        /// Sets the positions of the lower body.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="updateFeet">True if the feet positions should also be updated.</param>
        private void ApplyLowerBodyTargetPositions(AnimationStream stream, bool updateFeet)
        {
            LeftUpperLegBone.SetPosition(stream, _leftUpperLegTargetPos);
            RightUpperLegBone.SetPosition(stream, _rightUpperLegTargetPos);
            LeftLowerLegBone.SetPosition(stream, _leftLowerLegTargetPos);
            RightLowerLegBone.SetPosition(stream, _rightLowerLegTargetPos);
            if (updateFeet)
            {
                LeftFootBone.SetPosition(stream, _leftFootTargetPos);
                RightFootBone.SetPosition(stream, _rightFootTargetPos);
            }
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
                Vector3.Lerp(leftFootPos, _leftFootTargetPos, weight));
            RightFootBone.SetPosition(stream,
                Vector3.Lerp(rightFootPos, _rightFootTargetPos, weight));
        }

        /// <summary>
        /// Align the feet to toward the toes using the up axis from its original rotation.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void AlignFeet(AnimationStream stream, float weight)
        {
            var footAlignmentWeight = AlignFeetWeight.Get(stream) * weight;
            var originalLeftFootLocalRot = LeftFootBone.GetLocalRotation(stream);
            var originalRightFootLocalRot = RightFootBone.GetLocalRotation(stream);
            if (!LeftToesBone.IsValid(stream) || !RightToesBone.IsValid(stream))
            {
                LeftFootBone.SetLocalRotation(stream,
                    Quaternion.Slerp(originalLeftFootLocalRot, LeftFootLocalRot, footAlignmentWeight));
                RightFootBone.SetLocalRotation(stream,
                    Quaternion.Slerp(originalRightFootLocalRot, RightFootLocalRot, footAlignmentWeight));
                return;
            }

            var originalLeftFootToesDir = LeftToesBone.GetPosition(stream) - LeftFootBone.GetPosition(stream);
            var originalRightFootToesDir = RightToesBone.GetPosition(stream) - RightFootBone.GetPosition(stream);
            var targetLeftFootToesDir = originalLeftFootToesDir;
            var targetRightFootToesDir = originalRightFootToesDir;
            if (FeetToToesBoneTargets.Length > 0)
            {
                targetLeftFootToesDir = FeetToToesBoneTargets[1].GetPosition(stream) -
                                            FeetToToesBoneTargets[0].GetPosition(stream);
                targetRightFootToesDir = FeetToToesBoneTargets[3].GetPosition(stream) -
                                             FeetToToesBoneTargets[2].GetPosition(stream);
            }

            var leftFootTargetRotation = LeftFootLocalRot * GetRotationForFootAlignment(
                LeftFootLocalRot * Vector3.up,
                originalLeftFootToesDir, targetLeftFootToesDir);
            var rightFootTargetRotation = RightFootLocalRot * GetRotationForFootAlignment(
                RightFootLocalRot * Vector3.up,
                originalRightFootToesDir, targetRightFootToesDir);

            LeftFootBone.SetLocalRotation(stream,
                Quaternion.Slerp(originalLeftFootLocalRot, leftFootTargetRotation, footAlignmentWeight));
            RightFootBone.SetLocalRotation(stream,
                Quaternion.Slerp(originalRightFootLocalRot, rightFootTargetRotation, footAlignmentWeight));
        }

        /// <summary>
        /// Gets the rotation around an axis for aligning the feet.
        /// </summary>
        /// <param name="rotateAxis">The axis to rotate around.</param>
        /// <param name="originalFootToesDir">The original foot to toes direction.</param>
        /// <param name="targetFootToesDir">The target foot to toes direction.</param>
        /// <returns>The rotation between the foot to toes directions around an axis.</returns>
        private Quaternion GetRotationForFootAlignment(
            Vector3 rotateAxis, Vector3 originalFootToesDir, Vector3 targetFootToesDir)
        {
            var footAngle = Vector3.Angle(originalFootToesDir, targetFootToesDir);
            return Quaternion.AngleAxis(footAngle, rotateAxis);
        }

        /// <summary>
        /// For each bone pair, where a bone pair has a start and end bone, enforce its original proportion by using
        /// the tracked direction of the bone, but the original size.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void EnforceOriginalSkeletalProportions(AnimationStream stream, float weight)
        {
            var bonesLength = StartBones.Length;
            if (!_isFullBody)
            {
                bonesLength = _leftUpperLegBoneAnimIndex;
            }
            for (int i = 0; i < bonesLength; i++)
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
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">Constraint weight.</param>
        private void ApplyShouldersCorrection(AnimationStream stream, float weight)
        {
            // Adjust the y value of the shoulders.
            UpdateShoulderY(stream, weight, _leftShoulderEndBoneAnimIndex);
            UpdateShoulderY(stream, weight, _rightShoulderEndBoneAnimIndex);

            // Adjust the width of the shoulders.
            UpdateShoulderWidth(stream, weight, _leftShoulderEndBoneAnimIndex);
            UpdateShoulderWidth(stream, weight, _rightShoulderEndBoneAnimIndex);

            // Rotate shoulders, but keep upper arm rotations.
            UpdateShoulderRoll(stream, weight);
        }

        private void UpdateShoulderY(AnimationStream stream, float weight, int index)
        {
            var shoulderBone = EndBones[index];
            var shoulderPos = shoulderBone.GetPosition(stream);
            var shoulderParentPos = HipsToHeadBones[ShouldersParentIndex].GetPosition(stream);
            shoulderParentPos.x = shoulderPos.x;
            shoulderParentPos.z = shoulderPos.z;

            weight *= ShouldersHeightReductionWeight.Get(stream);

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

            weight *= ShouldersWidthReductionWeight.Get(stream);

            shoulderBone.SetPosition(stream,
                Vector3.Lerp(shoulderPos, shoulderSpinePos, weight));
            EndBones[index] = shoulderBone;
        }

        private void UpdateShoulderRoll(AnimationStream stream, float weight)
        {
            var shoulderRollWeight = weight * ShoulderRollWeight.Get(stream);
            if (LeftShoulderBone.IsValid(stream) && RightShoulderBone.IsValid(stream))
            {
                var leftShoulderRotation = LeftShoulderBone.GetLocalRotation(stream);
                var rightShoulderRotation = RightShoulderBone.GetLocalRotation(stream);
                var leftUpperArmRotation = LeftUpperArmBone.GetRotation(stream);
                var rightUpperArmRotation = RightUpperArmBone.GetRotation(stream);
                var leftTargetShoulderRotation = LeftShoulderOriginalLocalRot;
                var rightTargetShoulderRotation = RightShoulderOriginalLocalRot;
                LeftShoulderBone.SetLocalRotation(stream,
                    Quaternion.Slerp(
                        leftShoulderRotation, leftTargetShoulderRotation, shoulderRollWeight));
                RightShoulderBone.SetLocalRotation(stream,
                    Quaternion.Slerp(
                        rightShoulderRotation, rightTargetShoulderRotation, shoulderRollWeight));
                LeftUpperArmBone.SetRotation(stream, leftUpperArmRotation);
                RightUpperArmBone.SetRotation(stream, rightUpperArmRotation);
            }
            else
            {
                // If shoulder bones aren't valid, use the upper arm bone rotations.
                LeftUpperArmBone.SetLocalRotation(stream, LeftShoulderOriginalLocalRot);
                RightUpperArmBone.SetLocalRotation(stream, RightShoulderOriginalLocalRot);
            }
        }

        /// <summary>
        /// Interpolates the arm positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced. The hand positions can be incorrect after this function.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateArms(AnimationStream stream, float weight)
        {
            var leftUpperArmPos = LeftUpperArmBone.GetLocalPosition(stream);
            var rightUpperArmPos = RightUpperArmBone.GetLocalPosition(stream);
            var leftLowerArmPos = LeftLowerArmBone.GetLocalPosition(stream);
            var rightLowerArmPos = RightLowerArmBone.GetLocalPosition(stream);
            var leftArmOffsetWeight = weight * LeftArmOffsetWeight.Get(stream);
            var rightArmOffsetWeight = weight * RightArmOffsetWeight.Get(stream);

            LeftUpperArmBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationLeftUpperArmLocalPos, leftUpperArmPos, leftArmOffsetWeight));
            RightUpperArmBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationRightUpperArmLocalPos, rightUpperArmPos, rightArmOffsetWeight));
            LeftLowerArmBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationLeftLowerArmLocalPos, leftLowerArmPos, leftArmOffsetWeight));
            RightLowerArmBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationRightLowerArmLocalPos, rightLowerArmPos, rightArmOffsetWeight));
        }

        /// <summary>
        /// Interpolates the hand positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateHands(AnimationStream stream, float weight)
        {
            var leftHandPos = LeftHandBone.GetLocalPosition(stream);
            var rightHandPos = RightHandBone.GetLocalPosition(stream);
            var leftHandOffsetWeight = weight * LeftHandOffsetWeight.Get(stream);
            var rightHandOffsetWeight = weight * RightHandOffsetWeight.Get(stream);
            LeftHandBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationLeftHandLocalPos, leftHandPos, leftHandOffsetWeight));
            RightHandBone.SetLocalPosition(stream,
                Vector3.Lerp(_preDeformationRightHandLocalPos, rightHandPos, rightHandOffsetWeight));
        }

        /// <summary>
        /// Interpolates the leg positions from the pre-deformation positions to the positions after skeletal
        /// proportions are enforced. The feet positions can be incorrect after this function.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateLegs(AnimationStream stream, float weight)
        {
            _leftUpperLegTargetPos = LeftUpperLegBone.GetPosition(stream) + _leftFootOffset;
            _rightUpperLegTargetPos = RightUpperLegBone.GetPosition(stream) + _rightFootOffset;
            _leftLowerLegTargetPos = LeftLowerLegBone.GetPosition(stream) + _leftFootOffset;
            _rightLowerLegTargetPos = RightLowerLegBone.GetPosition(stream) + _rightFootOffset;
            _leftFootTargetPos = LeftFootBone.GetPosition(stream) + _leftFootOffset;
            _rightFootTargetPos = RightFootBone.GetPosition(stream) + _rightFootOffset;

            ApplyLowerBodyTargetPositions(stream, true);
        }

        /// <summary>
        /// Influence the toe positions based on what their original
        /// local positions were. This should be run after the character's
        /// proportions are enforced.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateAllToes(AnimationStream stream, float weight)
        {
            if (LeftToesBone.IsValid(stream) && LeftFootBone.IsValid(stream))
            {
                InterpolateToes(stream, LeftToesBone, LeftToesOriginalLocalPos,
                    weight * LeftToesOffsetWeight.Get(stream));
            }

            if (RightToesBone.IsValid(stream) && RightFootBone.IsValid(stream))
            {
                InterpolateToes(stream, RightToesBone, RightToesOriginalLocalPos,
                    weight * RightToesOffsetWeight.Get(stream));
            }
        }

        private void InterpolateToes(AnimationStream stream,
            ReadWriteTransformHandle toesBone,
            Vector3 toesOriginalLocalPos,
            float toesOffsetWeight)
        {
            var offsetToOriginalToesLocal = toesOriginalLocalPos - toesBone.GetLocalPosition(stream);
            // We need to interpolate the offset, and scale it as well. THEN add it to compute
            // the final position.
            var scaledAndWeightLocalOffset = ApplyScaleAndWeight(
                offsetToOriginalToesLocal, toesOffsetWeight);
            var adjustedToesLocalPos = toesBone.GetLocalPosition(stream) + scaledAndWeightLocalOffset;
            toesBone.SetLocalPosition(stream, adjustedToesLocalPos);
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

            var bonesToResetPositions = new List<HumanBodyBones>()
            {
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftShoulder,
                HumanBodyBones.RightShoulder,
                HumanBodyBones.Neck
            };
            var lowerBodyBones = new[]
            {
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot
            };

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
            job.FeetToToesBoneTargets = new NativeArray<ReadOnlyTransformHandle>(data.FeetToToesBoneTargets.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyOffsets = new NativeArray<Vector3>(data.HipsToHeadBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyTargetPositions = new NativeArray<Vector3>(data.HipsToHeadBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.LowerBodyTargetPositions = new NativeArray<Vector3>(lowerBodyBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneAnimData = new NativeArray<FullBodyDeformationJob.BoneDeformationAnimationData>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneAdjustData = new NativeArray<FullBodyDeformationJob.BoneDeformationAdjustmentData>(data.BoneAdjustments.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneDirections = new NativeArray<Vector3>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.EndBoneOffsetsLocal = new NativeArray<Vector3>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.ScaleFactor = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.OriginalHipsToHeadDistance = data.HipsToHeadDistance;

            var upperBodyProportion = 0.0f;
            for (int i = 0; i < data.HipsToHeadBones.Length; i++)
            {
                job.HipsToHeadBones[i] = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[i]);
                job.UpperBodyOffsets[i] = Vector3.zero;
                job.UpperBodyTargetPositions[i] = Vector3.zero;
                upperBodyProportion += data.BonePairs[i].HeightProportion;
            }
            job.LowerBodyProportion = 1.0f - upperBodyProportion;

            for (int i = 0; i < data.HipsToHeadBoneTargets.Length; i++)
            {
                job.HipsToHeadBoneTargets[i] = ReadOnlyTransformHandle.Bind(animator, data.HipsToHeadBoneTargets[i]);
            }

            for (int i = 0; i < data.FeetToToesBoneTargets.Length; i++)
            {
                job.FeetToToesBoneTargets[i] = ReadOnlyTransformHandle.Bind(animator, data.FeetToToesBoneTargets[i]);
            }

            for (int i = 0; i < data.BonePairs.Length; i++)
            {
                var boneAnimData = new FullBodyDeformationJob.BoneDeformationAnimationData
                {
                    Distance = data.BonePairs[i].Distance,
                    HeightProportion = data.BonePairs[i].HeightProportion,
                    LimbProportion = data.BonePairs[i].LimbProportion
                };
                job.StartBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].StartBone);
                job.EndBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].EndBone);
                job.BoneAnimData[i] = boneAnimData;

                job.EndBoneOffsetsLocal[i] = data.BonePairs[i].EndBoneLocalOffsetFromStart;
                if (job.EndBoneOffsetsLocal[i].magnitude < Mathf.Epsilon)
                {
                    Debug.LogWarning("End bone offsets invalid for deformation job, please run" +
                        " bone calculation.");
                }
            }

            for (int i = 0; i < data.BoneAdjustments.Length; i++)
            {
                var boneAdjustment = data.BoneAdjustments[i];
                var boneAdjustmentData = new FullBodyDeformationJob.BoneDeformationAdjustmentData()
                {
                    MainBone = ReadWriteTransformHandle.Bind(animator, animator.GetBoneTransform(boneAdjustment.Bone)),
                    RotationAdjustment = boneAdjustment.Adjustment,
                    ChildBone1 =
                        boneAdjustment.ChildBone1 != HumanBodyBones.LastBone ?
                            ReadWriteTransformHandle.Bind(animator,
                                animator.GetBoneTransform(boneAdjustment.ChildBone1)) :
                            new ReadWriteTransformHandle(),
                    ChildBone2 =
                        boneAdjustment.ChildBone2 != HumanBodyBones.LastBone ?
                            ReadWriteTransformHandle.Bind(animator,
                                animator.GetBoneTransform(boneAdjustment.ChildBone2)) :
                            new ReadWriteTransformHandle(),
                    ChildBone3 =
                        boneAdjustment.ChildBone3 != HumanBodyBones.LastBone ?
                            ReadWriteTransformHandle.Bind(animator,
                                animator.GetBoneTransform(boneAdjustment.ChildBone3)) :
                            new ReadWriteTransformHandle(),
                    SetPosition1 = !bonesToResetPositions.Contains(boneAdjustment.ChildBone1),
                    SetPosition2 = !bonesToResetPositions.Contains(boneAdjustment.ChildBone2),
                    SetPosition3 = !bonesToResetPositions.Contains(boneAdjustment.ChildBone3),
                };
                job.BoneAdjustData[i] = boneAdjustmentData;
            }

            job.BodyType = IntProperty.Bind(animator, component, data.DeformationBodyTypeIntProperty);
            job.SpineCorrectionType = IntProperty.Bind(animator, component, data.SpineCorrectionTypeIntProperty);
            job.SpineLowerAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineLowerAlignmentWeightFloatProperty);
            job.SpineUpperAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineUpperAlignmentWeightFloatProperty);
            job.ChestAlignmentWeight =
                FloatProperty.Bind(animator, component, data.ChestAlignmentWeightFloatProperty);
            job.ShouldersHeightReductionWeight =
                FloatProperty.Bind(animator, component, data.ShouldersHeightReductionWeightFloatProperty);
            job.ShouldersWidthReductionWeight =
                FloatProperty.Bind(animator, component, data.ShouldersWidthReductionWeightFloatProperty);
            job.AffectArmsBySpineCorrection =
                BoolProperty.Bind(animator, component, data.AffectArmsBySpineCorrection);
            job.LeftShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.LeftShoulderWeightFloatProperty);
            job.RightShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.RightShoulderWeightFloatProperty);
            job.ShoulderRollWeight =
                FloatProperty.Bind(animator, component, data.ShoulderRollFloatProperty);
            job.OriginalSpinePositionsWeight =
                FloatProperty.Bind(animator, component, data.OriginalSpinePositionsWeightProperty);
            job.OriginalSpineBoneCount =
                IntProperty.Bind(animator, component, data.OriginalSpineBoneCountIntProperty);
            job.OriginalSpineUseHipsToHeadToScale =
                BoolProperty.Bind(animator, component, data.OriginalSpineUseHipsToHeadScaleBoolProperty);
            job.LeftArmOffsetWeight = FloatProperty.Bind(animator, component, data.LeftArmWeightFloatProperty);
            job.RightArmOffsetWeight = FloatProperty.Bind(animator, component, data.RightArmWeightFloatProperty);
            job.LeftHandOffsetWeight = FloatProperty.Bind(animator, component, data.LeftHandWeightFloatProperty);
            job.RightHandOffsetWeight = FloatProperty.Bind(animator, component, data.RightHandWeightFloatProperty);
            job.LeftLegOffsetWeight = FloatProperty.Bind(animator, component, data.LeftLegWeightFloatProperty);
            job.RightLegOffsetWeight = FloatProperty.Bind(animator, component, data.RightLegWeightFloatProperty);
            job.LeftToesOffsetWeight = FloatProperty.Bind(animator, component, data.LeftToesWeightFloatProperty);
            job.RightToesOffsetWeight = FloatProperty.Bind(animator, component, data.RightToesWeightFloatProperty);
            job.AlignFeetWeight = FloatProperty.Bind(animator, component, data.AlignFeetWeightFloatProperty);
            job.SquashLimit =
                FloatProperty.Bind(animator, component, data.SquashLimitFloatProperty);
            job.StretchLimit =
                FloatProperty.Bind(animator, component, data.StretchLimitFloatProperty);

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
            job.HeadIndex = data.HipsToHeadBones.Length - 1;
            job.LeftToesOriginalLocalPos = data.LeftLeg.ToesLocalPos;
            job.RightToesOriginalLocalPos = data.RightLeg.ToesLocalPos;
            job.LeftShoulderOriginalLocalPos = data.LeftArm.ShoulderLocalPos;
            job.RightShoulderOriginalLocalPos = data.RightArm.ShoulderLocalPos;
            job.LeftShoulderOriginalLocalRot = data.LeftArm.ShoulderLocalRot;
            job.RightShoulderOriginalLocalRot = data.RightArm.ShoulderLocalRot;
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
                RiggingUtilities.DivideVector3(currentScale, data.StartingScale);

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
            job.BoneAdjustData.Dispose();
            job.HipsToHeadBones.Dispose();
            job.HipsToHeadBoneTargets.Dispose();
            job.FeetToToesBoneTargets.Dispose();
            job.UpperBodyOffsets.Dispose();
            job.UpperBodyTargetPositions.Dispose();
            job.LowerBodyTargetPositions.Dispose();
            job.BoneDirections.Dispose();
            job.EndBoneOffsetsLocal.Dispose();
            job.ScaleFactor.Dispose();
        }
    }
}
