// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.AnimationRigging.Deprecated;
using Oculus.Movement.Tracking;
using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;
using static Oculus.Movement.AnimationRigging.ExternalBoneTargets;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Has functions that allow adding components via the editor or runtime.
    /// These functions detect if they are being called in the editor and if so,
    /// affect the undo state.
    /// </summary>
    public class AddComponentsHelper
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
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets(poseAssetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {poseAssetName} cannot be found.");
                return null;
            }
            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RestPoseObjectHumanoid>(pathToAsset);
#else
            return null;
#endif
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
                var fullBodyDeformationConstraint =
                    retargetingLayer.GetComponentInChildren<FullBodyDeformationConstraint>(true);
                adjustmentsField.SetValue(retargetingLayer,
                    DeformationUtilities.GetJointAdjustments(
                        animator, restPoseObject, fullBodyDeformationConstraint));
            }
        }

        /// <summary>
        /// Adds components necessary for animation rigging to work.
        /// </summary>
        /// <param name="mainParent">Container for animation rigging components.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <returns>The rig builder and rig components created.</returns>
        public static (RigBuilder, GameObject) AddBasicAnimationRiggingComponents(
            GameObject mainParent,
            bool runtimeInvocation = false)
        {
            Rig rigComponent = mainParent.GetComponentInChildren<Rig>();
            if (!rigComponent)
            {
                // Create rig for constraints.
                GameObject rigObject = new GameObject("Rig");
                rigComponent = rigObject.AddComponent<Rig>();
                rigComponent.weight = 1.0f;
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(rigObject, "Create Rig");
                }
#endif
            }

            RigBuilder rigBuilder = mainParent.GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                rigBuilder = mainParent.AddComponent<RigBuilder>();
                rigBuilder.layers = new List<RigLayer>
                {
                    new RigLayer(rigComponent, true)
                };
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(rigBuilder, "Create RigBuilder");
                }
#endif
            }

#if UNITY_EDITOR
            if (runtimeInvocation)
            {
                rigComponent.transform.SetParent(mainParent.transform, true);
            }
            else
            {
                Undo.SetTransformParent(rigComponent.transform, mainParent.transform, "Add Rig to Main Parent");
                Undo.RegisterCompleteObjectUndo(rigComponent, "Rig Component Transform init");
            }
#else
            rigComponent.transform.SetParent(mainParent.transform, true);
#endif
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
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <returns>RetargetingLayer that was added.</returns>
        public static RetargetingLayer AddMainRetargetingComponent(
            GameObject mainParent,
            bool isFullBody,
            bool runtimeInvocation = false)
        {
            // Delete retargeting layer so that new one computes offsets.
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (retargetingLayer != null)
            {
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.DestroyObjectImmediate(retargetingLayer);
                }
                else
                {
                    GameObject.DestroyImmediate(retargetingLayer);
                }
#else
                GameObject.DestroyImmediate(retargetingLayer);
#endif
            }

            retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterCreatedObjectUndo(retargetingLayer, "Add Retargeting Layer");
            }
#endif

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
            else
            {
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
            }

#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                EditorUtility.SetDirty(retargetingLayer);
            }
#endif

            OVRBody bodyComp = mainParent.GetComponent<OVRBody>();
            if (!bodyComp)
            {
                bodyComp = mainParent.AddComponent<OVRBody>();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(bodyComp, "Add OVRBody component");
                }
#endif
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
            retargetingLayer.UpdateBonePairMappings();
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(retargetingLayer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(bodyComp);
            }
#endif
            return retargetingLayer;
        }

        /// <summary>
        /// Adds bone targets to rig object.
        /// </summary>
        /// <param name="rigObject">Rig object serving as parent for bone targets.</param>
        /// <param name="animator">Animator to obtain target bone.</param>
        /// <param name="isFullBody">Indicates if full body (or not).</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <returns>Array of bone targets added.</returns>
        public static BoneTarget[] AddBoneTargets(
            GameObject rigObject,
            Animator animator,
            bool isFullBody,
            bool runtimeInvocation)
        {
            var boneTargets = new List<BoneTarget>();
            Transform hipsTarget = AddBoneTarget(rigObject, "HipsTarget",
                animator.GetBoneTransform(HumanBodyBones.Hips), runtimeInvocation);
            Transform spineLowerTarget = AddBoneTarget(rigObject, "SpineLowerTarget",
                animator.GetBoneTransform(HumanBodyBones.Spine), runtimeInvocation);
            Transform spineUpperTarget = AddBoneTarget(rigObject, "SpineUpperTarget",
                animator.GetBoneTransform(HumanBodyBones.Chest), runtimeInvocation);
            Transform chestTarget = AddBoneTarget(rigObject, "ChestTarget",
                animator.GetBoneTransform(HumanBodyBones.UpperChest), runtimeInvocation);
            Transform neckTarget = AddBoneTarget(rigObject, "NeckTarget",
                animator.GetBoneTransform(HumanBodyBones.Neck), runtimeInvocation);
            Transform headTarget = AddBoneTarget(rigObject, "HeadTarget",
                animator.GetBoneTransform(HumanBodyBones.Head), runtimeInvocation);

            Tuple<OVRSkeleton.BoneId, Transform>[] bonesToRetarget = null;

            if (isFullBody)
            {
                Transform leftFootTarget = AddBoneTarget(rigObject, "LeftFootTarget",
                    animator.GetBoneTransform(HumanBodyBones.LeftFoot), runtimeInvocation);
                Transform leftToesTarget = AddBoneTarget(rigObject, "LeftToesTarget",
                    animator.GetBoneTransform(HumanBodyBones.LeftToes), runtimeInvocation);
                Transform rightFootTarget = AddBoneTarget(rigObject, "RightFootTarget",
                    animator.GetBoneTransform(HumanBodyBones.RightFoot), runtimeInvocation);
                Transform rightToesTarget = AddBoneTarget(rigObject, "RightToesTarget",
                    animator.GetBoneTransform(HumanBodyBones.RightToes), runtimeInvocation);
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
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <returns>Bone target added.</returns>
        private static Transform AddBoneTarget(
            GameObject mainParent,
            string nameOfTarget,
            Transform targetTransform = null,
            bool runtimeInvocation = false)
        {
            Transform boneTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (boneTarget == null)
            {
                GameObject boneTargetObject =
                    new GameObject(nameOfTarget);
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    boneTargetObject.transform.SetParent(mainParent.transform);
                }
                else
                {
                    Undo.RegisterCreatedObjectUndo(boneTargetObject,
                        "Create Bone Target " + nameOfTarget);

                    Undo.SetTransformParent(boneTargetObject.transform,
                        mainParent.transform,
                        $"Add Bone Target {nameOfTarget} To Main Parent");
                    Undo.RegisterCompleteObjectUndo(boneTargetObject,
                        $"Bone Target {nameOfTarget} Transform Init");
                }
#else
                boneTargetObject.transform.SetParent(mainParent.transform);
#endif
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
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <param name="setAsLastSibling">Allows adding as last sibling under constraints.</param>
        /// <returns>Retargeting constraint under the rig.</returns>
        public static RetargetingAnimationConstraint AddRetargetingConstraint(
            GameObject rigObject,
            RetargetingLayer retargetingLayer,
            bool runtimeInvocation = false,
            bool setAsLastSibling = false)
        {
            RetargetingAnimationConstraint retargetConstraint =
                rigObject.GetComponentInChildren<RetargetingAnimationConstraint>(true);
            if (retargetConstraint == null)
            {
                GameObject retargetingAnimConstraintObj =
                    new GameObject("RetargetingConstraint");
                retargetingAnimConstraintObj.SetActive(false);
                retargetConstraint =
                    retargetingAnimConstraintObj.AddComponent<RetargetingAnimationConstraint>();
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    retargetingAnimConstraintObj.transform.SetParent(rigObject.transform);
                }
                else
                {
                    Undo.RegisterCreatedObjectUndo(retargetingAnimConstraintObj, "Create Retargeting Constraint");
                    Undo.SetTransformParent(retargetingAnimConstraintObj.transform, rigObject.transform, "Add Retarget Constraint to Rig");
                    Undo.RegisterCompleteObjectUndo(retargetingAnimConstraintObj, "Retarget Constraint Transform Init");
                }
#else
                retargetingAnimConstraintObj.transform.SetParent(rigObject.transform);
#endif
                if (setAsLastSibling)
                {
                    retargetConstraint.transform.SetAsLastSibling();
                }
                retargetConstraint.transform.localPosition = Vector3.zero;
                retargetConstraint.transform.localRotation = Quaternion.identity;
                retargetConstraint.transform.localScale = Vector3.one;
            }

            retargetConstraint.RetargetingLayerComp = retargetingLayer;
            retargetConstraint.data.AllowDynamicAdjustmentsRuntime = true;
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(retargetConstraint);
            }
#endif
            return retargetConstraint;
        }

        /// <summary>
        /// Destroys legacy components on GameObject if they exist.
        /// </summary>
        /// <param name="gameObject">Main GameObject.</param>
        /// <param name="rigObject">Rig object.</param>
        public static void DestroyLegacyComponents(GameObject gameObject, GameObject rigObject)
        {
            DestroyTargetTransform(rigObject, "LeftHandTarget", true);
            DestroyTargetTransform(rigObject, "RightHandTarget", true);
            DestroyTargetTransform(rigObject, "LeftElbowTarget", true);
            DestroyTargetTransform(rigObject, "RightElbowTarget", true);
            DestroyTargetTransform(rigObject, "LeftArmIK", false);
            DestroyTargetTransform(rigObject, "RightArmIK", false);
            DestroyLegacyComponents<BlendHandConstraints>(gameObject);
            DestroyLegacyComponents<BlendHandConstraintsFullBody>(gameObject);
            DestroyLegacyComponents<RetargetedBoneTargets>(gameObject);
            DestroyLegacyComponents<AnimationRigSetup>(gameObject);
            DestroyLegacyComponents<FullBodyHandDeformation>(gameObject);
            DestroyLegacyProcessor<RetargetingHandDeformationProcessor>(gameObject);
            DestroyLegacyProcessor<RetargetingBlendHandProcessor>(gameObject);
        }

        /// <summary>
        /// Destroy transform based on name, if any exists. The child can be found recursively.
        /// </summary>
        /// <param name="mainParent">Parent of target transform.</param>
        /// <param name="nameOfTarget">Name of target.</param>
        /// <param name="recursiveSearch">Indicates if recursion should be used or not.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <returns>True if destroyed; false if not.</returns>
        public static bool DestroyTargetTransform(
            GameObject mainParent,
            string nameOfTarget,
            bool recursiveSearch,
            bool runtimeInvocation = false)
        {
            Transform targetTransform = recursiveSearch ?
                mainParent.transform.FindChildRecursive(nameOfTarget) :
                mainParent.transform.Find(nameOfTarget);
            if (targetTransform != null)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    GameObject.DestroyImmediate(targetTransform);
                }
                else
                {
                    Undo.DestroyObjectImmediate(targetTransform);
                }
#else
                GameObject.DestroyImmediate(targetTransform);
#endif
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add the hand deformation retargeting processor.
        /// </summary>
        /// <param name="retargetingLayer"></param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void AddHandDeformationRetargetingProcessor(
            RetargetingLayer retargetingLayer,
            bool runtimeInvocation = false)
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
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterCreatedObjectUndo(handDeformation, "Create hand deformation.");
                Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            }
#endif
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
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <typeparam name="T">The legacy component type.</typeparam>
        /// <returns>True if legacy components were destroyed.</returns>
        public static bool DestroyLegacyComponents<T>(GameObject gameObject, bool runtimeInvocation = false)
        {
            var componentsFound = gameObject.GetComponents<T>();

            foreach (var componentFound in componentsFound)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    GameObject.DestroyImmediate(componentFound as Object);
                }
                else
                {
                    Undo.DestroyObjectImmediate(componentFound as Object);
                }
#else
                GameObject.DestroyImmediate(componentFound as Object);
#endif
            }

            return componentsFound.Length > 0;
        }

        /// <summary>
        /// Destroy legacy processors.
        /// </summary>
        /// <param name="gameObject">The object to check for the legacy processors.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        /// <typeparam name="T">The legacy processor type.</typeparam>
        /// <returns>True if legacy processors were destroyed.</returns>
        public static bool DestroyLegacyProcessor<T>(GameObject gameObject, bool runtimeInvocation = false) where T : class
        {
            var retargetingLayer = gameObject.GetComponent<RetargetingLayer>();
            var processors = retargetingLayer.RetargetingProcessors;
            List<RetargetingProcessor> processorsFound = new List<RetargetingProcessor>();
            for (int i = 0; i < processors.Count; i++)
            {
                var processor = processors[i];
                if (processors[i] is T)
                {
                    processorsFound.Add(processor);
                }
            }

            foreach (var processor in processorsFound)
            {
                retargetingLayer.RetargetingProcessors.Remove(processor);
            }

            return processorsFound.Count > 0;
        }

        /// <summary>
        /// Adds retargeting processor that corrects hand bones.
        /// </summary>
        /// <param name="retargetingLayer">Retargeting layer to add to.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void AddCorrectBonesRetargetingProcessor(RetargetingLayer retargetingLayer,
            bool runtimeInvocation = false)
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
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectBones, "Create correct bones retargeting processor.");
                Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            }
#endif
            retargetingProcessorCorrectBones.name = "CorrectBones";
            retargetingProcessorCorrectBones.FingerPositionCorrectionWeight = 0.0f;
            retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectBones);
        }

        /// <summary>
        /// Add hand correction retargeting processor component to retargeting layer.
        /// </summary>
        /// <param name="retargetingLayer">Retargeting layer to add to.</param>
        /// <param name="handedness">Handedness of hand processor.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void AddCorrectHandRetargetingProcessor(
            RetargetingLayer retargetingLayer,
            bool runtimeInvocation = false)
        {
            RetargetingProcessorCorrectHand correctHandProcessor = null;
            int correctHandProcessorCount = 0;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var correctHand = processor as RetargetingProcessorCorrectHand;
                if (correctHand != null)
                {
                    correctHandProcessorCount++;
                    correctHandProcessor = correctHand;
                }
            }

            // If there is more than 1 correct hand processor, remove them.
            if (correctHandProcessorCount > 1)
            {
                DestroyLegacyProcessor<RetargetingProcessorCorrectHand>(retargetingLayer.gameObject);
                correctHandProcessorCount = 0;
            }
            if (correctHandProcessorCount > 0)
            {
                if (correctHandProcessor != null)
                {
                    correctHandProcessor.LeftHandProcessor.FullBodySecondBoneIdToTest =
                        OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftArmLower;
                    correctHandProcessor.LeftHandProcessor.FullBodyBoneIdToTest =
                        OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist;
                    correctHandProcessor.LeftHandProcessor.BoneIdToTest =
                        OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist;

                    correctHandProcessor.RightHandProcessor.FullBodySecondBoneIdToTest =
                        OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightArmLower;
                    correctHandProcessor.RightHandProcessor.FullBodyBoneIdToTest =
                        OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist;
                    correctHandProcessor.RightHandProcessor.BoneIdToTest =
                        OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandWrist;
                }
                return;
            }

            var retargetingProcessorCorrectHand = ScriptableObject.CreateInstance<RetargetingProcessorCorrectHand>();
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectHand, $"Create correct hand retargeting processor.");
                Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
            }
#endif
            bool isFullBody = retargetingLayer.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;
            retargetingProcessorCorrectHand.HandIKType = RetargetingProcessorCorrectHand.IKType.CCDIK;
            retargetingProcessorCorrectHand.name = "CorrectHand";
            retargetingProcessorCorrectHand.LeftHandProcessor = new RetargetingProcessorCorrectHand.HandProcessor();
            retargetingProcessorCorrectHand.RightHandProcessor = new RetargetingProcessorCorrectHand.HandProcessor();
            retargetingProcessorCorrectHand.LeftHandProcessor.FullBodySecondBoneIdToTest =
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftArmLower;
            retargetingProcessorCorrectHand.LeftHandProcessor.FullBodyBoneIdToTest =
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist;
            retargetingProcessorCorrectHand.LeftHandProcessor.BoneIdToTest =
                OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist;
            retargetingProcessorCorrectHand.RightHandProcessor.FullBodySecondBoneIdToTest =
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightArmLower;
            retargetingProcessorCorrectHand.RightHandProcessor.FullBodyBoneIdToTest =
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist;
            retargetingProcessorCorrectHand.RightHandProcessor.BoneIdToTest =
                OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandWrist;
            retargetingProcessorCorrectHand.BlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            retargetingProcessorCorrectHand.IsFullBody = isFullBody;

            retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectHand);
        }

        /// <summary>
        /// Sets up character for correctives face tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to add to.</param>
        /// <param name="allowDuplicates">Allow duplicates mapping or not.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void SetUpCharacterForCorrectivesFace(GameObject gameObject,
            bool allowDuplicates = true,
            bool runtimeInvocation = false)
        {
            try
            {
                AddComponentsHelper.ValidateGameObjectForFaceMapping(gameObject);
            }
            catch (InvalidOperationException e)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    Debug.LogWarning($"Face tracking setup error: {e.Message}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                }
#else
                Debug.LogWarning($"Face tracking setup error: {e.Message}");
#endif
                return;
            }

#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.IncrementCurrentGroup();
            }
#endif

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();

#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
                }
#endif
            }

            var face = gameObject.GetComponent<CorrectivesFace>();
            if (!face)
            {
                face = gameObject.AddComponent<CorrectivesFace>();
                face.FaceExpressions = faceExpressions;

#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(face, "Create CorrectivesFace component");
                }
#endif
            }

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(face, "Assign to BlendshapeModifier field");
                }
#endif
            }

#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map Correctives blendshapes");
            }
#endif
            face.RetargetingTypeField = OVRCustomFace.RetargetingType.OculusFace;
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                EditorUtility.SetDirty(face);
                EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

                Undo.SetCurrentGroupName($"Setup Character for Correctives Tracking");
            }
#endif
        }

        /// <summary>
        /// Sets up character for ARKit face tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to add to.</param>
        /// <param name="allowDuplicates">Allow duplicates mapping or not.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void SetUpCharacterForARKitFace(GameObject gameObject,
            bool allowDuplicates = true,
            bool runtimeInvocation = false)
        {
            try
            {
                AddComponentsHelper.ValidateGameObjectForFaceMapping(gameObject);
            }
            catch (InvalidOperationException e)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    Debug.LogWarning($"Face tracking setup error: {e.Message}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                }
#else
                Debug.LogWarning($"Face tracking setup error: {e.Message}");
#endif
                return;
            }

#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.IncrementCurrentGroup();
            }
#endif

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
                }
#endif
            }

            var face = gameObject.GetComponent<ARKitFace>();
            if (!face)
            {
                face = gameObject.AddComponent<ARKitFace>();
                face.FaceExpressions = faceExpressions;
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(face, "Create ARKit component");
                }
#endif
            }

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(face, "Assign to BlendshapeModifier field");
                }
#endif
            }

#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map ARKit blendshapes");
            }
#endif

            face.RetargetingTypeField = OVRCustomFace.RetargetingType.Custom;
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                EditorUtility.SetDirty(face);
                EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

                Undo.SetCurrentGroupName($"Setup Character for ARKit Tracking");
            }
#endif
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

        /// <summary>
        /// Validates GameObject for face mapping.
        /// </summary>
        /// <param name="go">GameObject to check.</param>
        /// <exception cref="InvalidOperationException">Exception thrown if GameObject fails check.</exception>
        public static void ValidateGameObjectForFaceMapping(GameObject go)
        {
            var renderer = go.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
            {
                throw new InvalidOperationException(
                    $"Adding a Face Tracking component requires a {nameof(SkinnedMeshRenderer)} " +
                    $"that contains blendshapes.");
            }
        }
    }
}
