// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
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
        const string _HUMANOID_REFERENCE_POSE_ASSET_NAME = "BodyTrackingHumanoidReferencePose";

        /// <summary>
        /// Adds joint adjustments for an animator.
        /// </summary>
        /// <param name="animator">Animator component.</param>
        /// <param name="retargetingLayer">Retargeting layer component to change adjustments of.</param>

        public static void AddJointAdjustments(Animator animator, RetargetingLayer retargetingLayer)
        {
            string[] guids = AssetDatabase.FindAssets(_HUMANOID_REFERENCE_POSE_ASSET_NAME);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Cannot compute adjustments because asset {_HUMANOID_REFERENCE_POSE_ASSET_NAME} " +
                    "cannot be found.");
                return;
            }

            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            RestPoseObjectHumanoid restPoseObject = AssetDatabase.LoadAssetAtPath<RestPoseObjectHumanoid>(pathToAsset);
            var hipAngleDifference = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorJoint
                (animator, HumanBodyBones.Hips);

            var adjustmentsField =
                typeof(RetargetingLayer).GetField(
                    "_adjustments",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            if (adjustmentsField != null)
            {
                adjustmentsField.SetValue(retargetingLayer, new[]
                {
                    new JointAdjustment
                    {
                        Joint = HumanBodyBones.Hips,
                        RotationTweaks = new Quaternion[] { hipAngleDifference } 
                    },
                    // manual adjustments follow to address possible shoulder issues
                    new JointAdjustment
                    {
                        Joint = HumanBodyBones.LeftShoulder,
                        RotationTweaks = new Quaternion[] { Quaternion.Euler(0.0f, 0.0f, 15.0f) }
                    },
                    new JointAdjustment
                    {
                        Joint = HumanBodyBones.RightShoulder,
                        RotationTweaks = new Quaternion[] { Quaternion.Euler(0.0f, 0.0f, 15.0f) }
                    }
                });
            }
        }
    }
}
