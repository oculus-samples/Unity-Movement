// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allow adding components at runtime.
    /// </summary>
    public class AddComponentsRuntime
    {
        /// <summary>
        /// Sets up character for retargeting, no animation rigging.
        /// </summary>
        /// <param name="selectedGameObject">GameObject used for setup process.</param>
        /// <param name="isFullBody">Allows toggling full body or not.</param>
        public static void SetupCharacterForRetargeting(GameObject selectedGameObject,
            bool isFullBody = false)
        {
            var ovrBodyComponent = selectedGameObject.AddComponent<OVRBody>();
            var retargeterComponent = selectedGameObject.AddComponent<OVRUnityHumanoidSkeletonRetargeter>();
            typeof(RetargetingLayer).GetField(
                "_skeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                retargeterComponent, isFullBody ? OVRSkeleton.SkeletonType.FullBody : OVRSkeleton.SkeletonType.Body);
            typeof(OVRBody).GetField(
                "_providedSkeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                ovrBodyComponent, isFullBody ? OVRPlugin.BodyJointSet.FullBody : OVRPlugin.BodyJointSet.UpperBody);
        }

        /// <summary>
        /// Adds animation rigging + retargeting at runtime.
        /// </summary>
        /// <param name="selectedGameObject">GameObject to add animation rigging + retargeting too.</param>
        /// <param name="isFullBody">Allows toggling full body or not.</param>
        /// <param name="addConstraints">Allows adding constraints or not.</param>
        /// <param name="restPoseObjectHumanoid">Allows using the rest pose object or not.</param>
        public static void SetupCharacterForAnimationRiggingRetargeting(
            GameObject selectedGameObject,
            bool isFullBody = false,
            bool addConstraints = false,
            RestPoseObjectHumanoid restPoseObjectHumanoid = null)
        {
            selectedGameObject.SetActive(false);

            HelperMenusBody.SetupCharacterForAnimationRiggingRetargetingConstraints(selectedGameObject, restPoseObjectHumanoid, addConstraints, isFullBody, true);

            RigBuilder rigBuilder = selectedGameObject.GetComponent<RigBuilder>();
            Animator animatorComp = selectedGameObject.GetComponent<Animator>();
            if (isFullBody)
            {
                animatorComp.gameObject.SetActive(true);
                animatorComp.gameObject.SetActive(false);
            }

            // Disable rig builder to allow T-pose to be captured by retargeting without having animation
            // rigging forcing character into the "motorcycle pose" first.
            rigBuilder.enabled = false;
            selectedGameObject.SetActive(true);
        }
    }
}
