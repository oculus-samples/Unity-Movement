// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// The deformation job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct DeformationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// Bone animation data for the deformation job.
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
        /// The array of start bones for deformation.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> StartBones;

        /// <summary>
        /// The array of end bones for deformation.
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
        /// The local position of the left shoulder.
        /// </summary>
        public Vector3 LeftShoulderOriginalLocalPos;

        /// <summary>
        /// The local position of the right shoulder.
        /// </summary>
        public Vector3 RightShoulderOriginalLocalPos;

        /// <summary>
        /// The lower arm to hand axis for the left arm.
        /// </summary>
        public Vector3 LeftLowerArmToHandAxis;

        /// <summary>
        /// The lower arm to hand axis for the right arm.
        /// </summary>
        public Vector3 RightLowerArmToHandAxis;

        /// <summary>
        /// True if this is an OVRCustomSkeleton.
        /// </summary>
        public bool IsOVRCustomSkeleton;

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
        /// The head index in the bone pair data.
        /// </summary>
        public int HeadIndex;

        private Vector3 _targetHipsPos;
        private Vector3 _targetHeadPos;
        private Vector3 _preDeformationLeftUpperArmPos;
        private Vector3 _preDeformationRightUpperArmPos;
        private Vector3 _preDeformationLeftLowerArmPos;
        private Vector3 _preDeformationRightLowerArmPos;
        private Vector3 _preDeformationLeftHandPos;
        private Vector3 _preDeformationRightHandPos;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        /// <inheritdoc />
        public void ProcessRootMotion(AnimationStream stream) { }

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

                _preDeformationLeftUpperArmPos = LeftUpperArmBone.GetPosition(stream);
                _preDeformationRightUpperArmPos = RightUpperArmBone.GetPosition(stream);
                _preDeformationLeftLowerArmPos = LeftLowerArmBone.GetPosition(stream);
                _preDeformationRightLowerArmPos = RightLowerArmBone.GetPosition(stream);
                _preDeformationLeftHandPos = LeftHandBone.GetPosition(stream);
                _preDeformationRightHandPos = RightHandBone.GetPosition(stream);

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
                ApplySpineCorrection(stream, weight);
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
        /// adding an offset on spine bones to align with the hips.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void AlignSpine(AnimationStream stream, float weight)
        {
            var spineLowerOffset = Vector3.zero;
            var spineUpperOffset = Vector3.zero;
            var chestOffset = Vector3.zero;

            if (HipsToHeadBoneTargets[SpineLowerIndex].IsValid(stream))
            {
                spineLowerOffset = HipsToHeadBoneTargets[HipsIndex].GetPosition(stream) -
                              HipsToHeadBoneTargets[SpineLowerIndex].GetPosition(stream);
                spineLowerOffset.y = 0.0f;
            }

            if (SpineUpperIndex > 0 && HipsToHeadBoneTargets[SpineUpperIndex].IsValid(stream))
            {
                spineUpperOffset = (HipsToHeadBoneTargets[HipsIndex].GetPosition(stream) -
                                    HipsToHeadBoneTargets[SpineUpperIndex].GetPosition(stream)) * 0.5f +
                                   (HipsToHeadBones[HeadIndex - 1].GetPosition(stream) -
                                    HipsToHeadBoneTargets[SpineUpperIndex].GetPosition(stream)) * 0.5f;
                spineUpperOffset.y = 0.0f;
            }

            if (ChestIndex > 0 && HipsToHeadBoneTargets[ChestIndex].IsValid(stream))
            {
                chestOffset = (HipsToHeadBoneTargets[HipsIndex].GetPosition(stream) -
                              HipsToHeadBoneTargets[ChestIndex].GetPosition(stream)) * 0.25f +
                              (HipsToHeadBones[HeadIndex - 1].GetPosition(stream) -
                              HipsToHeadBoneTargets[ChestIndex].GetPosition(stream)) * 0.75f;
                chestOffset.y = 0.0f;
            }

            for (int i = SpineLowerIndex; i <= HeadIndex; i++)
            {
                var targetBone = HipsToHeadBoneTargets[i];
                var originalBone = HipsToHeadBones[i];
                if (targetBone.IsValid(stream))
                {
                    var spineCorrectionWeight = weight;
                    var spineOffset = Vector3.zero;

                    if (i == SpineLowerIndex)
                    {
                        spineOffset = Vector3.Lerp(Vector3.zero, spineLowerOffset, SpineLowerAlignmentWeight.Get(stream) * weight); ;
                    }
                    if (i == SpineUpperIndex)
                    {
                        spineOffset = Vector3.Lerp(Vector3.zero, spineUpperOffset, SpineUpperAlignmentWeight.Get(stream) * weight);
                    }
                    if (i == ChestIndex)
                    {
                        spineOffset = Vector3.Lerp(Vector3.zero, chestOffset, ChestAlignmentWeight.Get(stream) * weight);
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
                if (IsOVRCustomSkeleton)
                {
                    var chestPos = HipsToHeadBones[ChestIndex].GetPosition(stream);
                    var chestRot = HipsToHeadBones[ChestIndex].GetRotation(stream);
                    var shoulderWeight = weight * LeftShoulderOffsetWeight.Get(stream);
                    var shoulderPos = LeftShoulderBone.GetPosition(stream);
                    var targetShoulderPos = chestPos + chestRot * LeftShoulderOriginalLocalPos;
                    LeftShoulderBone.SetPosition(stream,
                        Vector3.Lerp(shoulderPos, targetShoulderPos, shoulderWeight));
                }
                else
                {
                    var leftShoulderLocalPos = LeftShoulderBone.GetLocalPosition(stream);
                    var leftShoulderOffset = Vector3.Lerp(Vector3.zero,
                        LeftShoulderOriginalLocalPos - leftShoulderLocalPos, weight * LeftShoulderOffsetWeight.Get(stream));
                    LeftShoulderBone.SetLocalPosition(stream, leftShoulderLocalPos + leftShoulderOffset);
                }
            }

            if (RightShoulderBone.IsValid(stream))
            {
                if (IsOVRCustomSkeleton)
                {
                    var chestPos = HipsToHeadBones[ChestIndex].GetPosition(stream);
                    var chestRot = HipsToHeadBones[ChestIndex].GetRotation(stream);
                    var shoulderWeight = weight * RightShoulderOffsetWeight.Get(stream);
                    var shoulderPos = RightShoulderBone.GetPosition(stream);
                    var targetShoulderPos = chestPos + chestRot * RightShoulderOriginalLocalPos;
                    RightShoulderBone.SetPosition(stream,
                        Vector3.Lerp(shoulderPos, targetShoulderPos, shoulderWeight));
                }
                else
                {
                    var rightShoulderLocalPos = RightShoulderBone.GetLocalPosition(stream);
                    var rightShoulderOffset = Vector3.Lerp(Vector3.zero,
                        RightShoulderOriginalLocalPos - rightShoulderLocalPos, weight * RightShoulderOffsetWeight.Get(stream));
                    RightShoulderBone.SetLocalPosition(stream, rightShoulderLocalPos + rightShoulderOffset);
                }
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
            // First, adjust the positions of the body based on the correction type.
            var spineCorrectionType = (DeformationData.SpineTranslationCorrectionType)
                SpineCorrectionType.Get(stream);

            if (spineCorrectionType == DeformationData.SpineTranslationCorrectionType.None)
            {
                ApplyNoSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == DeformationData.SpineTranslationCorrectionType.AccurateHead)
            {
                ApplyAccurateHeadSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == DeformationData.SpineTranslationCorrectionType.AccurateHips ||
                spineCorrectionType == DeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                ApplyAccurateHipsSpineCorrection(stream, weight);
            }

            if (spineCorrectionType == DeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead)
            {
                ApplyAccurateHipsAndHeadSpineCorrection(stream, weight);
            }

            // If this is the OVRCustomSkeleton, update the shoulders positions again as
            // the hierarchy is flattened and the changes to the chest doesn't update the shoulders.
            if (IsOVRCustomSkeleton)
            {
                InterpolateShoulders(stream, weight);
            }
        }

        /// <summary>
        /// Apply no spine correction.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyNoSpineCorrection(AnimationStream stream, float weight)
        {
        }

        /// <summary>
        /// Keep the head accurate, adjusting the proportions of the body.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHeadSpineCorrection(AnimationStream stream, float weight)
        {
            var headOffset = _targetHeadPos - HeadBone.GetPosition(stream);

            // Upper body.
            UpperBodyOffsets[HipsIndex] = headOffset * BoneAnimData[HipsIndex].LimbProportion;
            UpperBodyTargetPositions[HipsIndex] = HipsBone.GetPosition(stream) +
                                                  Vector3.Lerp(Vector3.zero, UpperBodyOffsets[HipsIndex], weight);
            for (int i = SpineLowerIndex; i <= HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                UpperBodyOffsets[i] = UpperBodyOffsets[i - 1] + headOffset * BoneAnimData[i].LimbProportion;
                UpperBodyTargetPositions[i] = bone.GetPosition(stream) +
                                              Vector3.Lerp(Vector3.zero, UpperBodyOffsets[i], weight);
            }

            // Set bone positions.
            for (int i = HipsIndex; i <= HeadIndex; i++)
            {
                var bone = HipsToHeadBones[i];
                bone.SetPosition(stream, UpperBodyTargetPositions[i]);
                HipsToHeadBones[i] = bone;
            }
            HeadBone.SetPosition(stream, _targetHeadPos);
        }

        /// <summary>
        /// Keep the hips accurate.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHipsSpineCorrection(AnimationStream stream, float weight)
        {
            HipsBone.SetPosition(stream, _targetHipsPos);
        }

        /// <summary>
        /// Keep the hips and head accurate, adjusting the proportions of the upper body.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="weight"></param>
        private void ApplyAccurateHipsAndHeadSpineCorrection(AnimationStream stream, float weight)
        {
            ApplyAccurateHeadSpineCorrection(stream, weight);
            HipsBone.SetPosition(stream, _targetHipsPos);
            HeadBone.SetPosition(stream, _targetHeadPos);
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

                var targetPos = startPos + Vector3.Scale(BoneDirections[i] * data.Distance, ScaleFactor[0]);
                endBone.SetPosition(stream, Vector3.Lerp(endPos, targetPos, weight));
                StartBones[i] = startBone;
                EndBones[i] = endBone;
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
        /// Interpolates the hand positions from the pre-deformation positions to positions relative to
        /// its elbows after skeletal proportions are enforced.
        /// </summary>
        /// <param name="stream">The animation stream.</param>
        /// <param name="weight">The weight of this operation.</param>
        private void InterpolateHands(AnimationStream stream, float weight)
        {
            var leftHandOffsetWeight = weight * LeftHandOffsetWeight.Get(stream);
            var rightHandOffsetWeight = weight * RightHandOffsetWeight.Get(stream);

            var leftHandPos = LeftHandBone.GetPosition(stream);
            var leftElbowPos = LeftLowerArmBone.GetPosition(stream);
            var leftElbowDir = LeftLowerArmBone.GetRotation(stream) * LeftLowerArmToHandAxis;
            leftHandPos = leftElbowPos + leftElbowDir * Vector3.Distance(leftHandPos, leftElbowPos);

            var rightHandPos = RightHandBone.GetPosition(stream);
            var rightElbowPos = RightLowerArmBone.GetPosition(stream);
            var rightElbowDir = RightLowerArmBone.GetRotation(stream) * RightLowerArmToHandAxis;
            rightHandPos = rightElbowPos + rightElbowDir * Vector3.Distance(rightHandPos, rightElbowPos);

            LeftHandBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationLeftHandPos, leftHandPos, leftHandOffsetWeight));
            RightHandBone.SetPosition(stream,
                Vector3.Lerp(_preDeformationRightHandPos, rightHandPos, rightHandOffsetWeight));
        }
    }

    /// <summary>
    /// The deformation job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class DeformationJobBinder<T> : AnimationJobBinder<DeformationJob, T>
        where T : struct, IAnimationJobData, IDeformationData
    {
        /// <inheritdoc />
        public override DeformationJob Create(Animator animator, ref T data, Component component)
        {
            var job = new DeformationJob();

            job.HipsBone = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[0]);
            job.HeadBone = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[^1]);
            job.LeftShoulderBone = data.LeftArm.ShoulderBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.LeftArm.ShoulderBone) :
                new ReadWriteTransformHandle();
            job.RightShoulderBone = data.RightArm.ShoulderBone != null ?
                ReadWriteTransformHandle.Bind(animator, data.RightArm.ShoulderBone) :
                new ReadWriteTransformHandle();
            job.LeftUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.UpperArmBone);
            job.LeftLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.LowerArmBone);
            job.RightUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.UpperArmBone);
            job.RightLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.LowerArmBone);
            job.LeftHandBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.HandBone);
            job.RightHandBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.HandBone);

            job.StartBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.EndBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.HipsToHeadBones = new NativeArray<ReadWriteTransformHandle>(data.HipsToHeadBones.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.HipsToHeadBoneTargets = new NativeArray<ReadOnlyTransformHandle>(data.HipsToHeadBoneTargets.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyOffsets = new NativeArray<Vector3>(data.HipsToHeadBones.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.UpperBodyTargetPositions = new NativeArray<Vector3>(data.HipsToHeadBones.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneAnimData = new NativeArray<DeformationJob.BoneAnimationData>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneDirections = new NativeArray<Vector3>(data.BonePairs.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.ScaleFactor = new NativeArray<Vector3>(1,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < data.HipsToHeadBones.Length; i++)
            {
                job.HipsToHeadBones[i] = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[i]);
                job.UpperBodyOffsets[i] = Vector3.zero;
                job.UpperBodyTargetPositions[i] = Vector3.zero;
            }

            for (int i = 0; i < data.HipsToHeadBoneTargets.Length; i++)
            {
                job.HipsToHeadBoneTargets[i] = data.HipsToHeadBoneTargets[i] != null ?
                    ReadOnlyTransformHandle.Bind(animator, data.HipsToHeadBoneTargets[i]) :
                    new ReadOnlyTransformHandle();
            }

            for (int i = 0; i < data.BonePairs.Length; i++)
            {
                var boneAnimData = new DeformationJob.BoneAnimationData
                {
                    Distance = data.BonePairs[i].Distance,
                    HeightProportion = data.BonePairs[i].HeightProportion,
                    LimbProportion = data.BonePairs[i].LimbProportion
                };
                job.StartBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].StartBone);
                job.EndBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].EndBone);
                job.BoneAnimData[i] = boneAnimData;
            }

            job.SpineCorrectionType =
                IntProperty.Bind(animator, component, data.SpineCorrectionTypeIntProperty);
            job.SpineLowerAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineLowerAlignmentWeightFloatProperty);
            job.SpineUpperAlignmentWeight =
                FloatProperty.Bind(animator, component, data.SpineUpperAlignmentWeightFloatProperty);
            job.ChestAlignmentWeight =
                FloatProperty.Bind(animator, component, data.ChestAlignmentWeightFloatProperty);
            job.LeftShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.LeftShoulderWeightFloatProperty);
            job.RightShoulderOffsetWeight =
                FloatProperty.Bind(animator, component, data.RightShoulderWeightFloatProperty);
            job.LeftArmOffsetWeight =
                FloatProperty.Bind(animator, component, data.LeftArmWeightFloatProperty);
            job.RightArmOffsetWeight =
                FloatProperty.Bind(animator, component, data.RightArmWeightFloatProperty);
            job.LeftHandOffsetWeight =
                FloatProperty.Bind(animator, component, data.LeftHandWeightFloatProperty);
            job.RightHandOffsetWeight =
                FloatProperty.Bind(animator, component, data.RightHandWeightFloatProperty);

            job.LeftShoulderOriginalLocalPos = data.LeftArm.ShoulderLocalPos;
            job.RightShoulderOriginalLocalPos = data.RightArm.ShoulderLocalPos;
            job.LeftLowerArmToHandAxis = data.LeftArm.LowerArmToHandAxis;
            job.RightLowerArmToHandAxis = data.RightArm.LowerArmToHandAxis;
            job.HipsIndex = (int)HumanBodyBones.Hips;
            job.SpineLowerIndex = job.HipsIndex + 1;
            job.SpineUpperIndex =
                (RiggingUtilities.IsHumanoidAnimator(animator) &&
                    animator.GetBoneTransform(HumanBodyBones.Chest) != null) ?
                    job.SpineLowerIndex + 1 : job.SpineLowerIndex + 2;
            job.ChestIndex = job.SpineUpperIndex + 1;
            job.HeadIndex = data.HipsToHeadBones.Length - 1;
            job.IsOVRCustomSkeleton = data.ConstraintCustomSkeleton != null;

            return job;
        }

        /// <inheritdoc />
        public override void Update(DeformationJob job, ref T data)
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
        public override void Destroy(DeformationJob job)
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
    }
}
