// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.AnimationRigging.Utils;
using System;
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
        [Serializable]
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
                return targetPositionOffset;
            }
        }

        /// <summary>
        /// Triggered if proxy transforms were recreated.
        /// </summary>
        public int ProxyChangeCount => _proxyTransformLogic.ProxyChangeCount;

        /// <summary>
        /// Allows one to specify which positions to correct during late update.
        /// </summary>
        public AvatarMask CustomPositionsToCorrectLateUpdateMask
        {
            get; set;
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
            get => _applyAnimationConstraintsToCorrectedPositions;
            set => _applyAnimationConstraintsToCorrectedPositions = value;
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
        protected bool _enableTrackingByProxy = true;
        /// <inheritdoc cref="_enableTrackingByProxy"/>
        public bool EnableTrackingByProxy
        {
            get => _enableTrackingByProxy;
            set => _enableTrackingByProxy = value;
        }

        /// <summary>
        /// Triggers methods that can alter bone translations and rotations, before rendering and physics
        /// </summary>
        [SerializeField, Optional]
        protected OVRSkeletonProcessor _skeletonPostProcessing;
        /// <inheritdoc cref="_skeletonPostProcessing"/>
        public OVRSkeletonProcessor SkeletonPostProcessingEv
        {
            get => _skeletonPostProcessing;
            set => _skeletonPostProcessing = value;
        }

        /// <summary>
        /// Related retargeting constraint.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.RetargetingAnimationConstraint)]
        protected RetargetingAnimationConstraint _retargetingAnimationConstraint;
        /// <inheritdoc cref="_retargetingAnimationConstraint"/>
        public RetargetingAnimationConstraint RetargetingConstraint
        {
            get => _retargetingAnimationConstraint;
            set => _retargetingAnimationConstraint = value;
        }

        /// <summary>
        /// List of retargeting processors, which run in late update after retargeting and animation rigging.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.RetargetingProcessors)]
        protected List<RetargetingProcessor> _retargetingProcessors = new();
        /// <inheritdoc cref="_retargetingProcessors"/>
        public List<RetargetingProcessor> RetargetingProcessors
        {
            get => _retargetingProcessors;
            set => _retargetingProcessors = value;
        }

        /// <summary>
        /// Retargeting animation rig to be updated based on body tracking.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.RetargetingAnimationRig)]
        protected RetargetingAnimationRig _retargetingAnimationRig;
        /// <inheritdoc cref="_retargetingAnimationRig"/>
        public RetargetingAnimationRig RetargetingAnimationRigInst
        {
            get => _retargetingAnimationRig;
            set => _retargetingAnimationRig = value;
        }

        /// <summary>
        /// External bone targets to be updated based on body tracking.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.ExternalBoneTargets)]
        protected ExternalBoneTargets _externalBoneTargets;
        /// <inheritdoc cref="_externalBoneTargets"/>
        public ExternalBoneTargets ExternalBoneTargetsInst
        {
            get => _externalBoneTargets;
            set => _externalBoneTargets = value;
        }

        /// <summary>
        /// Retargeted bone mappings to be updated based on valid bones in the humanoid.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.RetargetedBoneMappings)]
        protected RetargetedBoneMappings _retargetedBoneMappings;
        /// <inheritdoc cref="_retargetedBoneMappings"/>
        public RetargetedBoneMappings RetargetedBoneMappingsInst
        {
            get => _retargetedBoneMappings;
            set => _retargetedBoneMappings = value;
        }

        /// <summary>
        /// Exposes a button used to update bone pair mappings based on the humanoid.
        /// </summary>
        [SerializeField, InspectorButton("UpdateBonePairMappings")]
        private bool _updateBoneMappingsData;

        private Pose[] _defaultPoses;
        private IJointConstraint[] _jointConstraints;
        private ProxyTransformLogic _proxyTransformLogic = new ProxyTransformLogic();
        private bool _isFocusedWhileInBuild = true;

        // Cached array versions of dictionaries used in OVRUnityHumanoidSkeletonRetargeter
        private HumanBodyBones[] _customBoneIdToHumanBodyBoneArray;
        private OVRSkeletonMetadata.BoneData[] _targetSkeletonDataBodyToBoneDataArray;
        private OVRHumanBodyBonesMappings.BodySection[] _boneToBodySectionArray;
        private Dictionary<HumanBodyBones, OVRUnityHumanoidSkeletonRetargeter.OVRSkeletonMetadata.BoneData>
            _lastReferencedtargetBoneData = null;

        private int _lastSkelChangeCountRt = -1;
        private Vector3 _lastTrackedScaleRt;
        private Dictionary<Transform, Quaternion> _storedRotationsAboveRoot =
            new Dictionary<Transform, Quaternion>();

        /// <summary>
        /// Check for required components.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _proxyTransformLogic.UseJobs = true;

            if (_retargetedBoneMappings.ConvertBonePairsToDictionaries())
            {
                BodyBoneMappingsInterface = _retargetedBoneMappings;
            }

            Assert.IsNotNull(_retargetingAnimationConstraint,
                "Please assign the retargeting constraint to RetargetingLayer.");

            for (int i = 0; i < _retargetingProcessors.Count; i++)
            {
                var retargetingProcessor = _retargetingProcessors[i];
                Assert.IsNotNull(retargetingProcessor,
                    "Please assign the retargeting processor to RetargetingLayer.");
                var instance = Instantiate(_retargetingProcessors[i]);
                instance.name = $"{name} {_retargetingProcessors[i].name}";
                _retargetingProcessors[i] = instance;
            }

            var animatorComp = GetComponent<Animator>();
            Assert.IsTrue(animatorComp.avatar != null,
                "Animator requires avatar component.");
            Assert.IsTrue(animatorComp.avatar.isHuman,
                "Animator avatar must be humanoid.");
            if (!animatorComp.avatar.humanDescription.hasTranslationDoF)
            {
                Debug.LogError("Translation DoF is not enabled on your avatar, this " +
                    "will prevent proper positional retargeting of its joints. Enable this " +
                    $"at {animatorComp.avatar} -> Configure Avatar -> Muscles " +
                    $"& Settings -> Translation DoF", animatorComp.avatar);
            }
        }

        private void OnDestroy()
        {
            _proxyTransformLogic.CleanUp();
        }

        /// <summary>
        /// Initialize base class and also any variables required by this class,
        /// such as the positions and rotations of the character joints at rest pose.
        /// </summary>
        protected override void Start()
        {
            try
            {
                _lastTrackedScaleRt = transform.lossyScale;
                // cache any transformation information above hips to make sure upright rest pose is captured.
                CaptureTransformInformationHipsUpwards(GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips));
                if (_retargetingAnimationRig.RigBuilderComp.enabled)
                {
                    Debug.LogError("Please disable the rig builder by default or else the animation system " +
                        " will prevent a correct capture of the rest pose.");
                }
                base.Start();

                ConstructDefaultPoseInformation();
                ConstructBoneAdjustmentInformation();
                CacheJointConstraints();
                CacheBoneMapping();
                ValidateHumanoid();

                _retargetingAnimationRig.ValidateRig(this);
                foreach (var retargetingProcessor in _retargetingProcessors)
                {
                    retargetingProcessor.SetupRetargetingProcessor(this);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }

        private void CaptureTransformInformationHipsUpwards(Transform currentTransform)
        {
            // Avoid going to topmost transform (the character). We want to ignore any rotations
            // a user wants to apply to the character itself.
            if (currentTransform == this.transform)
            {
                return;
            }

            if (!_storedRotationsAboveRoot.ContainsKey(currentTransform))
            {
                _storedRotationsAboveRoot[currentTransform] = currentTransform.localRotation;
            }
            CaptureTransformInformationHipsUpwards(currentTransform.parent);
        }

        /// <summary>
        /// Update bone pair mappings for the retargeted humanoid.
        /// </summary>
        public void UpdateBonePairMappings()
        {
            if (_retargetedBoneMappings == null)
            {
                _retargetedBoneMappings = new RetargetedBoneMappings();
            }
            _retargetedBoneMappings.UpdateBonePairMappings(this);
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

        private void CacheBoneMapping()
        {
            _targetSkeletonDataBodyToBoneDataArray = new OVRSkeletonMetadata.BoneData[(int)HumanBodyBones.LastBone];
            for (var boneIndex = HumanBodyBones.Hips; boneIndex < HumanBodyBones.LastBone; boneIndex++)
            {
                _targetSkeletonDataBodyToBoneDataArray[(int)boneIndex] = null;
            }

            _customBoneIdToHumanBodyBoneArray = new HumanBodyBones[(int)BoneId.Max];
            for (int i = 0; i < _customBoneIdToHumanBodyBoneArray.Length; i++)
            {
                _customBoneIdToHumanBodyBoneArray[i] = BodyBoneMappingsInterface.GetFullBodyBoneIdToHumanBodyBone.
                    GetValueOrDefault((BoneId)i, HumanBodyBones.LastBone);
            }

            _boneToBodySectionArray =
                new OVRHumanBodyBonesMappings.BodySection[OVRHumanBodyBonesMappings.BoneToBodySection.Count];
            for (int i = 0; i < _boneToBodySectionArray.Length; i++)
            {
                _boneToBodySectionArray[i] =
                    OVRHumanBodyBonesMappings.BoneToBodySection.GetValueOrDefault((HumanBodyBones)i,
                        OVRHumanBodyBonesMappings.BodySection.Head);
            }
        }

        private void ValidateHumanoid()
        {
            bool validHumanoid = true;
            foreach (var bodyBone in CustomBoneIdToHumanBodyBone.Values)
            {
                if (!AnimatorTargetSkeleton.GetBoneTransform(bodyBone))
                {
                    Debug.LogWarning($"Did not find {bodyBone} in {AnimatorTargetSkeleton}, this might affect" +
                        $" the retargeted result.");
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
            _retargetingAnimationRig.OnApplicationFocus(this, hasFocus);
            _isFocusedWhileInBuild = hasFocus;
        }

        /// <inheritdoc />
        protected override void Update()
        {
            UpdateSkeleton();
            _skeletonPostProcessing?.Invoke(this);
            var offsetRecomputedThisFrame = OffsetRegenerationNeededThisFrame();
            // The character needs to forced into a rest pose. Disable any transforms above the hips.
            if (offsetRecomputedThisFrame)
            {
                ResetBoneTransformationsHipsUpwards(AnimatorTargetSkeleton.GetBoneTransform(HumanBodyBones.Hips));
            }
            RecomputeSkeletalOffsetsIfNecessary();
            if (offsetRecomputedThisFrame)
            {
                // update state tracking variables.
                _lastTrackedScaleRt = transform.lossyScale;
                _lastSkelChangeCountRt = SkeletonChangedCount;
            }

            UpdateBoneDataToArray();

            _externalBoneTargets.ProcessSkeleton(this);

            if (_enableTrackingByProxy)
            {
                _proxyTransformLogic.UpdateState(Bones, transform);
            }
            _retargetingAnimationRig.UpdateRig(this);
        }

        private bool OffsetRegenerationNeededThisFrame()
        {
            bool scaleChanged = (transform.lossyScale - _lastTrackedScaleRt).sqrMagnitude
                > Mathf.Epsilon;
            bool skeletalCountChange = _lastSkelChangeCountRt != SkeletonChangedCount;

            return skeletalCountChange || scaleChanged;
        }

        /// <summary>
        /// If we want to force a character into T-pose, reset the transforms
        /// above the hips, but NOT on the character itself. Sometimes animation rigging
        /// changes transform values above the hips during runtime, which prevents us
        /// from forcing a character into a proper rest pose.
        /// </summary>
        /// <param name="currentTransform"></param>
        private void ResetBoneTransformationsHipsUpwards(Transform currentTransform)
        {
            // Avoid going to topmost transform (the character). We want to not affect any rotations
            // a user wants to apply to the character itself.
            if (currentTransform == this.transform)
            {
                return;
            }

            Quaternion defaultQuaternion = _storedRotationsAboveRoot[currentTransform];
            currentTransform.localRotation = defaultQuaternion;

            ResetBoneTransformationsHipsUpwards(currentTransform.parent);
        }

        /// <summary>
        /// Empty fixed update to avoid updating OVRSkeleton during fixed update.
        /// </summary>
        private void FixedUpdate()
        {
        }

        /// <summary>
        /// Allows fixing joints via the retargeting processors. The avatar does not allow
        /// precise finger positions even with translate DoF checked.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!_isFocusedWhileInBuild)
            {
                return;
            }

            foreach (var retargetingProcessor in _retargetingProcessors)
            {
                retargetingProcessor.PrepareRetargetingProcessor(this, Bones);
            }

            foreach (var retargetingProcessor in _retargetingProcessors)
            {
                retargetingProcessor.ProcessRetargetingLayer(this, Bones);
            }

            // Apply constraints on character after fixing positions.
            RunConstraints();
        }

        protected virtual void OnDrawGizmos()
        {
            foreach (var retargetingProcessor in _retargetingProcessors)
            {
                if (retargetingProcessor != null)
                {
                    retargetingProcessor.DrawGizmos();
                }
            }
        }

        protected virtual void UpdateBoneDataToArray()
        {
            // Update only if the source updated too.
            if (_lastReferencedtargetBoneData == TargetSkeletonData.BodyToBoneData)
            {
                return;
            }
            for (var boneIndex = HumanBodyBones.Hips; boneIndex < HumanBodyBones.LastBone; boneIndex++)
            {
                _targetSkeletonDataBodyToBoneDataArray[(int)boneIndex] =
                    TargetSkeletonData.BodyToBoneData.GetValueOrDefault(boneIndex, null);
            }
            _lastReferencedtargetBoneData = TargetSkeletonData.BodyToBoneData;
        }

        protected virtual bool ShouldUpdatePositionOfBone(HumanBodyBones humanBodyBone)
        {
            var bodySectionOfJoint = _boneToBodySectionArray[(int)humanBodyBone];
            return IsBodySectionInArray(bodySectionOfJoint,
                _skeletonType == SkeletonType.FullBody ? FullBodySectionToPosition : BodySectionToPosition);
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
            if (TargetSkeletonData == null)
            {
                return;
            }
            for (int i = 0; i < numBones; i++)
            {
                var currentBone = skeletalBones[i];
                HumanBodyBones targetHumanBodyBone;
                OVRSkeletonMetadata.BoneData targetBoneData;

                (targetBoneData, targetHumanBodyBone) =
                    GetTargetBoneDataFromOVRBone(skeletalBones[i]);
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
            int arrayId = 0;
            for (int i = 0; i < numBones; i++)
            {
                HumanBodyBones targetHumanBodyBone;
                OVRSkeletonMetadata.BoneData targetBoneData;

                var currBone = skeletalBones[i];
                (targetBoneData, targetHumanBodyBone) =
                    GetTargetBoneDataFromOVRBone(currBone);
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
                        BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)targetHumanBodyBone]);
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
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value;
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
            rotationOffsets[arrayId] = targetBoneData.CorrectionQuaternion.Value;
            shouldUpdatePositions[arrayId] =
                !adjustment.DisablePositionTransform &&
                bodySectionInPositionArray &&
                !jointFailsMask;
            shouldUpdateRotations[arrayId] =
                !adjustment.DisableRotationTransform &&
                !jointFailsMask;
            rotationAdjustments[arrayId] = adjustment.RotationChange * adjustment.PrecomputedRotationTweaks;
        }

        private (OVRSkeletonMetadata.BoneData, HumanBodyBones) GetTargetBoneDataFromOVRBone(OVRBone ovrBone)
        {
            var humanBodyBone = _customBoneIdToHumanBodyBoneArray[(int)ovrBone.Id];
            if (humanBodyBone == HumanBodyBones.LastBone)
            {
                return (null, HumanBodyBones.LastBone);
            }
            var targetData = _targetSkeletonDataBodyToBoneDataArray[(int)humanBodyBone];
            if (targetData == null)
            {
                return (null, HumanBodyBones.LastBone);
            }

            return (targetData, humanBodyBone);
        }

        #region Public API

        /// <inheritdoc/>
        public void AddProcessor(IOVRSkeletonProcessor processor)
        {
            _skeletonPostProcessing += processor.ProcessSkeleton;
        }

        /// <inheritdoc/>
        public void RemoveProcessor(IOVRSkeletonProcessor processor)
        {
            _skeletonPostProcessing -= processor.ProcessSkeleton;
        }

        /// <summary>
        /// Add the specified retargeting processor.
        /// </summary>
        /// <param name="processor">The processor to be added.</param>
        public void AddRetargetingProcessor(RetargetingProcessor processor)
        {
            _retargetingProcessors.Add(processor);
        }

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

        /// <summary>
        /// Returns the custom bone id to human body bone pairing, if it exists.
        /// </summary>
        /// <param name="boneId">The bone id to check for.</param>
        /// <returns>The human body bone for a custom bone id. Returns null if it doesn't exist.</returns>
        public HumanBodyBones? GetCustomBoneIdToHumanBodyBone(BoneId boneId)
        {
            var humanBodyBone = _customBoneIdToHumanBodyBoneArray[(int)boneId];
            if (humanBodyBone == HumanBodyBones.LastBone)
            {
                return null;
            }
            return humanBodyBone;
        }

        /// <summary>
        /// Returns the human body bone to body section pairing.
        /// </summary>
        /// <param name="humanBodyBone">The human body bone to check for.</param>
        /// <returns>The body section for a human body bone.</returns>
        public OVRHumanBodyBonesMappings.BodySection GetHumanBodyBoneToBodySection(HumanBodyBones humanBodyBone)
        {
            return _boneToBodySectionArray[(int)humanBodyBone];
        }

        /// <summary>
        /// Returns the correction quaternion for a human body bone, if it exists.
        /// </summary>
        /// <param name="humanBodyBone">The human body bone to check for.</param>
        /// <returns>The correction quaternion for a human body bone. Returns null if it doesn't exist.</returns>
        public Quaternion? GetCorrectionQuaternion(HumanBodyBones humanBodyBone)
        {
            var targetData = _targetSkeletonDataBodyToBoneDataArray[(int)humanBodyBone];
            if (targetData == null)
            {
                return null;
            }
            if (!targetData.CorrectionQuaternion.HasValue)
            {
                return null;
            }
            return targetData.CorrectionQuaternion.Value;
        }

        /// <summary>
        /// Returns the original joint for a human body bone.
        /// </summary>
        /// <param name="humanBodyBone">The human body bone to check for.</param>
        /// <returns>The original joint for a human body bone.</returns>
        public Transform GetOriginalJoint(HumanBodyBones humanBodyBone)
        {
            var targetData = _targetSkeletonDataBodyToBoneDataArray[(int)humanBodyBone];
            if (targetData == null)
            {
                return null;
            }
            return targetData.OriginalJoint;
        }

        /// <summary>
        /// Returns the animator used for retargeting.
        /// </summary>
        /// <returns>The animator used for retargeting.</returns>
        public Animator GetAnimatorTargetSkeleton()
        {
            return AnimatorTargetSkeleton;
        }

        /// <summary>
        /// Returns the joint adjustment for a human body bone.
        /// </summary>
        /// <param name="humanBodyBone">The human body bone to check for.</param>
        /// <returns>The joint adjustment for a human body bone.</returns>
        public JointAdjustment GetFindAdjustment(HumanBodyBones humanBodyBone)
        {
            return FindAdjustment(humanBodyBone);
        }

        /// <summary>
        /// Returns true if the position of a human body bone should be updated.
        /// </summary>
        /// <param name="humanBodyBone">The human body bone to check for.</param>
        /// <returns>True if the position of a human body should be updated.</returns>
        public bool GetShouldUpdatePositionOfBone(HumanBodyBones humanBodyBone)
        {
            return ShouldUpdatePositionOfBone(humanBodyBone);
        }

        #endregion
    }
}
