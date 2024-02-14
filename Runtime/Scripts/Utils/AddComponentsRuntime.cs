// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using Oculus.Movement.Tracking;
using System.Reflection;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using static Oculus.Movement.AnimationRigging.RetargetedBoneTargets;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allow adding components at runtime.
    /// </summary>
    public class AddComponentsRuntime
    {
        /// <summary>
        /// Sets up character for retargeting.
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
        /// Allows adding correctives face tracking to a character at runtime.
        /// </summary>
        /// <param name="selectedGameObject">GameObject to add correctives face tracking to.</param>
        /// <param name="allowDuplicates">Whether or not to allow duplicate mapping.</param>
        public static void SetupCharacterForCorrectivesFace(
            GameObject selectedGameObject,
            bool allowDuplicates)
        {
            try
            {
                ValidateGameObjectForFaceMapping(selectedGameObject);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"Face Tracking setup error: {e.Message}.");
                return;
            }

            var faceExpressions = selectedGameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = selectedGameObject.AddComponent<OVRFaceExpressions>();
            }

            var face = selectedGameObject.GetComponent<CorrectivesFace>();
            if (!face)
            {
                face = selectedGameObject.AddComponent<CorrectivesFace>();
                face.FaceExpressions = faceExpressions;
            }

            face.RetargetingTypeField = OVRCustomFace.RetargetingType.OculusFace;
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
        }

        /// <summary>
        /// Allows adding correctives face tracking to a character at runtime.
        /// </summary>
        /// <param name="selectedGameObject">GameObject to add correctives face tracking to.</param>
        /// <param name="allowDuplicates">Whether or not to allow duplicate mapping.</param>
        public static void SetupCharacterForARKitFace(
            GameObject selectedGameObject,
            bool allowDuplicates)
        {
            try
            {
                ValidateGameObjectForFaceMapping(selectedGameObject);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"Face Tracking setup error: {e.Message}.");
                return;
            }

            var faceExpressions = selectedGameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = selectedGameObject.AddComponent<OVRFaceExpressions>();
            }

            var face = selectedGameObject.GetComponent<ARKitFace>();
            if (!face)
            {
                face = selectedGameObject.AddComponent<ARKitFace>();
                face.FaceExpressions = faceExpressions;
            }

            face.RetargetingTypeField = OVRCustomFace.RetargetingType.Custom;
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
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
                ValidateGameObjectForAnimationRigging(selectedGameObject);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"Retargeting setup error: {e.Message}.");
                return;
            }
            // Disable the character, add components, THEN enable it.
            // Animationg rigging doesn't start properly otherwise.
            selectedGameObject.SetActive(false);
            var mainParent = selectedGameObject;

            // Add the retargeting and body tracking components at root first.
            RetargetingLayer retargetingLayer = AddMainRetargetingComponents(mainParent, isFullBody);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) = AddBasicAnimationRiggingComponents(mainParent);
            // disable rig builder. in case we need to set up any constraints, we might need to enable
            // the animator. but we don't want the rig to evaluate any constraints, so keep the rig disabled
            // until the character has been set up.
            rigBuilder.enabled = false;

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            Animator animatorComp = selectedGameObject.GetComponent<Animator>();

            // Body deformation.
            if (addConstraints)
            {
                if (isFullBody)
                {
                    RetargetedBoneTarget[] spineBoneTargets = AddSpineBoneTargets(rigObject, animatorComp);
                    FullBodyDeformationConstraint deformationConstraint =
                        AddFullBodyDeformationConstraint(rigObject, animatorComp, spineBoneTargets, restPoseObjectHumanoid);

                    AddRetargetedFullBodyBoneTargetComponent(selectedGameObject, spineBoneTargets);

                    // enable only to get the transform, then disable.
                    // if blendhand constraint is added on a enabled gameobject, errors might be thrown
                    // due to Awake being run too early.
                    animatorComp.gameObject.SetActive(true);
                    var headTransform = animatorComp.GetBoneTransform(HumanBodyBones.Head);
                    animatorComp.gameObject.SetActive(false);
                    AddHandBlendConstraintFullBody(selectedGameObject, null,
                        retargetingLayer, OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                        headTransform);

                    AddHandBlendConstraintFullBody(selectedGameObject, null,
                        retargetingLayer, OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                        headTransform);
                }
                else
                {
                    RetargetedBoneTarget[] spineBoneTargets = AddSpineBoneTargets(rigObject, animatorComp);
                    DeformationConstraint deformationConstraint =
                        AddDeformationConstraint(rigObject, animatorComp, spineBoneTargets);
                    constraintMonos.Add(deformationConstraint);

                    AddRetargetedBoneTargetComponent(selectedGameObject, spineBoneTargets);

                    // enable only to get the transform, then disable.
                    // if blendhand constraint is added on a enabled gameobject, errors might be thrown
                    // due to Awake being run too early.
                    animatorComp.gameObject.SetActive(true);
                    var headTransform = animatorComp.GetBoneTransform(HumanBodyBones.Head);
                    animatorComp.gameObject.SetActive(false);
                    AddHandBlendConstraint(selectedGameObject,
                        retargetingLayer, OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist,
                        headTransform);
                    AddHandBlendConstraint(selectedGameObject,
                        retargetingLayer, OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandWrist,
                        headTransform);
                }
            }

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(mainParent, retargetingLayer, rigBuilder,
                constraintMonos.ToArray(), retargetingLayer);
            AddJointAdjustments(animatorComp, retargetingLayer, restPoseObjectHumanoid);

            // Add retargeting processors to the retargeting layer.
            AddCorrectBonesRetargetingProcessor(retargetingLayer);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);

            if (isFullBody)
            {
                animatorComp.gameObject.SetActive(true);
                var leftHand = animatorComp.GetBoneTransform(HumanBodyBones.LeftHand);
                var rightHand = animatorComp.GetBoneTransform(HumanBodyBones.RightHand);
                animatorComp.gameObject.SetActive(false);
                AddFullBodyHandDeformation(selectedGameObject,
                    animatorComp, retargetingLayer, leftHand,
                    rightHand);
            }

            rigBuilder.enabled = true;
            selectedGameObject.SetActive(true);
        }

        private static RetargetingLayer AddMainRetargetingComponents(
            GameObject mainParent,
            bool isFullBody)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
            }
            retargetingLayer.EnableTrackingByProxy = true;

            var bodySectionToPosition =
                    typeof(OVRUnityHumanoidSkeletonRetargeter).GetField(
                        isFullBody ? "_fullBodySectionToPosition" : "_bodySectionToPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic);

            if (bodySectionToPosition != null)
            {
                if (isFullBody)
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
                        OVRHumanBodyBonesMappings.BodySection.Head,
                        OVRHumanBodyBonesMappings.BodySection.LeftLeg,
                        OVRHumanBodyBonesMappings.BodySection.LeftFoot,
                        OVRHumanBodyBonesMappings.BodySection.RightLeg,
                        OVRHumanBodyBonesMappings.BodySection.RightFoot
                    });
                }
                else
                {
                    bodySectionToPosition.SetValue(retargetingLayer, new[]
                    {
                        OVRHumanBodyBonesMappings.BodySection.LeftArm,
                        OVRHumanBodyBonesMappings.BodySection.RightArm,
                        OVRHumanBodyBonesMappings.BodySection.LeftHand,
                        OVRHumanBodyBonesMappings.BodySection.RightHand,
                        OVRHumanBodyBonesMappings.BodySection.Hips,
                        OVRHumanBodyBonesMappings.BodySection.Neck,
                        OVRHumanBodyBonesMappings.BodySection.Head
                    });
                }
            }

            OVRBody bodyComp = mainParent.GetComponent<OVRBody>();
            if (!bodyComp)
            {
                bodyComp = mainParent.AddComponent<OVRBody>();
            }

            typeof(RetargetingLayer).GetField(
                "_skeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                retargetingLayer, isFullBody ? OVRSkeleton.SkeletonType.FullBody : OVRSkeleton.SkeletonType.Body);
            typeof(OVRBody).GetField(
                "_providedSkeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                bodyComp, isFullBody ? OVRPlugin.BodyJointSet.FullBody : OVRPlugin.BodyJointSet.UpperBody);

            return retargetingLayer;
        }

        private static (RigBuilder, GameObject) AddBasicAnimationRiggingComponents(GameObject mainParent)
        {
            Rig rigComponent = mainParent.GetComponentInChildren<Rig>();
            if (!rigComponent)
            {
                // Create rig for constraints.
                GameObject rigObject = new GameObject("Rig");
                rigComponent = rigObject.AddComponent<Rig>();
                rigComponent.weight = 1.0f;
            }

            RigBuilder rigBuilder = mainParent.GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                rigBuilder = mainParent.AddComponent<RigBuilder>();
                rigBuilder.layers = new List<RigLayer>
                {
                    new RigLayer(rigComponent, true)
                };
            }

            rigComponent.transform.SetParent(mainParent.transform, true);
            rigComponent.transform.localPosition = Vector3.zero;
            rigComponent.transform.localRotation = Quaternion.identity;
            rigComponent.transform.localScale = Vector3.one;

            return (rigBuilder, rigComponent.gameObject);
        }

        private static RetargetingAnimationConstraint AddRetargetingConstraint(
            GameObject rigObject, RetargetingLayer retargetingLayer)
        {
            RetargetingAnimationConstraint retargetConstraint =
                rigObject.GetComponentInChildren<RetargetingAnimationConstraint>();
            if (retargetConstraint == null)
            {
                GameObject retargetingAnimConstraintObj =
                    new GameObject("RetargetingConstraint");
                retargetingAnimConstraintObj.SetActive(false);
                retargetConstraint =
                    retargetingAnimConstraintObj.AddComponent<RetargetingAnimationConstraint>();
                retargetConstraint.RetargetingLayerComp = retargetingLayer;
                retargetConstraint.data.AllowDynamicAdjustmentsRuntime = true;
                retargetingAnimConstraintObj.SetActive(true);

                retargetConstraint.transform.SetParent(rigObject.transform, true);
                retargetConstraint.transform.SetAsLastSibling();
                retargetConstraint.transform.localPosition = Vector3.zero;
                retargetConstraint.transform.localRotation = Quaternion.identity;
                retargetConstraint.transform.localScale = Vector3.one;

                // keep retargeter disabled until it initializes properly
                retargetConstraint.gameObject.SetActive(false);
            }
            return retargetConstraint;
        }

        private static RetargetedBoneTarget[] AddSpineBoneTargets(GameObject rigObject,
            Animator animator)
        {
            var boneTargets = new List<RetargetedBoneTarget>();
            Transform hipsTarget = AddSpineTarget(rigObject, "HipsTarget",
                animator.GetBoneTransform(HumanBodyBones.Hips));
            Transform spineLowerTarget = AddSpineTarget(rigObject, "SpineLowerTarget",
                animator.GetBoneTransform(HumanBodyBones.Spine));
            Transform spineUpperTarget = AddSpineTarget(rigObject, "SpineUpperTarget",
                animator.GetBoneTransform(HumanBodyBones.Chest));
            Transform chestTarget = AddSpineTarget(rigObject, "ChestTarget",
                animator.GetBoneTransform(HumanBodyBones.UpperChest));
            Transform neckTarget = AddSpineTarget(rigObject, "NeckTarget",
                animator.GetBoneTransform(HumanBodyBones.Neck));
            Transform headTarget = AddSpineTarget(rigObject, "HeadTarget",
                animator.GetBoneTransform(HumanBodyBones.Head));

            Tuple<OVRSkeleton.BoneId, Transform>[] bonesToRetarget =
            {
                new(OVRSkeleton.BoneId.Body_Hips, hipsTarget),
                new(OVRSkeleton.BoneId.Body_SpineLower, spineLowerTarget),
                new(OVRSkeleton.BoneId.Body_SpineUpper, spineUpperTarget),
                new(OVRSkeleton.BoneId.Body_Chest, chestTarget),
                new(OVRSkeleton.BoneId.Body_Neck, neckTarget),
                new(OVRSkeleton.BoneId.Body_Head, headTarget),
            };

            foreach (var boneToRetarget in bonesToRetarget)
            {
                RetargetedBoneTarget boneRTTarget = new RetargetedBoneTarget();
                boneRTTarget.BoneId = boneToRetarget.Item1;
                boneRTTarget.Target = boneToRetarget.Item2;
                boneRTTarget.HumanBodyBone = OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone[boneRTTarget.BoneId];
                boneTargets.Add(boneRTTarget);
            }
            return boneTargets.ToArray();
        }

        private static Transform AddSpineTarget(GameObject mainParent,
            string nameOfTarget, Transform targetTransform = null)
        {
            Transform spineTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (spineTarget == null)
            {
                GameObject spineTargetObject =
                    new GameObject(nameOfTarget);
                spineTargetObject.transform.SetParent(mainParent.transform, true);
                spineTarget = spineTargetObject.transform;
            }

            if (targetTransform != null)
            {
                spineTarget.position = targetTransform.position;
                spineTarget.rotation = targetTransform.rotation;
                spineTarget.localScale = targetTransform.localScale;
            }
            else
            {
                spineTarget.localPosition = Vector3.zero;
                spineTarget.localRotation = Quaternion.identity;
                spineTarget.localScale = Vector3.one;
            }
            return spineTarget;
        }

        private static DeformationConstraint AddDeformationConstraint(
            GameObject rigObject, Animator animator, RetargetedBoneTarget[] spineBoneTargets)
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
            GameObject rigObject, Animator animator, RetargetedBoneTarget[] spineBoneTargets,
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
            deformationConstraint.data.LeftLegWeight = 1.0f;
            deformationConstraint.data.RightLegWeight = 1.0f;
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

        private static RetargetedBoneTargets AddRetargetedBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            RetargetedBoneTargets retargetedBoneTargets =
                mainParent.AddComponent<RetargetedBoneTargets>();

            retargetedBoneTargets.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
            retargetedBoneTargets.RetargetedBoneTargetsArray = boneTargetsArray;
            return retargetedBoneTargets;
        }

        private static FullBodyRetargetedBoneTargets AddRetargetedFullBodyBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            FullBodyRetargetedBoneTargets retargetedBoneTargets =
                mainParent.AddComponent<FullBodyRetargetedBoneTargets>();

            retargetedBoneTargets.AutoAdd = mainParent.GetComponent<RetargetingLayer>();
            retargetedBoneTargets.RetargetedBoneTargets = boneTargetsArray;

            return retargetedBoneTargets;
        }

        private static FullBodyRetargetedBoneTargets AddFullBodyRetargetedBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            FullBodyRetargetedBoneTargets retargetedBoneTargets =
                mainParent.AddComponent<FullBodyRetargetedBoneTargets>();

            retargetedBoneTargets.AutoAdd = mainParent.GetComponent<RetargetingLayer>();
            retargetedBoneTargets.RetargetedBoneTargets = boneTargetsArray;

            return retargetedBoneTargets;
        }

        private static BlendHandConstraints AddHandBlendConstraint(
            GameObject mainParent, RetargetingLayer retargetingLayer,
            OVRHumanBodyBonesMappings.BodyTrackingBoneId boneIdToTest, Transform headTransform)
        {
            BlendHandConstraints blendConstraint =
                mainParent.AddComponent<BlendHandConstraints>();

            blendConstraint.Constraints = null;
            blendConstraint.RetargetingLayerComp = retargetingLayer;
            blendConstraint.BoneIdToTest = boneIdToTest;
            blendConstraint.HeadTransform = headTransform;
            blendConstraint.AutoAddTo = mainParent.GetComponent<MonoBehaviour>();
            blendConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

            return blendConstraint;
        }

        private static BlendHandConstraintsFullBody AddHandBlendConstraintFullBody(
            GameObject mainParent, MonoBehaviour[] constraints, RetargetingLayer retargetingLayer,
            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId boneIdToTest, Transform headTransform)
        {
            BlendHandConstraintsFullBody blendConstraint =
                mainParent.AddComponent<BlendHandConstraintsFullBody>();

            blendConstraint.Constraints = null;
            blendConstraint.RetargetingLayerComp = retargetingLayer;
            blendConstraint.BoneIdToTest = boneIdToTest;
            blendConstraint.HeadTransform = headTransform;
            blendConstraint.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
            blendConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

            return blendConstraint;
        }

        private static void AddAnimationRiggingLayer(GameObject mainParent,
            OVRSkeleton skeletalComponent, RigBuilder rigBuilder,
            MonoBehaviour[] constraintComponents,
            RetargetingLayer retargetingLayer)
        {
            AnimationRigSetup rigSetup = mainParent.GetComponent<AnimationRigSetup>();
            if (rigSetup)
            {
                return;
            }
            var animatorComponent = mainParent.GetComponent<Animator>();
            rigSetup = mainParent.AddComponent<AnimationRigSetup>();
            rigSetup.Skeleton = skeletalComponent;
            rigSetup.AnimatorComp = animatorComponent;
            rigSetup.RigbuilderComp = rigBuilder;
            if (constraintComponents != null)
            {
                foreach (var constraintComponent in constraintComponents)
                {
                    rigSetup.AddSkeletalConstraint(constraintComponent);
                }
            }

            rigSetup.RebindAnimator = true;
            rigSetup.ReEnableRig = true;
            rigSetup.RetargetingLayerComp = retargetingLayer;
            rigSetup.CheckSkeletalUpdatesByProxy = true;
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
                            RotationTweaks = new [] { rotationTweak }
                        };
                        adjustments.Add(adjustment);
                    }
                    adjustmentsField.SetValue(retargetingLayer, adjustments.ToArray());
                }
                else
                {
                    var hipAngleDifference = restPoseObjectHumanoid.CalculateRotationDifferenceFromRestPoseToAnimatorJoint
                                 (animator, HumanBodyBones.Hips);
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

        private static void AddCorrectBonesRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            bool needCorrectBones = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                if (processor as RetargetingProcessorCorrectBones != null)
                {
                    needCorrectBones = false;
                }
            }

            if (needCorrectBones)
            {
                var retargetingProcessorCorrectBones = ScriptableObject.CreateInstance<RetargetingProcessorCorrectBones>();
                retargetingProcessorCorrectBones.name = "CorrectBones";
                retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectBones);
            }
        }

        private static void AddCorrectHandRetargetingProcessor(RetargetingLayer retargetingLayer, Handedness handedness)
        {
            bool needCorrectHand = true;
            foreach (var processor in retargetingLayer.RetargetingProcessors)
            {
                var correctHand = processor as RetargetingProcessorCorrectHand;
                if (correctHand != null)
                {
                    if (correctHand.Handedness == handedness)
                    {
                        needCorrectHand = false;
                    }
                }
            }

            if (needCorrectHand)
            {
                var retargetingProcessorCorrectHand = ScriptableObject.CreateInstance<RetargetingProcessorCorrectHand>();
                var handednessString = handedness == Handedness.Left ? "Left" : "Right";
                retargetingProcessorCorrectHand.Handedness = handedness;
                retargetingProcessorCorrectHand.HandIKType = RetargetingProcessorCorrectHand.IKType.CCDIK;
                retargetingProcessorCorrectHand.name = $"Correct{handednessString}Hand";
                retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectHand);
            }
        }

        private static void AddFullBodyHandDeformation(GameObject mainParent,
            Animator animatorComp, OVRSkeleton skeletalComponent, Transform leftHand,
            Transform rightHand)
        {
            FullBodyHandDeformation fullBodyHandDeformation =
                mainParent.AddComponent<FullBodyHandDeformation>();

            fullBodyHandDeformation.AnimatorComp = animatorComp;
            fullBodyHandDeformation.Skeleton = skeletalComponent;
            fullBodyHandDeformation.LeftHand = leftHand;
            fullBodyHandDeformation.RightHand = rightHand;
            fullBodyHandDeformation.FingerOffsets = new FullBodyHandDeformation.FingerOffset[0];
            // enable animator to be able to calculate finger data
            animatorComp.gameObject.SetActive(true);
            fullBodyHandDeformation.CalculateFingerData();
            animatorComp.gameObject.SetActive(false);
        }

        private static void ValidateGameObjectForAnimationRigging(GameObject go)
        {
            var animatorComp = go.GetComponent<Animator>();
            if (animatorComp == null || animatorComp.avatar == null
                || !animatorComp.avatar.isHuman)
            {
                throw new InvalidOperationException(
                    $"Animation Rigging requires an {nameof(Animator)} " +
                    $"component with a Humanoid avatar.");
            }
        }

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
