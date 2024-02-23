// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Has common menu functions.
    /// </summary>
    public class HelperMenusCommon
    {
        private const string _HUMANOID_REFERENCE_POSE_ASSET_NAME = "BodyTrackingHumanoidReferencePose";
        private const string _HUMANOID_REFERENCE_T_POSE_ASSET_NAME = "BodyTrackingHumanoidReferenceTPose";
        private const float _tPoseArmDirectionMatchThreshold = 0.95f;
        private const float _tPoseArmHeightMatchThreshold = 0.1f;
        private static readonly HumanBodyBones[] _excludedAccumulatedRotationBones = new HumanBodyBones[]
        {
            HumanBodyBones.Hips, HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder
        };

        /// <summary>
        /// Find and return the reference rest pose humanoid object in the project.
        /// </summary>
        /// <returns>The rest pose humanoid object.</returns>
        public static RestPoseObjectHumanoid GetRestPoseObject(bool isTPose = false)
        {
            var poseAssetName = isTPose ?
                _HUMANOID_REFERENCE_T_POSE_ASSET_NAME : _HUMANOID_REFERENCE_POSE_ASSET_NAME;
            string[] guids = AssetDatabase.FindAssets(poseAssetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {poseAssetName} cannot be found.");
                return null;
            }

            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RestPoseObjectHumanoid>(pathToAsset);
        }

        /// <summary>
        /// Given an animator, determine if the avatar is in T-pose or A-pose.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <returns>True if T-pose.</returns>
        public static bool CheckIfTPose(Animator animator)
        {
            var shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            var upperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var lowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (shoulder == null)
            {
                // Naive approach to check if the lowerArm is placed in A-pose or not when
                // missing a shoulder bone.
                return upperArm.position.y - lowerArm.position.y < _tPoseArmHeightMatchThreshold;
            }
            var shoulderToUpperArm = (shoulder.position - upperArm.position).normalized;
            var lowerArmToHand = (lowerArm.position - hand.position).normalized;
            var armDirectionMatch = Vector3.Dot(shoulderToUpperArm, lowerArmToHand);
            return armDirectionMatch >= _tPoseArmDirectionMatchThreshold;
        }

        /// <summary>
        /// Adds joint adjustments for an animator.
        /// </summary>
        /// <param name="animator">Animator component.</param>
        /// <param name="retargetingLayer">Retargeting layer component to change adjustments of.</param>
        public static void AddJointAdjustments(Animator animator, RetargetingLayer retargetingLayer)
        {
            var restPoseObject = GetRestPoseObject(CheckIfTPose(animator));
            if (restPoseObject == null)
            {
                Debug.LogError($"Cannot compute adjustments because asset {_HUMANOID_REFERENCE_POSE_ASSET_NAME} " +
                               "cannot be found.");
                return;
            }

            var adjustmentsField =
                typeof(RetargetingLayer).GetField(
                    "_adjustments",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            if (adjustmentsField != null)
            {
                var fullBodyDeformationConstraint = retargetingLayer.GetComponentInChildren<FullBodyDeformationConstraint>(true);
                if (fullBodyDeformationConstraint != null)
                {
                    adjustmentsField.SetValue(retargetingLayer,
                        GetDeformationJointAdjustments(animator, fullBodyDeformationConstraint));
                }
                else
                {
                    adjustmentsField.SetValue(retargetingLayer,
                        GetFallbackJointAdjustments(animator, restPoseObject));
                }
            }
        }

        private static JointAdjustment[] GetDeformationJointAdjustments(Animator animator, FullBodyDeformationConstraint constraint)
        {
            var adjustments = new List<JointAdjustment>();
            var deformationData = constraint.data as IFullBodyDeformationData;
            var isMissingUpperChestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest) == null;
            var boneAdjustmentData = deformationData.BoneAdjustments;
            foreach (var boneAdjustment in boneAdjustmentData)
            {
                var rotationTweak = boneAdjustment.Adjustment;
                if (isMissingUpperChestBone && boneAdjustment.Bone == HumanBodyBones.Chest)
                {
                    // As UpperChest -> Neck and Chest -> Neck bone pair directions on the rest pose
                    // skeleton are opposite, this rotation tweak is inverted.
                    rotationTweak = Quaternion.Inverse(rotationTweak);
                }
                if (boneAdjustment.Bone == HumanBodyBones.UpperChest)
                {
                    // Reducing the rotation adjustment on the upper chest yields a better result visually.
                    rotationTweak =
                        Quaternion.Slerp(Quaternion.identity, rotationTweak, 0.5f);
                }
                var adjustment = new JointAdjustment()
                {
                    Joint = boneAdjustment.Bone,
                    RotationTweaks = new[] { rotationTweak }
                };
                adjustments.Add(adjustment);
            }
            return adjustments.ToArray();
        }

        private static JointAdjustment[] GetFallbackJointAdjustments(Animator animator, RestPoseObjectHumanoid restPoseObject)
        {
            var hipAngleDifference = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorJoint
                (animator, HumanBodyBones.Hips);
            var shoulderAngleDifferences =
                DeformationCommon.GetShoulderAdjustments(animator, restPoseObject, Quaternion.identity);
            return new[]
            {
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.Hips,
                    RotationTweaks = new[] { hipAngleDifference }
                },
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.LeftShoulder,
                    RotationTweaks = new[] { shoulderAngleDifferences[0].Adjustment }
                },
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.RightShoulder,
                    RotationTweaks = new[] { shoulderAngleDifferences[1].Adjustment }
                }
            };
        }

        public static void AddBlendHandRetargetingProcessor(RetargetingLayer retargetingLayer, Handedness handedness)
        {
            bool needCorrectHand = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var correctHand = processor as RetargetingBlendHandProcessor;
                if (correctHand != null)
                {
                    if (correctHand.GetHandedness() == handedness)
                    {
                        needCorrectHand = false;
                    }
                }
            }

            if (!needCorrectHand)
            {
                return;
            }

            bool isFullBody = retargetingLayer.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;
            var blendHand = ScriptableObject.CreateInstance<RetargetingBlendHandProcessor>();
            var handednessString = handedness == Handedness.Left ? "Left" : "Right";
            Undo.RegisterCreatedObjectUndo(blendHand, $"Create ({handednessString}) blend hand.");
            Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            blendHand.BlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            blendHand.IsFullBody = isFullBody;
            if (isFullBody)
            {
                blendHand.FullBodyBoneIdToTest = handedness == Handedness.Left ?
                    OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist :
                    OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist;
            }
            else
            {
                blendHand.BoneIdToTest = handedness == Handedness.Left ?
                    OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist :
                    OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandWrist;
            }
            blendHand.name = $"Blend{handednessString}Hand";
            // Add processor at beginning so that it runs before other processors.
            if (retargetingLayer.RetargetingProcessors.Count > 0)
            {
                retargetingLayer.RetargetingProcessors.Insert(0, blendHand);
            }
            else
            {
                retargetingLayer.AddRetargetingProcessor(blendHand);
            }
        }

        public static bool DestroyLegacyComponents<Component>(GameObject gameObject)
        {
            var componentsFound = gameObject.GetComponents<Component>();

            foreach(var componentFound in componentsFound)
            {
                Undo.DestroyObjectImmediate(componentFound as Object);
            }

            return componentsFound.Length > 0;
        }
    }
}
