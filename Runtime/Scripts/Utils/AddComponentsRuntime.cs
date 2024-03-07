// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Tracking;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System;
using System.Reflection;
using System.Collections.Generic;
using static OVRUnityHumanoidSkeletonRetargeter;
using static Oculus.Movement.AnimationRigging.ExternalBoneTargets;

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
        /// Adds Animation rigging + retargeting at runtime. Similar to the HelperMenus version except
        /// no undo actions since those are not allowed at runtime.
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
            try
            {
                AddComponentsHelper.ValidGameObjectForAnimationRigging(selectedGameObject);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"Retargeting setup error: {e.Message}.");
                return;
            }
            // Disable the character, add components, THEN enable it.
            // Animation rigging doesn't start properly otherwise.
            selectedGameObject.SetActive(false);
            var mainParent = selectedGameObject;
            var previousPositionAndRotation =
                new AffineTransform(selectedGameObject.transform.position, selectedGameObject.transform.rotation);

            // Bring the object to root so that the auto adjustments calculations are correct.
            selectedGameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Add the retargeting and body tracking components at root first.
            RetargetingLayer retargetingLayer = AddComponentsHelper.AddMainRetargetingComponent(
                mainParent, isFullBody, true);

            GameObject rigObject = null;
            RigBuilder rigBuilder = null;
            (rigBuilder, rigObject) = AddComponentsHelper.AddBasicAnimationRiggingComponents(mainParent,
                true);
            // disable rig builder. in case we need to set up any constraints, we might need to enable
            // the animator. but we don't want the rig to evaluate any constraints, so keep the rig disabled
            // until the character has been set up.
            rigBuilder.enabled = false;

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                AddComponentsHelper.AddRetargetingConstraint(rigObject, retargetingLayer, true, true);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            Animator animatorComp = selectedGameObject.GetComponent<Animator>();

            // Body deformation.
            if (addConstraints)
            {
                if (isFullBody)
                {
                    BoneTarget[] boneTargets = AddComponentsHelper.AddBoneTargets(rigObject, animatorComp, true);
                    FullBodyDeformationConstraint deformationConstraint =
                        AddFullBodyDeformationConstraint(rigObject, animatorComp, boneTargets, restPoseObjectHumanoid);

                    AddComponentsHelper.SetupExternalBoneTargets(retargetingLayer, true, boneTargets);
                }
                else
                {
                    BoneTarget[] boneTargets = AddComponentsHelper.AddBoneTargets(rigObject, animatorComp, false);
                    DeformationConstraint deformationConstraint =
                        AddDeformationConstraint(rigObject, animatorComp, boneTargets);
                    constraintMonos.Add(deformationConstraint);

                    AddComponentsHelper.SetupExternalBoneTargets(retargetingLayer, true, boneTargets);
                }
            }

            // Add final components to tie everything together.
            AddJointAdjustments(animatorComp, retargetingLayer, restPoseObjectHumanoid);
            AddComponentsHelper.AddRetargetingAnimationRig(retargetingLayer,
                rigBuilder, constraintMonos.ToArray());

            // Add retargeting processors to the retargeting layer.
            AddComponentsHelper.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Left, true);
            AddComponentsHelper.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Right, true);
            AddComponentsHelper.AddCorrectBonesRetargetingProcessor(retargetingLayer, true);
            AddComponentsHelper.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left,
                true);
            AddComponentsHelper.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right,
                true);
            AddComponentsHelper.AddHandDeformationRetargetingProcessor(retargetingLayer, true);

            selectedGameObject.transform.SetPositionAndRotation(
                previousPositionAndRotation.translation, previousPositionAndRotation.rotation);
            if (isFullBody)
            {
                animatorComp.gameObject.SetActive(true);
                animatorComp.gameObject.SetActive(false);
            }

            rigBuilder.enabled = true;
            selectedGameObject.SetActive(true);
        }

        private static DeformationConstraint AddDeformationConstraint(
            GameObject rigObject, Animator animator, BoneTarget[] spineBoneTargets)
        {
            DeformationConstraint deformationConstraint;
            GameObject deformationConstraintObject =
                new GameObject("Deformation");
            deformationConstraint =
                deformationConstraintObject.AddComponent<DeformationConstraint>();

            deformationConstraintObject.transform.SetParent(rigObject.transform, false);
            deformationConstraintObject.transform.localPosition = Vector3.zero;
            deformationConstraintObject.transform.localRotation = Quaternion.identity;
            deformationConstraintObject.transform.localScale = Vector3.one;

            foreach (var spineBoneTarget in spineBoneTargets)
            {
                spineBoneTarget.Target.SetParent(deformationConstraint.transform, false);
            }

            deformationConstraint.data.SpineTranslationCorrectionTypeField
                = DeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead;
            deformationConstraint.data.SpineLowerAlignmentWeight = 1.0f;
            deformationConstraint.data.SpineUpperAlignmentWeight = 0.5f;
            deformationConstraint.data.ChestAlignmentWeight = 0.0f;
            deformationConstraint.data.LeftShoulderWeight = 0.75f;
            deformationConstraint.data.RightShoulderWeight = 0.75f;
            deformationConstraint.data.LeftArmWeight = 1.0f;
            deformationConstraint.data.RightArmWeight = 1.0f;
            deformationConstraint.data.LeftHandWeight = 1.0f;
            deformationConstraint.data.RightHandWeight = 1.0f;

            // enable to find bones
            animator.gameObject.SetActive(true);
            // set up deformation but prevent it from running any code at runtime
            deformationConstraint.enabled = false;
            deformationConstraint.data.AssignAnimator(animator);
            deformationConstraint.data.SetUpLeftArmData();
            deformationConstraint.data.SetUpRightArmData();
            deformationConstraint.data.SetUpHipsToHeadBones();
            deformationConstraint.data.SetUpHipsToHeadBoneTargets(deformationConstraint.transform);
            deformationConstraint.data.SetUpBonePairs();
            deformationConstraint.data.InitializeStartingScale();
            animator.gameObject.SetActive(false);
            // re-enable deformation so that when the animator game object is turned on, it will activate
            deformationConstraint.enabled = true;

            return deformationConstraint;
        }

        private static FullBodyDeformationConstraint AddFullBodyDeformationConstraint(
            GameObject rigObject, Animator animator, BoneTarget[] spineBoneTargets,
            RestPoseObjectHumanoid restPoseObjectHumanoid)
        {
            FullBodyDeformationConstraint deformationConstraint = null;

            GameObject deformationConstraintObject =
                new GameObject("Deformation");
            deformationConstraint =
                deformationConstraintObject.AddComponent<FullBodyDeformationConstraint>();

            deformationConstraintObject.transform.SetParent(rigObject.transform, false);
            deformationConstraintObject.transform.localPosition = Vector3.zero;
            deformationConstraintObject.transform.localRotation = Quaternion.identity;
            deformationConstraintObject.transform.localScale = Vector3.one;

            foreach (var spineBoneTarget in spineBoneTargets)
            {
                spineBoneTarget.Target.SetParent(deformationConstraint.transform, false);
            }

            deformationConstraint.data.SpineTranslationCorrectionTypeField
                = FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead;
            deformationConstraint.data.SpineLowerAlignmentWeight = 1.0f;
            deformationConstraint.data.SpineUpperAlignmentWeight = 1.0f;
            deformationConstraint.data.ChestAlignmentWeight = 0.0f;
            deformationConstraint.data.LeftShoulderWeight = 0.75f;
            deformationConstraint.data.RightShoulderWeight = 0.75f;
            deformationConstraint.data.LeftArmWeight = 1.0f;
            deformationConstraint.data.RightArmWeight = 1.0f;
            deformationConstraint.data.LeftHandWeight = 1.0f;
            deformationConstraint.data.RightHandWeight = 1.0f;
            deformationConstraint.data.AlignLeftLegWeight = 1.0f;
            deformationConstraint.data.AlignRightLegWeight = 1.0f;
            deformationConstraint.data.LeftToesWeight = 1.0f;
            deformationConstraint.data.RightToesWeight = 1.0f;
            deformationConstraint.data.AlignFeetWeight = 0.75f;

            // enable to find bones
            animator.gameObject.SetActive(true);
            // set up deformation but prevent it from running any code at runtime
            deformationConstraint.enabled = false;
            deformationConstraint.data.AssignAnimator(animator);
            deformationConstraint.data.SetUpLeftArmData();
            deformationConstraint.data.SetUpRightArmData();
            deformationConstraint.data.SetUpLeftLegData();
            deformationConstraint.data.SetUpRightLegData();
            deformationConstraint.data.SetUpHipsAndHeadBones();
            deformationConstraint.data.SetUpBonePairs();
            deformationConstraint.data.SetUpBoneTargets(deformationConstraint.transform);
            deformationConstraint.data.SetUpAdjustments(restPoseObjectHumanoid);
            deformationConstraint.data.InitializeStartingScale();
            animator.gameObject.SetActive(false);
            // re-enable deformation so that when the animator game object is turned on, it will activate
            deformationConstraint.enabled = true;

            return deformationConstraint;
        }

        private static void AddJointAdjustments(Animator animator, RetargetingLayer retargetingLayer,
            RestPoseObjectHumanoid restPoseObjectHumanoid)
        {
            var adjustmentsField =
                typeof(RetargetingLayer).GetField(
                    "_adjustments",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            if (adjustmentsField != null)
            {
                var adjustments = new List<JointAdjustment>();
                var fullBodyDeformationConstraint = retargetingLayer.GetComponentInChildren<FullBodyDeformationConstraint>(true);
                if (fullBodyDeformationConstraint != null)
                {
                    var deformationData = fullBodyDeformationConstraint.data as IFullBodyDeformationData;
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
                    adjustmentsField.SetValue(retargetingLayer, adjustments.ToArray());
                }
                else
                {
                    // enable animator to find bones, temporarily.
                    animator.gameObject.SetActive(true);
                    var hipAngleDifference = restPoseObjectHumanoid.CalculateRotationDifferenceFromRestPoseToAnimatorJoint
                                 (animator, HumanBodyBones.Hips);
                    animator.gameObject.SetActive(false);
                    adjustmentsField.SetValue(retargetingLayer, new[]
                    {
                        new JointAdjustment()
                        {
                            Joint = HumanBodyBones.Hips,
                            RotationTweaks = new [] { hipAngleDifference }
                        },
                        new JointAdjustment()
                        {
                            Joint = HumanBodyBones.LeftShoulder,
                            RotationTweaks = new [] { Quaternion.Euler(0,0, 15) }
                        },
                        new JointAdjustment()
                        {
                            Joint = HumanBodyBones.RightShoulder,
                            RotationTweaks = new [] { Quaternion.Euler(0,0, 15) }
                        }
                    });
                }
            }
        }
    }
}
