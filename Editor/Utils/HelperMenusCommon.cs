// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
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
        /// Find and return the reference rest pose humanoid object in the project.
        /// </summary>
        /// <returns>The rest pose humanoid object.</returns>
        public static RestPoseObjectHumanoid GetRestPoseObject()
        {
            string[] guids = AssetDatabase.FindAssets(_HUMANOID_REFERENCE_POSE_ASSET_NAME);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {_HUMANOID_REFERENCE_POSE_ASSET_NAME} cannot be found.");
                return null;
            }

            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RestPoseObjectHumanoid>(pathToAsset);
        }

        /// <summary>
        /// Adds joint adjustments for an animator.
        /// </summary>
        /// <param name="animator">Animator component.</param>
        /// <param name="retargetingLayer">Retargeting layer component to change adjustments of.</param>
        public static void AddJointAdjustments(Animator animator, RetargetingLayer retargetingLayer)
        {
            var restPoseObject = GetRestPoseObject();
            if (restPoseObject == null)
            {
                Debug.LogError($"Cannot compute adjustments because asset {_HUMANOID_REFERENCE_POSE_ASSET_NAME} " +
                               "cannot be found.");
                return;
            }

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
                        RotationChange = hipAngleDifference
                    },
                    // manual adjustments follow to address possible shoulder issues
                    new JointAdjustment
                    {
                        Joint = HumanBodyBones.LeftShoulder,
                        RotationChange = Quaternion.Euler(0.0f, 0.0f, 15.0f)
                    },
                    new JointAdjustment
                    {
                        Joint = HumanBodyBones.RightShoulder,
                        RotationChange = Quaternion.Euler(0.0f, 0.0f, 15.0f)
                    }
                });
            }
        }
    }
}
