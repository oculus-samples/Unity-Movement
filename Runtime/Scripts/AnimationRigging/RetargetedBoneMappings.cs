// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Body.Input;
using Oculus.Movement.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings;

namespace Oculus.Movement.AnimationRigging
{
    [Serializable]
    public class RetargetedBoneMappings : OVRHumanBodyBonesMappingsInterface
    {
        /// <inheritdoc />
        public Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>> GetBoneToJointPair =>
            _boneToJointPair;
        
        /// <inheritdoc />
        public Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>> GetBoneIdToJointPair => 
            _fullBodyBoneIdToBonePairs;
        
        /// <inheritdoc />
        public Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>> GetFullBodyBoneIdToJointPair => 
            _fullBodyBoneIdToBonePairs;
        
        /// <inheritdoc />
        public Dictionary<OVRSkeleton.BoneId, HumanBodyBones> GetBoneIdToHumanBodyBone =>
            _fullBodyBoneIdToHumanBodyBone;
        
        /// <inheritdoc />
        public Dictionary<OVRSkeleton.BoneId, HumanBodyBones> GetFullBodyBoneIdToHumanBodyBone => 
            _fullBodyBoneIdToHumanBodyBone;
        
        // Static members from interface.
        /// <inheritdoc />
        public Dictionary<HumanBodyBones, BodySection> GetBoneToBodySection => 
            BoneToBodySection;

        /// <summary>
        /// HumanBodyBone pairs for this humanoid.
        /// </summary>
        [SerializeField, EnumNamedArray(typeof(HumanBodyBones))] 
        [Tooltip(RetargetedBoneMappingsTooltips.HumanBodyBonePairs)]
        private HumanBodyBones[] _humanBodyBonePairs;

        /// <summary>
        /// HumanBodyBone to BodyJointId mapping for this humanoid.
        /// </summary>
        [SerializeField, EnumNamedArray(typeof(HumanBodyBones))]
        [Tooltip(RetargetedBoneMappingsTooltips.HumanBodyBoneToBoneId)]
        private FullBodyTrackingBoneId[] _humanBodyBoneToBoneId;

        /// <summary>
        /// BodyJointId to BodyJointId mapping for this humanoid.
        /// </summary>
        [SerializeField, EnumNamedArray(typeof(FullBodyTrackingBoneId))]
        [Tooltip(RetargetedBoneMappingsTooltips.HumanBodyBoneToBoneId)]
        private FullBodyTrackingBoneId[] _boneIdToBonePairs;

        private Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>> _boneToJointPair;
        private Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>> _fullBodyBoneIdToBonePairs;
        private Dictionary<OVRSkeleton.BoneId, HumanBodyBones> _fullBodyBoneIdToHumanBodyBone;
        
        /// <summary>
        /// Update bone pair mappings for the retargeted humanoid.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        public void UpdateBonePairMappings(RetargetingLayer retargetingLayer)
        {
            var animator = retargetingLayer.GetComponent<Animator>();
            var humanBodyBonePairs = new List<HumanBodyBones>();
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var childBone = DeformationUtilities.FindChildHumanBodyBones(animator, bone);
                // Specific child bone mapping for the hands.
                if (bone == HumanBodyBones.LeftHand)
                {
                    childBone = HumanBodyBones.LeftMiddleProximal;
                }
                else if (bone == HumanBodyBones.RightHand)
                {
                    childBone = HumanBodyBones.RightMiddleProximal;
                }
                humanBodyBonePairs.Add(childBone);
            }
            _humanBodyBonePairs = humanBodyBonePairs.ToArray();
            UpdateBoneIdToBonePairs();
            UpdateHumanBodyBoneToBoneId(animator);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(retargetingLayer);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(retargetingLayer);
#endif
        }

        /// <summary>
        /// Converts the existing human body bone mappings to dictionaries required for the interface.
        /// </summary>
        /// <returns>Returns true if valid bone pairs were converted to dictionaries successfully.</returns>
        public bool ConvertBonePairsToDictionaries()
        {
            if (_humanBodyBonePairs == null || _humanBodyBonePairs.Length == 0 ||
                _humanBodyBoneToBoneId == null || _humanBodyBoneToBoneId.Length == 0 ||
                _boneIdToBonePairs == null || _boneIdToBonePairs.Length == 0)
            {
                Debug.LogWarning("Required human body bone mappings is missing! Using default mappings.");
                return false;
            }

            var excludedBones = new[] { HumanBodyBones.LeftEye, HumanBodyBones.RightEye, HumanBodyBones.Jaw };
            _boneToJointPair = new Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>>();
            _fullBodyBoneIdToHumanBodyBone = new Dictionary<OVRSkeleton.BoneId, HumanBodyBones>();
            for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
            {
                if (Array.IndexOf(excludedBones, bone) != -1)
                {
                    continue;
                }

                var boneIndex = (int)bone;
                var childBone = _humanBodyBonePairs[boneIndex];
                var targetBoneId = (OVRSkeleton.BoneId)_humanBodyBoneToBoneId[boneIndex];
                
                _boneToJointPair.Add(bone, new Tuple<HumanBodyBones, HumanBodyBones>(bone, childBone));
                _fullBodyBoneIdToHumanBodyBone.Add(targetBoneId, bone);
            }

            // Update bone to bone mapping
            _fullBodyBoneIdToBonePairs = new Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>>();
            for (var bone = FullBodyTrackingBoneId.FullBody_Hips; bone < FullBodyTrackingBoneId.FullBody_End; bone++)
            {
                var boneId = (OVRSkeleton.BoneId)bone;
                var targetBoneId = _boneIdToBonePairs[(int)bone + 1];
                if (targetBoneId == FullBodyTrackingBoneId.Remove)
                {
                    continue;
                }
                
                var childBoneId = (OVRSkeleton.BoneId)targetBoneId;
                _fullBodyBoneIdToBonePairs.Add(boneId, 
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(boneId, childBoneId));
            }
            return true;
        }

        private void UpdateBoneIdToBonePairs()
        {
            _boneIdToBonePairs = new []
            {
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_SpineLower,
                FullBodyTrackingBoneId.FullBody_SpineMiddle,
                FullBodyTrackingBoneId.FullBody_SpineUpper,
                FullBodyTrackingBoneId.FullBody_Chest,
                FullBodyTrackingBoneId.FullBody_Neck,
                FullBodyTrackingBoneId.FullBody_Head,
                FullBodyTrackingBoneId.FullBody_Head,
                FullBodyTrackingBoneId.FullBody_LeftArmUpper,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftArmLower,
                FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightArmUpper,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightArmLower,
                FullBodyTrackingBoneId.FullBody_RightHandWrist,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandThumbProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandThumbDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandThumbTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftHandRingProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandRingIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandRingDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandRingTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbProximal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbDistal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightHandIndexProximal,
                FullBodyTrackingBoneId.FullBody_RightHandIndexIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandIndexDistal,
                FullBodyTrackingBoneId.FullBody_RightHandIndexTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleDistal,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightHandRingProximal,
                FullBodyTrackingBoneId.FullBody_RightHandRingIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandRingDistal,
                FullBodyTrackingBoneId.FullBody_RightHandRingTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightHandLittleProximal,
                FullBodyTrackingBoneId.FullBody_RightHandLittleIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandLittleDistal,
                FullBodyTrackingBoneId.FullBody_RightHandLittleTip,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftLowerLeg,
                FullBodyTrackingBoneId.FullBody_LeftFootAnkle,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftFootBall,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_LeftFootBall,
                FullBodyTrackingBoneId.FullBody_RightLowerLeg,
                FullBodyTrackingBoneId.FullBody_RightFootAnkle,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightFootBall,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.Remove,
                FullBodyTrackingBoneId.FullBody_RightFootBall,
            };
        }

        private void UpdateHumanBodyBoneToBoneId(Animator animator)
        {
            // The Humanoid Chest bone is mapped to the OVRSkeleton.SpineMiddle bone, instead of the default mapping
            // which is the OVRSkeleton.SpineUpper bone.
            _humanBodyBoneToBoneId = new []
            {
                FullBodyTrackingBoneId.FullBody_Hips, // Hips
                FullBodyTrackingBoneId.FullBody_LeftUpperLeg,
                FullBodyTrackingBoneId.FullBody_RightUpperLeg,
                FullBodyTrackingBoneId.FullBody_LeftLowerLeg,
                FullBodyTrackingBoneId.FullBody_RightLowerLeg,
                FullBodyTrackingBoneId.FullBody_LeftFootAnkle,
                FullBodyTrackingBoneId.FullBody_RightFootAnkle,
                FullBodyTrackingBoneId.FullBody_SpineLower, // Spine
                FullBodyTrackingBoneId.FullBody_SpineMiddle, // Chest
                FullBodyTrackingBoneId.FullBody_Neck, // Neck
                FullBodyTrackingBoneId.FullBody_Head, // Head
                FullBodyTrackingBoneId.FullBody_LeftShoulder,
                FullBodyTrackingBoneId.FullBody_RightShoulder,
                FullBodyTrackingBoneId.FullBody_LeftArmUpper,
                FullBodyTrackingBoneId.FullBody_RightArmUpper,
                FullBodyTrackingBoneId.FullBody_LeftArmLower,
                FullBodyTrackingBoneId.FullBody_RightArmLower,
                FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                FullBodyTrackingBoneId.FullBody_RightHandWrist,
                FullBodyTrackingBoneId.FullBody_LeftFootBall,
                FullBodyTrackingBoneId.FullBody_RightFootBall,
                FullBodyTrackingBoneId.Remove, // LeftEye
                FullBodyTrackingBoneId.Remove, // RightEye
                FullBodyTrackingBoneId.Remove, // Jaw
                FullBodyTrackingBoneId.FullBody_LeftHandThumbMetacarpal,
                FullBodyTrackingBoneId.FullBody_LeftHandThumbProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandThumbDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandIndexDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandMiddleDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandRingProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandRingIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandRingDistal,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleProximal,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleIntermediate,
                FullBodyTrackingBoneId.FullBody_LeftHandLittleDistal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbMetacarpal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbProximal,
                FullBodyTrackingBoneId.FullBody_RightHandThumbDistal,
                FullBodyTrackingBoneId.FullBody_RightHandIndexProximal,
                FullBodyTrackingBoneId.FullBody_RightHandIndexIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandIndexDistal,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleProximal,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandMiddleDistal,
                FullBodyTrackingBoneId.FullBody_RightHandRingProximal,
                FullBodyTrackingBoneId.FullBody_RightHandRingIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandRingDistal,
                FullBodyTrackingBoneId.FullBody_RightHandLittleProximal,
                FullBodyTrackingBoneId.FullBody_RightHandLittleIntermediate,
                FullBodyTrackingBoneId.FullBody_RightHandLittleDistal,
                FullBodyTrackingBoneId.FullBody_Chest, // UpperChest
            };

            // If the upper chest is missing, update the chest mapping to map to the SpineUpper bone, rather than
            // the Chest bone.
            if (animator.GetBoneTransform(HumanBodyBones.UpperChest) == null)
            {
                _humanBodyBoneToBoneId[(int)HumanBodyBones.Chest] = FullBodyTrackingBoneId.FullBody_SpineUpper;
            }
        }
    }
}
