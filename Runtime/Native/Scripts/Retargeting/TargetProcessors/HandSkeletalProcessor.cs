// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting.IK;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Forces hand bones to match a custom hand and runs IK for the rest of the bones.
    /// </summary>
    [Serializable]
    public class HandSkeletalProcessor : TargetProcessor
    {
        /// <summary>
        /// Job used to quickly store poses from custom hand transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct GetCustomHandLocalPosesJob : IJobParallelForTransform
        {
            /// <summary>
            /// Poses to write to.
            /// </summary>
            [WriteOnly]
            public NativeArray<Pose> Poses;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid)
                {
                    Poses[index] = new Pose(transform.localPosition, transform.localRotation);
                }
            }
        }

        /// <summary>
        /// Job used to apply stored poses to transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct WriteCharacterHandLocalPosesJob : IJob
        {
            /// <summary>
            /// Poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<Pose> CustomHandPoses;

            /// <summary>
            /// Indices on target.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> TargetIndices;

            /// <summary>
            /// Interpolation weight.
            /// </summary>
            [ReadOnly]
            public float Weight;

            /// <summary>
            /// Poses to write to.
            /// </summary>
            public NativeArray<NativeTransform> OutputPoses;

            /// <inheritdoc cref="IJob.Execute()"/>
            public void Execute()
            {
                for (var i = 0; i < CustomHandPoses.Length; i++)
                {
                    var sourcePose = CustomHandPoses[i];
                    var targetIndex = TargetIndices[i];
                    var targetPose = OutputPoses[targetIndex];
                    var lerpPosition = Vector3.Lerp(targetPose.Position, sourcePose.position, Weight);
                    var lerpRot = Quaternion.Slerp(targetPose.Orientation, sourcePose.rotation, Weight);
                    OutputPoses[targetIndex] = new NativeTransform(lerpRot, lerpPosition, targetPose.Scale);
                }
            }
        }

        /// <summary>
        /// Each target skeleton joint should be matched with a
        /// custom hand transform that we retarget to. The former must be an index,
        /// because we use that index to access a native transform array.
        /// </summary>
        private class TargetToCustomHandAssociation
        {
            /// <summary>
            /// Target joint index.
            /// </summary>
            public int TargetIndex;

            /// <summary>
            /// Custom hand transform.
            /// </summary>
            public Transform CustomHandTransform;
        }

        /// <summary>
        /// Data container for IK computations.
        /// </summary>
        private class ArmIKData
        {
            /// <summary>
            /// Target to custom hand association
            /// </summary>
            public TargetToCustomHandAssociation[] TargetToCustomHand;

            /// <summary>
            /// Arm joints in reverse order (expected for IK). The end effector is first, and the last
            /// index corresponds to the first joint.
            /// </summary>
            public int[] ArmJointIndicesReversed;

            /// <summary>
            /// Source poses.
            /// </summary>
            public NativeArray<Pose> CustomHandPoses;

            /// <summary>
            /// Source transforms on custom hand.
            /// </summary>
            public TransformAccessArray CustomHandTransforms;

            /// <summary>
            /// Indices on target skeleton.
            /// </summary>
            public NativeArray<int> TargetIndices;

            /// <summary>
            /// Get custom hand poses job.
            /// </summary>
            public GetCustomHandLocalPosesJob GetCustomHandPosesJob;

            /// <summary>
            /// Cleans up.
            /// </summary>
            public void CleanUp()
            {
                if (CustomHandPoses.IsCreated)
                {
                    CustomHandPoses.Dispose();
                }

                if (CustomHandTransforms.isCreated)
                {
                    CustomHandTransforms.Dispose();
                }

                if (TargetIndices.IsCreated)
                {
                    TargetIndices.Dispose();
                }
            }
        }

        /// <summary>
        /// The types of IK available to be used.
        /// </summary>
        public enum IKAlgorithm
        {
            None,
            CCDIK
        }

        /// <summary>
        /// Gets or sets the hands transform.
        /// </summary>
        /// <remarks>
        /// The root transform for the hand hierarchy that will be used for retargeting.
        /// </remarks>
        public Transform HandsTransform
        {
            get => _handsTransform;
            set => _handsTransform = value;
        }

        /// <summary>
        /// Gets or sets the left hand transform name.
        /// </summary>
        /// <remarks>
        /// The name of the left hand transform in the skeleton hierarchy, typically "LeftHand".
        /// </remarks>
        public string LeftHandTransformName
        {
            get => _leftHandTransformName;
            set => _leftHandTransformName = value;
        }

        /// <summary>
        /// Gets or sets the right hand transform name.
        /// </summary>
        /// <remarks>
        /// The name of the right hand transform in the skeleton hierarchy, typically "RightHand".
        /// </remarks>
        public string RightHandTransformName
        {
            get => _rightHandTransformName;
            set => _rightHandTransformName = value;
        }

        /// <summary>
        /// Gets or sets the left hand joints.
        /// </summary>
        /// <remarks>
        /// Array of transforms representing the joints in the left hand that will be used for retargeting.
        /// These are the custom hand joints we are retargeting to.
        /// </remarks>
        public Transform[] LeftHandJoints
        {
            get => _leftHandJoints;
            set => _leftHandJoints = value;
        }

        /// <summary>
        /// Gets or sets the right hand joints.
        /// </summary>
        /// <remarks>
        /// Array of transforms representing the joints in the right hand that will be used for retargeting.
        /// These are the custom hand joints we are retargeting to.
        /// </remarks>
        public Transform[] RightHandJoints
        {
            get => _rightHandJoints;
            set => _rightHandJoints = value;
        }

        /// <summary>
        /// Gets or sets whether to use retargeted hand rotation.
        /// </summary>
        /// <remarks>
        /// When enabled, the processor will use the retargeted hand rotation data instead of the original rotation.
        /// </remarks>
        public bool UseRetargetedHandRotation
        {
            get => _useRetargetedHandRotation;
            set => _useRetargetedHandRotation = value;
        }

        /// <summary>
        /// Gets or sets whether to match fingers.
        /// </summary>
        /// <remarks>
        /// When enabled, the processor will match the finger positions and rotations from the custom hand.
        /// </remarks>
        public bool MatchFingers
        {
            get => _matchFingers;
            set => _matchFingers = value;
        }

        /// <summary>
        /// Gets or sets whether to limit stretching.
        /// </summary>
        /// <remarks>
        /// When enabled, the processor will limit the amount of stretching that can occur during retargeting.
        /// </remarks>
        public bool LimitStretch
        {
            get => _limitStretch;
            set => _limitStretch = value;
        }

        /// <summary>
        /// Gets or sets the number of IK iterations.
        /// </summary>
        /// <remarks>
        /// The number of iterations to perform when solving the IK chain. Higher values provide more accurate results
        /// but require more computation. Ten iterations is typically recommended.
        /// </remarks>
        public int IkIterations
        {
            get => _ikIterations;
            set => _ikIterations = value;
        }

        /// <summary>
        /// Gets or sets the IK tolerance.
        /// </summary>
        /// <remarks>
        /// The tolerance threshold for the IK solver. Lower values provide more accurate results but may require
        /// more iterations. A value of 1e-6f is commonly used.
        /// </remarks>
        public float IkTolerance
        {
            get => _ikTolerance;
            set => _ikTolerance = value;
        }

        /// <summary>
        /// Gets or sets whether to run code in serial mode.
        /// </summary>
        /// <remarks>
        /// When enabled, the processor will execute operations serially instead of using parallel jobs.
        /// This may be useful for debugging or on platforms with limited threading support.
        /// </remarks>
        public bool RunSerial
        {
            get => _runSerial;
            set => _runSerial = value;
        }

        /// <summary>
        /// Gets or sets the IK algorithm to run.
        /// </summary>
        /// <remarks>
        /// The inverse kinematics algorithm that will be used for solving the arm chain.
        /// </remarks>
        public IKAlgorithm Algorithm
        {
            get => _algorithm;
            set => _algorithm = value;
        }

        /// <summary>
        /// Hand transform.
        /// </summary>
        [SerializeField]
        private Transform _handsTransform;

        /// <summary>
        /// Left hand name. Can be "LeftHand."
        /// </summary>
        [SerializeField]
        private string _leftHandTransformName;

        /// <summary>
        /// Right hand name. Can be "RightHand."
        /// </summary>
        [SerializeField]
        private string _rightHandTransformName;

        /// <summary>
        /// Left hand joints, this is on the custom hand joints we are retargeting to.
        /// </summary>
        [SerializeField]
        private Transform[] _leftHandJoints;

        /// <summary>
        /// Right hand joints, this is on the custom hand joints we are retargeting to.
        /// </summary>
        [SerializeField]
        private Transform[] _rightHandJoints;

        /// <summary>
        /// If we want to use retargeted hand rotation.
        /// </summary>
        [SerializeField]
        private bool _useRetargetedHandRotation;

        /// <summary>
        /// Enable to match fingers.
        /// </summary>
        [SerializeField]
        private bool _matchFingers;

        /// <summary>
        /// Enable to limit stretching.
        /// </summary>
        [SerializeField]
        private bool _limitStretch;

        /// <summary>
        /// Number of total IK iterations. Ten recommended.
        /// </summary>
        [SerializeField]
        private int _ikIterations;

        /// <summary>
        /// IK tolerance. 1e-6f is one possible value.
        /// </summary>
        [SerializeField]
        private float _ikTolerance;

        /// <summary>
        /// Run code in serial mode.
        /// </summary>
        [SerializeField]
        private bool _runSerial;

        /// <summary>
        /// IK algorithm to run.
        /// </summary>
        [SerializeField]
        private IKAlgorithm _algorithm;

        // Native Utility.
        private CharacterRetargeter _retargeter;
        private int[] _parentJointIndicesTarget;
        private ArmIKData _leftArmIKData;
        private ArmIKData _rightArmIKData;

        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter retargeter)
        {
            _retargeter = retargeter;
            Assert.IsNotNull(_handsTransform);
            Assert.IsTrue(_leftHandJoints is { Length: > 0 });
            Assert.IsTrue(_rightHandJoints is { Length: > 0 });

            if (!GetParentJointIndexes(retargeter.RetargetingHandle, SkeletonType.TargetSkeleton,
                    out var parentJointIndicesTarget))
            {
                throw new Exception("Failed to obtain joint data from configuration");
            }

            _parentJointIndicesTarget = parentJointIndicesTarget.ToArray();
            var pluginHandle = retargeter.SkeletonRetargeter.NativeHandle;
            GetKnownJointName(pluginHandle, SkeletonType.TargetSkeleton, KnownJointType.LeftWrist,
                out var leftWristJointName);

            GetKnownJointName(pluginHandle, SkeletonType.TargetSkeleton, KnownJointType.RightWrist,
                out var rightWristJointName);

            var leftWrist = retargeter.transform.FindChildRecursive(leftWristJointName);
            var rightWrist = retargeter.transform.FindChildRecursive(rightWristJointName);
            var characterLeftHandJoints = leftWrist.GetComponentsInChildren<Transform>(true);
            var characterRightHandJoints = rightWrist.GetComponentsInChildren<Transform>(true);

            GetKnownJointName(pluginHandle, SkeletonType.TargetSkeleton, KnownJointType.LeftUpperArm,
                out var leftShoulderName);
            GetKnownJointName(pluginHandle, SkeletonType.TargetSkeleton, KnownJointType.RightUpperArm,
                out var rightShoulderName);
            _leftArmIKData = new ArmIKData();
            _rightArmIKData = new ArmIKData();

            var armBones =
                FindTransformsDownChain(retargeter.transform.FindChildRecursive(leftShoulderName), leftWrist);
            armBones.Reverse();

            // For each arm bone, find the corresponding index in the config.
            InitializeArmJointIndices(pluginHandle, armBones, _leftArmIKData);
            Assert.IsTrue(_leftArmIKData.ArmJointIndicesReversed.Length > 0, "IK must have at least one arm joint.");

            armBones = FindTransformsDownChain(
                retargeter.transform.FindChildRecursive(rightShoulderName), rightWrist);
            armBones.Reverse();
            InitializeArmJointIndices(pluginHandle, armBones, _rightArmIKData);
            Assert.IsTrue(_rightArmIKData.ArmJointIndicesReversed.Length > 0, "IK must have at least one arm joint.");

            var targetToCustomHandTuples = new List<Tuple<Transform, Transform>>();
            FillJointMappings(
                characterLeftHandJoints,
                _leftHandJoints,
                ref targetToCustomHandTuples);
            InitializeTargetToCustomHandAssociation(pluginHandle, targetToCustomHandTuples, _leftArmIKData);
            Assert.IsTrue(_leftArmIKData != null && _leftArmIKData.TargetToCustomHand.Length > 0);

            targetToCustomHandTuples.Clear();
            FillJointMappings(
                characterRightHandJoints,
                _rightHandJoints,
                ref targetToCustomHandTuples);
            InitializeTargetToCustomHandAssociation(pluginHandle, targetToCustomHandTuples, _rightArmIKData);
            Assert.IsTrue(_rightArmIKData != null && _rightArmIKData.TargetToCustomHand.Length > 0);

            SetupJob(
                _leftArmIKData.TargetToCustomHand,
                ref _leftArmIKData.CustomHandPoses,
                ref _leftArmIKData.CustomHandTransforms,
                ref _leftArmIKData.TargetIndices,
                ref _leftArmIKData.GetCustomHandPosesJob);
            SetupJob(
                _rightArmIKData.TargetToCustomHand,
                ref _rightArmIKData.CustomHandPoses,
                ref _rightArmIKData.CustomHandTransforms,
                ref _rightArmIKData.TargetIndices,
                ref _rightArmIKData.GetCustomHandPosesJob);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            _leftArmIKData?.CleanUp();
            _rightArmIKData?.CleanUp();
        }

        /// <inheritdoc />
        public override void UpdatePose(ref NativeArray<NativeTransform> pose)
        {
        }

        /// <inheritdoc />
        public override void LateUpdatePose(
            ref NativeArray<NativeTransform> currentPose,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            var targetWorldPose = SkeletonUtilities.ComputeWorldPoses(
                _retargeter.SkeletonRetargeter,
                ref targetPoseLocal,
                _retargeter.transform.position,
                _retargeter.transform.rotation);

            var oldRotation = Quaternion.identity;
            if (_useRetargetedHandRotation)
            {
                // If using the retargeted hand rotation, source the retargeted data first.
                // And then affected the custom hand joint root.
                _leftHandJoints[0].rotation =
                    targetWorldPose[_leftArmIKData.ArmJointIndicesReversed[0]].Orientation;
                _rightHandJoints[0].rotation =
                    targetWorldPose[_rightArmIKData.ArmJointIndicesReversed[0]].Orientation;
            }
            else
            {
                SetJointRotationToTarget(targetPoseLocal, targetWorldPose,
                    _leftHandJoints[0].rotation, _leftArmIKData.ArmJointIndicesReversed[0]);
                SetJointRotationToTarget(targetPoseLocal, targetWorldPose,
                    _rightHandJoints[0].rotation, _rightArmIKData.ArmJointIndicesReversed[0]);
            }
            targetWorldPose.Dispose();

            if (_runSerial)
            {
                FixLocalFingerPosesSerial(targetPoseLocal, _weight);
            }
            else
            {
                FixLocalFingerPosesJobs(targetPoseLocal, _weight);
            }

            if (_algorithm == IKAlgorithm.CCDIK)
            {
                RunCCDOnSkeletalData(_leftArmIKData, _leftHandJoints, ref targetPoseLocal, _weight);
                RunCCDOnSkeletalData(_rightArmIKData, _rightHandJoints, ref targetPoseLocal, _weight);
            }
        }

        private void SetJointRotationToTarget(
            NativeArray<NativeTransform> targetPoseLocal,
            NativeArray<NativeTransform> targetPoseWorld,
            Quaternion targetRotationWorld, int armJointIndex)
        {
            var parentIndexIkJoint = _parentJointIndicesTarget[armJointIndex];
            var inLocalSpace = Quaternion.Inverse(targetPoseWorld[parentIndexIkJoint].Orientation) * targetRotationWorld;
            targetPoseLocal[armJointIndex] =
                new NativeTransform(inLocalSpace, targetPoseLocal[armJointIndex].Position,
                targetPoseLocal[armJointIndex].Scale);
        }

        private List<Transform> FindTransformsDownChain(Transform startingTransform, Transform endTransform)
        {
            var transforms = new List<Transform>();
            var currentTransform = startingTransform;
            while (currentTransform != null && currentTransform != endTransform)
            {
                transforms.Add(currentTransform);
                currentTransform = currentTransform.GetChild(0);
            }

            transforms.Add(endTransform);
            return transforms;
        }

        private void InitializeArmJointIndices(ulong pluginHandle, List<Transform> armBones, ArmIKData armIkData)
        {
            var validArmJoints = new List<int>();
            foreach (var bone in armBones)
            {
                var armBone = bone.name;
                GetJointIndex(pluginHandle, SkeletonType.TargetSkeleton, armBone, out int jointIndex);
                if (jointIndex < 0)
                {
                    Debug.LogWarning($"Could not map joint {armBone} so won't use as IK joint.");
                    continue;
                }

                validArmJoints.Add(jointIndex);
            }

            armIkData.ArmJointIndicesReversed = validArmJoints.ToArray();
        }

        private void FillJointMappings(Transform[] characterBonesHand, Transform[] customHandBones,
            ref List<Tuple<Transform, Transform>> mappedCharacterDataBones)
        {
            // Skip hand index by starting at index 1
            for (var i = 1; i < customHandBones.Length; i++)
            {
                var targetJoint = customHandBones[i];
                Transform animJoint = null;

                foreach (var bone in characterBonesHand)
                {
                    if (bone.name == targetJoint.name)
                    {
                        animJoint = bone;
                        break;
                    }
                }

                if (animJoint == null)
                {
                    Debug.LogWarning($"Could not find animator joint corresponding to {targetJoint}.");
                    continue;
                }

                var pair = new Tuple<Transform, Transform>(animJoint, targetJoint);
                mappedCharacterDataBones.Add(pair);
            }
        }

        private void InitializeTargetToCustomHandAssociation(
            ulong pluginHandle, List<Tuple<Transform, Transform>> targetToCustomHandTuples, ArmIKData armIkData)
        {
            var handAssoc = new List<TargetToCustomHandAssociation>();
            foreach (var customHandTuple in targetToCustomHandTuples)
            {
                var boneName = customHandTuple.Item1.name;
                GetJointIndex(pluginHandle, SkeletonType.TargetSkeleton, boneName, out var jointIndex);
                if (jointIndex == -1)
                {
                    Debug.LogWarning($"Could not find joint index for joint {boneName}. Skipping.");
                    continue;
                }

                handAssoc.Add(new TargetToCustomHandAssociation()
                {
                    TargetIndex = jointIndex,
                    CustomHandTransform = customHandTuple.Item2
                });
            }

            armIkData.TargetToCustomHand = handAssoc.ToArray();
        }

        private void SetupJob(
            TargetToCustomHandAssociation[] targetToCustomHand,
            ref NativeArray<Pose> customHandPoses,
            ref TransformAccessArray customHandTransforms,
            ref NativeArray<int> targetIndicesArray,
            ref GetCustomHandLocalPosesJob getPosesJob)
        {
            // extract and flatten mapping between custom hand and target character
            targetIndicesArray = new NativeArray<int>(targetToCustomHand.Length, Allocator.Persistent);
            var customHandBones = new List<Transform>();
            for (int i = 0; i < targetToCustomHand.Length; i++)
            {
                targetIndicesArray[i] = targetToCustomHand[i].TargetIndex;
                customHandBones.Add(targetToCustomHand[i].CustomHandTransform);
            }

            // set up the job to get the poses from custom hand.
            customHandPoses = new NativeArray<Pose>(targetToCustomHand.Length, Allocator.Persistent);
            getPosesJob = new GetCustomHandLocalPosesJob
            {
                Poses = customHandPoses
            };
            customHandTransforms = new TransformAccessArray(customHandBones.ToArray());
        }

        private void RunCCDOnSkeletalData(
            ArmIKData armIKData,
            Transform[] customHandJoints,
            ref NativeArray<NativeTransform> targetSkeletonPoseLocal,
            float weight)
        {
            var ikChainIndicesRev = armIKData.ArmJointIndicesReversed;
            int chainBeginningIndex = ikChainIndicesRev.Length - 1;
            var chainRootIndex = _parentJointIndicesTarget[ikChainIndicesRev[chainBeginningIndex]];
            var chainEndEffectorIndex = ikChainIndicesRev[0];
            var endEffectorParentIndex = _parentJointIndicesTarget[chainEndEffectorIndex];

            if (chainRootIndex == -1 || endEffectorParentIndex == -1)
            {
                Debug.LogError("Cannot run CCD; invalid parent indices.");
                return;
            }

            // Get the current world pose as provided by other processors.
            var targetSkeletonPoseWorld = SkeletonUtilities.ComputeWorldPoses(
                _retargeter.SkeletonRetargeter,
                ref targetSkeletonPoseLocal,
                _retargeter.transform.position,
                _retargeter.transform.rotation);
            var rootPoseWorld = new Pose(targetSkeletonPoseWorld[chainRootIndex].Position,
                targetSkeletonPoseWorld[chainRootIndex].Orientation);
            var customHandTargetPosition = customHandJoints[0].position;
            var targetPositionLerpedWorld =
                Vector3.Lerp(targetSkeletonPoseWorld[chainEndEffectorIndex].Position, customHandTargetPosition, weight);

            // Build IK transforms from the indices given.
            var ikChainTransformsRev = GetIKChainEndFirst(ikChainIndicesRev, ref targetSkeletonPoseLocal);

            // Run CCD IK solver.
            IKUtilities.SolveCCDIKLocalNativeArray(
                rootPoseWorld,
                ikChainTransformsRev,
                targetPositionLerpedWorld,
                _ikTolerance,
                _ikIterations);

            // Set the final poses, but store the end effector rotation before doing that.
            // Note that this updates only rotation.
            var originalEndEffectorRotationWorld = targetSkeletonPoseWorld[chainEndEffectorIndex].Orientation;
            for (var i = 0; i < ikChainIndicesRev.Length; i++)
            {
                // Each element in the transform array is 1-to-1 with the index array used to build it.
                var currentIndexInTarget = ikChainIndicesRev[i];
                var poseToLerpTo = ikChainTransformsRev[i];
                var poseToSet = targetSkeletonPoseLocal[currentIndexInTarget];
                poseToSet.Orientation =
                    Quaternion.Slerp(poseToSet.Orientation, poseToLerpTo.Orientation, weight);
                targetSkeletonPoseLocal[currentIndexInTarget] = poseToSet;
            }

            // Covert the CCD-modified local positions back to global we need to use these values to compute
            // the local transform of the end effector to push it toward the target.
            targetSkeletonPoseWorld.Dispose();
            targetSkeletonPoseWorld = SkeletonUtilities.ComputeWorldPoses(
                _retargeter.SkeletonRetargeter,
                ref targetSkeletonPoseLocal,
                _retargeter.transform.position,
                _retargeter.transform.rotation);
            // Calculate local space end effector position.
            var limitedEndEffectWorldSpace = targetSkeletonPoseWorld[chainEndEffectorIndex].Position;
            var limitedEndEffectLocalSpace = GetTargetRelativeToEffectorParent(endEffectorParentIndex, targetSkeletonPoseWorld,
                limitedEndEffectWorldSpace,
                _retargeter.SkeletonRetargeter.RootScale);

            // Push end effector to target. We need to know what the local position of the target
            // is based on the last recomputed world positions of the IK chain after CCD has
            // modified them.
            var targetLocalPosition =
                _limitStretch
                    ? limitedEndEffectLocalSpace
                    : GetTargetRelativeToEffectorParent(endEffectorParentIndex, targetSkeletonPoseWorld,
                        // Use offset here in case the limited end effector needs to be moved back.
                        targetPositionLerpedWorld,
                        _retargeter.SkeletonRetargeter.RootScale);

            var endEffectorRotationInLocalSpace =
                Quaternion.Inverse(targetSkeletonPoseWorld[endEffectorParentIndex].Orientation) *
                (originalEndEffectorRotationWorld);
            targetSkeletonPoseLocal[chainEndEffectorIndex] =
                new NativeTransform(
                    endEffectorRotationInLocalSpace,
                    targetLocalPosition);
            targetSkeletonPoseWorld.Dispose();
        }

        private NativeArray<NativeTransform> GetIKChainEndFirst(
            int[] ikChainReversed,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            var ikChainArrayEndFirst = new NativeArray<NativeTransform>(
                ikChainReversed.Length,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            int lastChainIndex = ikChainReversed.Length - 1;
            for (int i = 0; i <= lastChainIndex; i++)
            {
                ikChainArrayEndFirst[i] = targetPoseLocal[ikChainReversed[i]];
            }

            return ikChainArrayEndFirst;
        }

        private void FixLocalFingerPosesSerial(NativeArray<NativeTransform> targetPoseLocal, float weight)
        {
            if (!_matchFingers)
            {
                return;
            }

            foreach (var pair in _leftArmIKData.TargetToCustomHand)
            {
                SetLocalPositionAndRotation(targetPoseLocal, pair.TargetIndex, pair.CustomHandTransform, weight);
            }

            foreach (var pair in _rightArmIKData.TargetToCustomHand)
            {
                SetLocalPositionAndRotation(targetPoseLocal, pair.TargetIndex, pair.CustomHandTransform, weight);
            }
        }

        private Vector3 GetTargetRelativeToEffectorParent(int endEffectorParentIndex,
            NativeArray<NativeTransform> targetWorldPose, Vector3 targetPositionWorldLerp, Vector3 rootScale)
        {
            var parentEndEffectorWorld = targetWorldPose[endEffectorParentIndex];
            var targetRelativeToParent = Quaternion.Inverse(parentEndEffectorWorld.Orientation) *
                                         (targetPositionWorldLerp - parentEndEffectorWorld.Position);
            return Vector3.Scale(targetRelativeToParent,
                new Vector3(1f / rootScale.x, 1f / rootScale.y, 1f / rootScale.z));
        }

        private void SetLocalPositionAndRotation(
            NativeArray<NativeTransform> targetPoseLocal,
            int targetSkeletonIndex,
            Transform customHandBone,
            float weight)
        {
            var targetOriginalPos = targetPoseLocal[targetSkeletonIndex];
            var targetLocalPos = Vector3.Lerp(targetOriginalPos.Position, customHandBone.localPosition, weight);
            var targetLocalRot = Quaternion.Slerp(targetOriginalPos.Orientation, customHandBone.localRotation, weight);
            targetPoseLocal[targetSkeletonIndex] =
                new NativeTransform(targetLocalRot, targetLocalPos, targetOriginalPos.Scale);
        }

        private void FixLocalFingerPosesJobs(NativeArray<NativeTransform> currentPose, float weight)
        {
            if (!_matchFingers)
            {
                return;
            }

            var leftReadJobHandle = _leftArmIKData.GetCustomHandPosesJob.ScheduleReadOnly(
                _leftArmIKData.CustomHandTransforms, 32);
            var leftWriteJob = new WriteCharacterHandLocalPosesJob
            {
                CustomHandPoses = _leftArmIKData.CustomHandPoses,
                TargetIndices = _leftArmIKData.TargetIndices,
                OutputPoses = currentPose,
                Weight = weight
            };
            var leftWriteJobHandle = leftWriteJob.Schedule(leftReadJobHandle);

            var rightReadJobHandle = _rightArmIKData.GetCustomHandPosesJob.ScheduleReadOnly(
                _rightArmIKData.CustomHandTransforms,
                32,
                leftWriteJobHandle);
            var rightWriteJob = new WriteCharacterHandLocalPosesJob
            {
                CustomHandPoses = _rightArmIKData.CustomHandPoses,
                TargetIndices = _rightArmIKData.TargetIndices,
                OutputPoses = currentPose,
                Weight = weight
            };
            var rightWriteJobHandle = rightWriteJob.Schedule(rightReadJobHandle);

            rightWriteJobHandle.Complete();
        }
    }
}
