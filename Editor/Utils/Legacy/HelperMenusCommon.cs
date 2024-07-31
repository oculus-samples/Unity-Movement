// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.AnimationRigging.Deprecated;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.ExternalBoneTargets;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils.Legacy
{
    /// <summary>
    /// Has common menu functions.
    /// </summary>
    public class HelperMenusCommon
    {
        /// <summary>
        /// Prefix for movement samples one-click menus.
        /// </summary>
        public const string _MOVEMENT_SAMPLES_MENU =
            "GameObject/Movement Samples/";

        private const string _HUMANOID_REFERENCE_POSE_ASSET_NAME = "BodyTrackingHumanoidReferencePose";
        private const string _HUMANOID_REFERENCE_T_POSE_ASSET_NAME = "BodyTrackingHumanoidReferenceTPose";
        private const float _tPoseArmDirectionMatchThreshold = 0.95f;
        private const float _tPoseArmHeightMatchThreshold = 0.1f;

        private const string _LEFT_HANDEDNESS_STRING = "Left";
        private const string _RIGHT_HANDEDNESS_STRING = "Right";

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
        public static void AddJointAdjustments(
            Animator animator,
            RetargetingLayer retargetingLayer)
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
                        GetDeformationJointAdjustments(animator, restPoseObject, fullBodyDeformationConstraint));
                }
                else
                {
                    adjustmentsField.SetValue(retargetingLayer,
                        GetFallbackJointAdjustments(animator, restPoseObject));
                }
            }
        }

        private static JointAdjustment[] GetDeformationJointAdjustments(
            Animator animator,
            RestPoseObjectHumanoid restPoseObject,
            FullBodyDeformationConstraint constraint)
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
                    // If the spine to chest bone isn't aligned, then inverse the rotation as we want
                    // to apply the adjustment relative to the character's spine direction.
                    var currentBoneDir = (animator.GetBoneTransform(HumanBodyBones.Chest).position -
                                         animator.GetBoneTransform(HumanBodyBones.Spine).position).normalized;
                    var previousBoneDir = (animator.GetBoneTransform(HumanBodyBones.Chest).position -
                                          animator.GetBoneTransform(HumanBodyBones.Hips).position).normalized;
                    var boneDirDotComparison = Vector3.Dot(previousBoneDir, currentBoneDir);
                    if (boneDirDotComparison < 1.0f - Mathf.Epsilon)
                    {
                        rotationTweak = Quaternion.Inverse(rotationTweak);
                    }
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
                    RotationTweaks = new[]
                    {
                        rotationTweak
                    }
                };
                adjustments.Add(adjustment);
            }
            // Assume that we want the feet to be pointing world space forward.
            var footAdjustments = GetFootAdjustments(animator, restPoseObject, Vector3.forward);
            adjustments.AddRange(footAdjustments);
            return adjustments.ToArray();
        }

        private static JointAdjustment[] GetFallbackJointAdjustments(
            Animator animator,
            RestPoseObjectHumanoid restPoseObject)
        {
            var hipAngleDifference = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorJoint
                (animator, HumanBodyBones.Hips);
            var shoulderAngleDifferences =
                DeformationCommon.GetShoulderAdjustments(animator, restPoseObject, Quaternion.identity);
            // Assume that we want the feet to be pointing world space forward.
            var footAdjustments = GetFootAdjustments(animator, restPoseObject, Vector3.forward);
            var adjustmentAlignment =
                DeformationCommon.GetHipsRightForwardAlignmentForAdjustments(animator, Vector3.right, Vector3.forward);
            List<JointAdjustment> result = new List<JointAdjustment>
            {
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.Hips,
                    RotationTweaks = new[]
                    {
                        Quaternion.Euler(adjustmentAlignment * hipAngleDifference.eulerAngles)
                    }
                },
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.LeftShoulder,
                    RotationTweaks = new[]
                    {
                        Quaternion.Euler(adjustmentAlignment * shoulderAngleDifferences[0].Adjustment.eulerAngles)
                    }
                },
                new JointAdjustment()
                {
                    Joint = HumanBodyBones.RightShoulder,
                    RotationTweaks = new[]
                    {
                        Quaternion.Euler(adjustmentAlignment * shoulderAngleDifferences[1].Adjustment.eulerAngles)
                    }
                }
            };
            if (footAdjustments.Length > 0)
            {
                result.Add(footAdjustments[0]);
                result.Add(footAdjustments[1]);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Adds components necessary for animation rigging to work.
        /// </summary>
        /// <param name="mainParent">Container for animation rigging components.</param>
        /// <returns>The rig builder and rig components created.</returns>
        public static (RigBuilder, GameObject) AddBasicAnimationRiggingComponents(
            GameObject mainParent)
        {
            Rig rigComponent = mainParent.GetComponentInChildren<Rig>();
            if (!rigComponent)
            {
                // Create rig for constraints.
                GameObject rigObject = new GameObject("Rig");
                rigComponent = rigObject.AddComponent<Rig>();
                rigComponent.weight = 1.0f;
                if (Application.isEditor)
                {
                    Undo.RegisterCreatedObjectUndo(rigObject, "Create Rig");
                }
            }

            RigBuilder rigBuilder = mainParent.GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                rigBuilder = mainParent.AddComponent<RigBuilder>();
                rigBuilder.layers = new List<RigLayer>
                {
                    new RigLayer(rigComponent, true)
                };
                if (Application.isEditor)
                {
                    Undo.RegisterCreatedObjectUndo(rigBuilder, "Create RigBuilder");
                }
            }

            if (Application.isEditor)
            {
                Undo.SetTransformParent(rigComponent.transform, mainParent.transform, "Add Rig to Main Parent");
                Undo.RegisterCompleteObjectUndo(rigComponent, "Rig Component Transform init");
            }
            else
            {
                rigComponent.transform.SetParent(mainParent.transform, true);
            }
            rigComponent.transform.localPosition = Vector3.zero;
            rigComponent.transform.localRotation = Quaternion.identity;
            rigComponent.transform.localScale = Vector3.one;

            return (rigBuilder, rigComponent.gameObject);
        }

        /// <summary>
        /// Adds main retargeting component and sets it up.
        /// </summary>
        /// <param name="mainParent">GameObject to add to.</param>
        /// <param name="isFullBody">If full body or not.</param>
        /// <returns>RetargetingLayer that was added.</returns>
        public static RetargetingLayer AddMainRetargetingComponent(
            GameObject mainParent,
            bool isFullBody)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();

            // Check for old retargeting layer first.
            RetargetingLayer baseRetargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (retargetingLayer == null && baseRetargetingLayer != null)
            {
                Undo.DestroyObjectImmediate(baseRetargetingLayer);
            }

            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
                Undo.RegisterCreatedObjectUndo(retargetingLayer, "Add Retargeting Layer");
            }

            if (isFullBody)
            {
                var fullBodySectionToPosition =
                    typeof(OVRUnityHumanoidSkeletonRetargeter).GetField(
                        "_fullBodySectionToPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                if (fullBodySectionToPosition != null)
                {
                    fullBodySectionToPosition.SetValue(retargetingLayer, new[]
                    {
                        OVRHumanBodyBonesMappings.BodySection.LeftArm,
                        OVRHumanBodyBonesMappings.BodySection.RightArm,
                        OVRHumanBodyBonesMappings.BodySection.LeftHand,
                        OVRHumanBodyBonesMappings.BodySection.RightHand,
                        OVRHumanBodyBonesMappings.BodySection.Hips,
                        OVRHumanBodyBonesMappings.BodySection.Back,
                        OVRHumanBodyBonesMappings.BodySection.Neck,
                        OVRHumanBodyBonesMappings.BodySection.Head,
                        OVRHumanBodyBonesMappings.BodySection.LeftLeg,
                        OVRHumanBodyBonesMappings.BodySection.LeftFoot,
                        OVRHumanBodyBonesMappings.BodySection.RightLeg,
                        OVRHumanBodyBonesMappings.BodySection.RightFoot
                    });
                }
            }

            var bodySectionToPosition =
                typeof(OVRUnityHumanoidSkeletonRetargeter).GetField(
                    "_bodySectionToPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (bodySectionToPosition != null)
            {
                bodySectionToPosition.SetValue(retargetingLayer, new[]
                {
                    OVRHumanBodyBonesMappings.BodySection.LeftArm,
                    OVRHumanBodyBonesMappings.BodySection.RightArm,
                    OVRHumanBodyBonesMappings.BodySection.LeftHand,
                    OVRHumanBodyBonesMappings.BodySection.RightHand,
                    OVRHumanBodyBonesMappings.BodySection.Hips,
                    OVRHumanBodyBonesMappings.BodySection.Back,
                    OVRHumanBodyBonesMappings.BodySection.Neck,
                    OVRHumanBodyBonesMappings.BodySection.Head
                });
            }

            EditorUtility.SetDirty(retargetingLayer);

            OVRBody bodyComp = mainParent.GetComponent<OVRBody>();
            if (!bodyComp)
            {
                bodyComp = mainParent.AddComponent<OVRBody>();
                Undo.RegisterCreatedObjectUndo(bodyComp, "Add OVRBody component");
            }

            typeof(RetargetingLayer).GetField(
                "_skeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                    retargetingLayer, isFullBody ?
                    OVRSkeleton.SkeletonType.FullBody : OVRSkeleton.SkeletonType.Body);
            typeof(OVRBody).GetField(
                "_providedSkeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                    bodyComp, isFullBody ?
                    OVRPlugin.BodyJointSet.FullBody : OVRPlugin.BodyJointSet.UpperBody);

            retargetingLayer.EnableTrackingByProxy = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(retargetingLayer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bodyComp);

            return retargetingLayer;
        }

        /// <summary>
        /// Adds bone targets to rig object.
        /// </summary>
        /// <param name="rigObject">Rig object serving as parent for bone targets.</param>
        /// <param name="animator">Animator to obtain target bone.</param>
        /// <param name="isFullBody">Indicates if full body (or not).</param>
        /// <returns>Array of bone targets added.</returns>
        public static BoneTarget[] AddBoneTargets(
            GameObject rigObject, Animator animator, bool isFullBody)
        {
            var boneTargets = new List<BoneTarget>();
            Transform hipsTarget = AddBoneTarget(rigObject, "HipsTarget",
                animator.GetBoneTransform(HumanBodyBones.Hips));
            Transform spineLowerTarget = AddBoneTarget(rigObject, "SpineLowerTarget",
                animator.GetBoneTransform(HumanBodyBones.Spine));
            Transform spineUpperTarget = AddBoneTarget(rigObject, "SpineUpperTarget",
                animator.GetBoneTransform(HumanBodyBones.Chest));
            Transform chestTarget = AddBoneTarget(rigObject, "ChestTarget",
                animator.GetBoneTransform(HumanBodyBones.UpperChest));
            Transform neckTarget = AddBoneTarget(rigObject, "NeckTarget",
                animator.GetBoneTransform(HumanBodyBones.Neck));
            Transform headTarget = AddBoneTarget(rigObject, "HeadTarget",
                animator.GetBoneTransform(HumanBodyBones.Head));

            Tuple<OVRSkeleton.BoneId, Transform>[] bonesToRetarget = null;

            if (isFullBody)
            {
                Transform leftFootTarget = AddBoneTarget(rigObject, "LeftFootTarget",
                    animator.GetBoneTransform(HumanBodyBones.LeftFoot));
                Transform leftToesTarget = AddBoneTarget(rigObject, "LeftToesTarget",
                    animator.GetBoneTransform(HumanBodyBones.LeftToes));
                Transform rightFootTarget = AddBoneTarget(rigObject, "RightFootTarget",
                    animator.GetBoneTransform(HumanBodyBones.RightFoot));
                Transform rightToesTarget = AddBoneTarget(rigObject, "RightToesTarget",
                    animator.GetBoneTransform(HumanBodyBones.RightToes));
                bonesToRetarget = new Tuple<OVRSkeleton.BoneId, Transform>[]
                {
                    new(OVRSkeleton.BoneId.FullBody_Hips, hipsTarget),
                    new(OVRSkeleton.BoneId.FullBody_SpineLower, spineLowerTarget),
                    new(OVRSkeleton.BoneId.FullBody_SpineUpper, spineUpperTarget),
                    new(OVRSkeleton.BoneId.FullBody_Chest, chestTarget),
                    new(OVRSkeleton.BoneId.FullBody_Neck, neckTarget),
                    new(OVRSkeleton.BoneId.FullBody_Head, headTarget),
                    new(OVRSkeleton.BoneId.FullBody_LeftFootAnkle, leftFootTarget),
                    new(OVRSkeleton.BoneId.FullBody_LeftFootBall, leftToesTarget),
                    new(OVRSkeleton.BoneId.FullBody_RightFootAnkle, rightFootTarget),
                    new(OVRSkeleton.BoneId.FullBody_RightFootBall, rightToesTarget)
                };
            }
            else
            {
                bonesToRetarget = new Tuple<OVRSkeleton.BoneId, Transform>[]
                {
                    new(OVRSkeleton.BoneId.Body_Hips, hipsTarget),
                    new(OVRSkeleton.BoneId.Body_SpineLower, spineLowerTarget),
                    new(OVRSkeleton.BoneId.Body_SpineUpper, spineUpperTarget),
                    new(OVRSkeleton.BoneId.Body_Chest, chestTarget),
                    new(OVRSkeleton.BoneId.Body_Neck, neckTarget),
                    new(OVRSkeleton.BoneId.Body_Head, headTarget)
                };
            }

            var boneIdToHumanBodyBone =
                isFullBody ? OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone :
                OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone;

            foreach (var boneToRetarget in bonesToRetarget)
            {
                BoneTarget boneRTTarget = new BoneTarget();
                boneRTTarget.BoneId = boneToRetarget.Item1;
                boneRTTarget.Target = boneToRetarget.Item2;
                boneRTTarget.HumanBodyBone = boneIdToHumanBodyBone[boneRTTarget.BoneId];
                boneTargets.Add(boneRTTarget);
            }
            return boneTargets.ToArray();
        }

        /// <summary>
        /// Adds transform for bone target.
        /// </summary>
        /// <param name="mainParent">Parent of bone target.</param>
        /// <param name="nameOfTarget">Name of targe to add.</param>
        /// <param name="targetTransform">Target transform to copy.</param>
        /// <returns>Bone target added.</returns>
        private static Transform AddBoneTarget(
            GameObject mainParent,
            string nameOfTarget,
            Transform targetTransform = null)
        {
            Transform boneTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (boneTarget == null)
            {
                GameObject boneTargetObject =
                    new GameObject(nameOfTarget);
                Undo.RegisterCreatedObjectUndo(boneTargetObject,
                    "Create Bone Target " + nameOfTarget);

                Undo.SetTransformParent(boneTargetObject.transform,
                    mainParent.transform,
                    $"Add Bone Target {nameOfTarget} To Main Parent");
                Undo.RegisterCompleteObjectUndo(boneTargetObject,
                    $"Bone Target {nameOfTarget} Transform Init");
                boneTarget = boneTargetObject.transform;
            }

            if (targetTransform != null)
            {
                boneTarget.position = targetTransform.position;
                boneTarget.rotation = targetTransform.rotation;
                boneTarget.localScale = targetTransform.localScale;
            }
            else
            {
                boneTarget.localPosition = Vector3.zero;
                boneTarget.localRotation = Quaternion.identity;
                boneTarget.localScale = Vector3.one;
            }
            return boneTarget;
        }

        /// <summary>
        /// Adds retargeting constraint under the rig object.
        /// </summary>
        /// <param name="rigObject">Rig object that will be the parent of the constraint.</param>
        /// <param name="retargetingLayer">Retargeting layer associated with the constraint.</param>
        /// <returns>Retargeting constraint under the rig.</returns>
        public static RetargetingAnimationConstraint AddRetargetingConstraint(
            GameObject rigObject,
            RetargetingLayer retargetingLayer)
        {
            RetargetingAnimationConstraint retargetConstraint =
                rigObject.GetComponentInChildren<RetargetingAnimationConstraint>(true);
            if (retargetConstraint == null)
            {
                GameObject retargetingAnimConstraintObj =
                    new GameObject("RetargetingConstraint");
                retargetConstraint =
                    retargetingAnimConstraintObj.AddComponent<RetargetingAnimationConstraint>();
                Undo.RegisterCreatedObjectUndo(retargetingAnimConstraintObj, "Create Retargeting Constraint");

                Undo.SetTransformParent(retargetingAnimConstraintObj.transform, rigObject.transform, "Add Retarget Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(retargetingAnimConstraintObj, "Retarget Constraint Transform Init");
                retargetConstraint.transform.localPosition = Vector3.zero;
                retargetConstraint.transform.localRotation = Quaternion.identity;
                retargetConstraint.transform.localScale = Vector3.one;

                // Keep retargeter disabled until it initializes properly.
                retargetConstraint.gameObject.SetActive(false);
            }

            retargetConstraint.RetargetingLayerComp = retargetingLayer;
            retargetConstraint.data.AllowDynamicAdjustmentsRuntime = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(retargetConstraint);
            return retargetConstraint;
        }

        private static JointAdjustment[] GetFootAdjustments(
            Animator animator,
            RestPoseObjectHumanoid restPoseObject,
            Vector3 desiredFootDirection)
        {
            if (restPoseObject.GetBonePoseData(HumanBodyBones.LeftToes) == null ||
                restPoseObject.GetBonePoseData(HumanBodyBones.RightToes) == null)
            {
                Debug.LogError("Expected valid toes data in the rest pose object for aligning the feet. " +
                               "No foot adjustments will be created.");
                return System.Array.Empty<JointAdjustment>();
            }

            var footAdjustments = new List<JointAdjustment>();
            var leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var legDotProduct = Vector3.Dot(leftLeg.forward, rightLeg.forward);
            bool shouldMirrorLegs = legDotProduct < 0.0f;
            var adjustmentAlignment =
                DeformationCommon.GetHipsRightForwardAlignmentForAdjustments(animator, Vector3.right, Vector3.forward);

            if (animator.GetBoneTransform(HumanBodyBones.LeftToes) == null)
            {
                var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                // Align the feet in the desired foot direction.
                var footToToes =
                    (restPoseObject.GetBonePoseData(HumanBodyBones.LeftToes).WorldPose.position -
                     restPoseObject.GetBonePoseData(HumanBodyBones.LeftFoot).WorldPose.position).normalized;
                var footAngle = Vector3.Angle(desiredFootDirection, footToToes);
                // Use the inverse rotation here as the rotation of the feet need to be inverted.
                var adjustment = Quaternion.Inverse(Quaternion.AngleAxis(footAngle, foot.up));
                footAdjustments.Add(new JointAdjustment
                {
                    Joint = HumanBodyBones.LeftFoot,
                    RotationTweaks = new[]
                    {
                        Quaternion.Euler(adjustmentAlignment * adjustment.eulerAngles)
                    }
                });
            }
            if (animator.GetBoneTransform(HumanBodyBones.RightToes) == null)
            {
                var foot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                // Align the feet in the desired foot direction.
                var footToToes =
                    (restPoseObject.GetBonePoseData(HumanBodyBones.RightToes).WorldPose.position -
                     restPoseObject.GetBonePoseData(HumanBodyBones.RightFoot).WorldPose.position).normalized;
                var footAngle = Vector3.Angle(desiredFootDirection, footToToes);
                // Use the inverse rotation here as the rotation of the feet need to be inverted.
                var adjustment = Quaternion.Inverse(Quaternion.AngleAxis(
                    footAngle * (shouldMirrorLegs ? 1 : -1), foot.up));
                footAdjustments.Add(new JointAdjustment
                {
                    Joint = HumanBodyBones.RightFoot,
                    RotationTweaks = new[]
                    {
                        Quaternion.Euler(adjustmentAlignment * adjustment.eulerAngles)
                    }
                });
            }
            return footAdjustments.ToArray();
        }

        /// <summary>
        /// Add the blend hand retargeting processor.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer to add to.</param>
        /// <param name="handedness">The handedness of the processor.</param>
        public static void AddBlendHandRetargetingProcessor(
            RetargetingLayer retargetingLayer,
            Handedness handedness)
        {
            bool needCorrectHand = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var blendHandProcessor = processor as RetargetingBlendHandProcessor;
                if (blendHandProcessor != null)
                {
                    if (blendHandProcessor.GetHandedness() == handedness)
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

        /// <summary>
        /// Destroys legacy components on GameObject if they exist.
        /// </summary>
        /// <param name="gameObject">Main GameObject.</param>
        /// <param name="rigObject">Rig object.</param>
        public static void DestroyLegacyComponents(GameObject gameObject, GameObject rigObject)
        {
            HelperMenusCommon.DestroyTargetTransform(rigObject, "LeftHandTarget", true);
            HelperMenusCommon.DestroyTargetTransform(rigObject, "RightHandTarget", true);
            HelperMenusCommon.DestroyTargetTransform(rigObject, "LeftElbowTarget", true);
            HelperMenusCommon.DestroyTargetTransform(rigObject, "RightElbowTarget", true);
            HelperMenusCommon.DestroyTargetTransform(rigObject, "LeftArmIK", false);
            HelperMenusCommon.DestroyTargetTransform(rigObject, "RightArmIK", false);
            HelperMenusCommon.DestroyLegacyComponents<BlendHandConstraints>(gameObject);
            HelperMenusCommon.DestroyLegacyComponents<RetargetedBoneTargets>(gameObject);
            HelperMenusCommon.DestroyLegacyComponents<AnimationRigSetup>(gameObject);
            HelperMenusCommon.DestroyLegacyComponents<FullBodyHandDeformation>(gameObject);
        }

        /// <summary>
        /// Destroy transform based on name, if any exists. The child can be found recursively.
        /// </summary>
        /// <param name="mainParent">Parent of target transform.</param>
        /// <param name="nameOfTarget">Name of target.</param>
        /// <param name="recursiveSearch">Indicates if recursion should be used or not.</param>
        /// <returns>True if destroyed; false if not.</returns>
        public static bool DestroyTargetTransform(GameObject mainParent, string nameOfTarget,
            bool recursiveSearch)
        {
            Transform targetTransform = recursiveSearch ?
                mainParent.transform.FindChildRecursive(nameOfTarget) :
                mainParent.transform.Find(nameOfTarget);
            if (targetTransform != null)
            {
                Undo.DestroyObjectImmediate(targetTransform);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add the hand deformation retargeting processor.
        /// </summary>
        /// <param name="retargetingLayer"></param>
        public static void AddHandDeformationRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            bool needHandDeformation = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var handDeformationProcessor = processor as RetargetingHandDeformationProcessor;
                if (handDeformationProcessor != null)
                {
                    needHandDeformation = false;
                    break;
                }
            }

            if (!needHandDeformation)
            {
                return;
            }

            var handDeformation = ScriptableObject.CreateInstance<RetargetingHandDeformationProcessor>();
            Undo.RegisterCreatedObjectUndo(handDeformation, "Create hand deformation.");
            Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            handDeformation.name = "HandDeformation";
            handDeformation.CalculateFingerData(retargetingLayer.GetComponent<Animator>());
            retargetingLayer.AddRetargetingProcessor(handDeformation);
        }

        /// <summary>
        /// Setup external bone targets.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer to add to.</param>
        /// <param name="isFullBody">True if full body.</param>
        /// <param name="boneTargets">The array of bone targets to setup.</param>
        public static void SetupExternalBoneTargets(RetargetingLayer retargetingLayer, bool isFullBody,
            BoneTarget[] boneTargets)
        {
            var externalBoneTargets = new ExternalBoneTargets
            {
                Enabled = true,
                FullBody = isFullBody,
                BoneTargetsArray = boneTargets
            };
            retargetingLayer.ExternalBoneTargetsInst = externalBoneTargets;
        }

        /// <summary>
        /// Add the retargeting animation rig.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="rigBuilder">The rig builder.</param>
        /// <param name="constraintComponents">The constraint components.</param>
        public static void AddRetargetingAnimationRig(
            RetargetingLayer retargetingLayer, RigBuilder rigBuilder, MonoBehaviour[] constraintComponents)
        {
            var retargetingAnimationRig = new RetargetingAnimationRig
            {
                RigBuilderComp = rigBuilder
            };
            retargetingAnimationRig.RebindAnimator = true;
            retargetingAnimationRig.ReEnableRig = true;
            retargetingAnimationRig.RigToggleOnFocus = false;
            retargetingLayer.RetargetingAnimationRigInst = retargetingAnimationRig;
            retargetingAnimationRig.OVRSkeletonConstraintComps = constraintComponents;
        }

        /// <summary>
        /// Destroy legacy components.
        /// </summary>
        /// <param name="gameObject">The object to check for the legacy components.</param>
        /// <typeparam name="T">The legacy component type.</typeparam>
        /// <returns>True if legacy components were destroyed.</returns>
        public static bool DestroyLegacyComponents<T>(GameObject gameObject)
        {
            var componentsFound = gameObject.GetComponents<T>();

            foreach (var componentFound in componentsFound)
            {
                Undo.DestroyObjectImmediate(componentFound as UnityEngine.Object);
            }

            return componentsFound.Length > 0;
        }

        /// <summary>
        /// Adds retargeting processor that corrects hand bones.
        /// </summary>
        /// <param name="retargetingLayer">Retargeting layer to add to.</param>
        public static void AddCorrectBonesRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            bool needCorrectBones = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                if (processor as RetargetingProcessorCorrectBones != null)
                {
                    needCorrectBones = false;
                }
            }

            if (!needCorrectBones)
            {
                return;
            }

            var retargetingProcessorCorrectBones = ScriptableObject.CreateInstance<RetargetingProcessorCorrectBones>();
            Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectBones, "Create correct bones retargeting processor.");
            Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            retargetingProcessorCorrectBones.name = "CorrectBones";
            retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectBones);
        }

        /// <summary>
        /// Add hand correction retargeting processor component to retargeting layer.
        /// </summary>
        /// <param name="retargetingLayer">Retargeting layer to add to.</param>
        /// <param name="handedness">Handedness of hand processor.</param>
        public static void AddCorrectHandRetargetingProcessor(
            RetargetingLayer retargetingLayer,
            Handedness handedness)
        {
            bool needCorrectHand = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var correctHand = processor as RetargetingProcessorCorrectHand;
                if (correctHand != null)
                {
                    needCorrectHand = false;
                }
            }

            if (!needCorrectHand)
            {
                return;
            }

            var retargetingProcessorCorrectHand = ScriptableObject.CreateInstance<RetargetingProcessorCorrectHand>();
            var handednessString = handedness == Handedness.Left ?
                _LEFT_HANDEDNESS_STRING : _RIGHT_HANDEDNESS_STRING;
            Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectHand, $"Create correct hand ({handednessString}) retargeting processor.");
            Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            retargetingProcessorCorrectHand.HandIKType = RetargetingProcessorCorrectHand.IKType.CCDIK;
            retargetingProcessorCorrectHand.name = $"Correct{handednessString}Hand";
            retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectHand);
        }

        /// <summary>
        /// Checks to see if the GameObject can be used for animation rigging or not.
        /// </summary>
        /// <param name="gameObject">GameObject to check.</param>
        /// <exception cref="InvalidOperationException">Exception thrown if check fails.</exception>
        public static void ValidGameObjectForAnimationRigging(GameObject gameObject)
        {
            var animatorComp = gameObject.GetComponent<Animator>();
            if (animatorComp == null || animatorComp.avatar == null
                || !animatorComp.avatar.isHuman)
            {
                throw new InvalidOperationException(
                    $"Animation Rigging requires an {nameof(Animator)} " +
                    $"component with a Humanoid avatar.");
            }
        }
    }
}
