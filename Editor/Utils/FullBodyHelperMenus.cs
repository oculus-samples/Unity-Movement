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

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Provides useful menus to help one set up tracking technologies
    /// on characters.
    /// </summary>
    internal static class FullBodyHelperMenus
    {
        private const string _MOVEMENT_SAMPLES_MENU =
            "GameObject/Movement Samples/";
        private const string _MOVEMENT_SAMPLES_BT_MENU =
            "Body Tracking/";

        private const string _ANIM_RIGGING_RETARGETING_FULL_BODY_MENU_CONSTRAINTS =
            "Animation Rigging Retargeting (full body) (constraints)";

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_BT_MENU + _ANIM_RIGGING_RETARGETING_FULL_BODY_MENU_CONSTRAINTS)]
        private static void SetupCharacterForAnimationRiggingRetargetingConstraints()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidGameObjectForAnimationRigging(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Retargeting setup error.", e.Message, "Ok");
                return;
            }

            Undo.IncrementCurrentGroup();

            // Add the retargeting and body tracking components at root first.
            Animator animatorComp = activeGameObject.GetComponent<Animator>();
            RetargetingLayer retargetingLayer = AddMainRetargetingComponents(animatorComp, activeGameObject);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) = AddBasicAnimationRiggingComponents(activeGameObject);

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            // Destroy old components
            DestroyBoneTarget(rigObject, "LeftHandTarget");
            DestroyBoneTarget(rigObject, "RightHandTarget");
            DestroyBoneTarget(rigObject, "LeftElbowTarget");
            DestroyBoneTarget(rigObject, "RightElbowTarget");
            DestroyTwoBoneIKConstraint(rigObject, "LeftArmIK");
            DestroyTwoBoneIKConstraint(rigObject, "RightArmIK");
            HelperMenusCommon.DestroyLegacyComponents<BlendHandConstraintsFullBody>(activeGameObject);
            HelperMenusCommon.DestroyLegacyComponents<FullBodyRetargetedBoneTargets>(activeGameObject);
            HelperMenusCommon.DestroyLegacyComponents<AnimationRigSetup>(activeGameObject);

            // Full body deformation.
            BoneTarget[] boneTargets = AddBoneTargets(rigObject, animatorComp);
            FullBodyDeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp, boneTargets);
            constraintMonos.Add(deformationConstraint);

            // Setup retargeted bone targets.
            HelperMenusCommon.SetupExternalBoneTargets(retargetingLayer, true, boneTargets);

            // Disable root motion.
            animatorComp.applyRootMotion = false;
            Debug.Log($"Disabling root motion on the {animatorComp.gameObject.name} animator.");
            EditorUtility.SetDirty(animatorComp);

            // Add retargeting animation rig to tie everything together.
            HelperMenusCommon.AddJointAdjustments(animatorComp, retargetingLayer);
            HelperMenusCommon.AddRetargetingAnimationRig(
                retargetingLayer, rigBuilder, constraintMonos.ToArray());
            EditorUtility.SetDirty(retargetingLayer);

            // Add retargeting processors to the retargeting layer.
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            AddCorrectBonesRetargetingProcessor(retargetingLayer);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);

            // Add full body hand deformation.
            AddFullBodyHandDeformation(activeGameObject, animatorComp, retargetingLayer);

            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
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
                Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectBones, "Create correct bones retargeting processor.");
                Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
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
                Undo.RegisterCreatedObjectUndo(retargetingProcessorCorrectHand, $"Create correct hand ({handednessString}) retargeting processor.");
                Undo.RecordObject(retargetingLayer, "Add retargeting processor to retargeting layer.");
                retargetingProcessorCorrectHand.Handedness = handedness;
                retargetingProcessorCorrectHand.HandIKType = RetargetingProcessorCorrectHand.IKType.CCDIK;
                retargetingProcessorCorrectHand.name = $"Correct{handednessString}Hand";
                retargetingLayer.AddRetargetingProcessor(retargetingProcessorCorrectHand);
            }
        }

        private static BoneTarget[] AddBoneTargets(GameObject rigObject, Animator animator)
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

            Transform leftFootTarget = AddBoneTarget(rigObject, "LeftFootTarget",
                animator.GetBoneTransform(HumanBodyBones.LeftFoot));
            Transform leftToesTarget = AddBoneTarget(rigObject, "LeftToesTarget",
                animator.GetBoneTransform(HumanBodyBones.LeftToes));
            Transform rightFootTarget = AddBoneTarget(rigObject, "RightFootTarget",
                animator.GetBoneTransform(HumanBodyBones.RightFoot));
            Transform rightToesTarget = AddBoneTarget(rigObject, "RightToesTarget",
                animator.GetBoneTransform(HumanBodyBones.RightToes));

            Tuple<OVRSkeleton.BoneId, Transform>[] bonesToRetarget =
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
                new(OVRSkeleton.BoneId.FullBody_RightFootBall, rightToesTarget),
            };

            foreach (var boneToRetarget in bonesToRetarget)
            {
                BoneTarget boneRTTarget = new BoneTarget();
                boneRTTarget.BoneId = boneToRetarget.Item1;
                boneRTTarget.Target = boneToRetarget.Item2;
                boneRTTarget.HumanBodyBone = OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[boneRTTarget.BoneId];
                boneTargets.Add(boneRTTarget);
            }
            return boneTargets.ToArray();
        }

        private static RetargetingLayer AddMainRetargetingComponents(Animator animator, GameObject mainParent)
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
                retargetingLayer, OVRSkeleton.SkeletonType.FullBody);
            typeof(OVRBody).GetField(
                "_providedSkeletonType", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                bodyComp, OVRPlugin.BodyJointSet.FullBody);

            retargetingLayer.EnableTrackingByProxy = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(retargetingLayer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bodyComp);

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
                Undo.RegisterCreatedObjectUndo(rigObject, "Create Rig");
            }

            RigBuilder rigBuilder = mainParent.GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                rigBuilder = mainParent.AddComponent<RigBuilder>();
                rigBuilder.layers = new System.Collections.Generic.List<RigLayer>
                {
                    new RigLayer(rigComponent, true)
                };
                Undo.RegisterCreatedObjectUndo(rigBuilder, "Create RigBuilder");
            }

            Undo.SetTransformParent(rigComponent.transform, mainParent.transform, "Add Rig to Main Parent");
            Undo.RegisterCompleteObjectUndo(rigComponent, "Rig Component Transform init");
            rigComponent.transform.localPosition = Vector3.zero;
            rigComponent.transform.localRotation = Quaternion.identity;
            rigComponent.transform.localScale = Vector3.one;

            return (rigBuilder, rigComponent.gameObject);
        }

        private static RetargetingAnimationConstraint AddRetargetingConstraint(
            GameObject rigObject, RetargetingLayer retargetingLayer)
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

                // keep retargeter disabled until it initializes properly
                retargetConstraint.gameObject.SetActive(false);
            }
            retargetConstraint.RetargetingLayerComp = retargetingLayer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(retargetConstraint);
            return retargetConstraint;
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
            rigSetup = mainParent.AddComponent<AnimationRigSetup>();
            rigSetup.Skeleton = skeletalComponent;
            var animatorComponent = mainParent.GetComponent<Animator>();
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

            Undo.RegisterCreatedObjectUndo(rigSetup, "Create Anim Rig Setup");
        }

        private static FullBodyDeformationConstraint AddDeformationConstraint(
            GameObject rigObject, Animator animator, BoneTarget[] boneTargets)
        {
            FullBodyDeformationConstraint deformationConstraint =
                rigObject.GetComponentInChildren<FullBodyDeformationConstraint>();
            DeformationConstraint baseDeformationConstraint =
                rigObject.GetComponentInChildren<DeformationConstraint>();
            if (deformationConstraint == null && baseDeformationConstraint != null)
            {
                Undo.DestroyObjectImmediate(baseDeformationConstraint.gameObject);
            }
            GameObject deformationConstraintObject = null;
            if (deformationConstraint == null)
            {
                deformationConstraintObject =
                    new GameObject("Deformation");
                deformationConstraint =
                    deformationConstraintObject.AddComponent<FullBodyDeformationConstraint>();

                Undo.RegisterCreatedObjectUndo(deformationConstraintObject, "Create Deformation");

                Undo.SetTransformParent(deformationConstraintObject.transform, rigObject.transform,
                    $"Add Deformation Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(deformationConstraintObject,
                    $"Deformation Constraint Transform Init");
                deformationConstraintObject.transform.localPosition = Vector3.zero;
                deformationConstraintObject.transform.localRotation = Quaternion.identity;
                deformationConstraintObject.transform.localScale = Vector3.one;
            }
            else
            {
                var rig = rigObject.GetComponentInChildren<Rig>();
                if (rig != null)
                {
                    var deformationTransform = rig.transform.FindChildRecursive("Deformation");
                    deformationConstraintObject = deformationTransform.gameObject;
                }
            }
            if (deformationConstraintObject != null)
            {
                foreach (var boneTarget in boneTargets)
                {
                    Undo.SetTransformParent(boneTarget.Target, deformationConstraintObject.transform,
                        $"Parent Bone Target {boneTarget.Target.name} to Deformation.");
                    Undo.RegisterCompleteObjectUndo(boneTarget.Target,
                        $"Bone Target {boneTarget.Target.name} Init");
                }
            }

            deformationConstraint.data.SpineTranslationCorrectionTypeField
                = FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead;
            deformationConstraint.data.SpineLowerAlignmentWeight = 0.5f;
            deformationConstraint.data.SpineUpperAlignmentWeight = 1.0f;
            deformationConstraint.data.ChestAlignmentWeight = 0.0f;
            deformationConstraint.data.LeftShoulderWeight = 1.0f;
            deformationConstraint.data.RightShoulderWeight = 1.0f;
            deformationConstraint.data.LeftArmWeight = 1.0f;
            deformationConstraint.data.RightArmWeight = 1.0f;
            deformationConstraint.data.LeftHandWeight = 1.0f;
            deformationConstraint.data.RightHandWeight = 1.0f;
            deformationConstraint.data.LeftLegWeight = 1.0f;
            deformationConstraint.data.RightLegWeight = 1.0f;
            deformationConstraint.data.LeftToesWeight = 1.0f;
            deformationConstraint.data.RightToesWeight = 1.0f;
            deformationConstraint.data.AlignFeetWeight = 0.75f;

            deformationConstraint.data.AssignAnimator(animator);
            deformationConstraint.data.SetUpLeftArmData();
            deformationConstraint.data.SetUpRightArmData();
            deformationConstraint.data.SetUpLeftLegData();
            deformationConstraint.data.SetUpRightLegData();
            deformationConstraint.data.SetUpHipsAndHeadBones();
            deformationConstraint.data.SetUpBonePairs();
            deformationConstraint.data.SetUpBoneTargets(deformationConstraint.transform);
            deformationConstraint.data.SetUpAdjustments(HelperMenusCommon.GetRestPoseObject(HelperMenusCommon.CheckIfTPose(animator)));
            deformationConstraint.data.InitializeStartingScale();

            PrefabUtility.RecordPrefabInstancePropertyModifications(deformationConstraint);

            return deformationConstraint;
        }

        private static Transform AddBoneTarget(GameObject mainParent,
            string nameOfTarget, Transform targetTransform = null)
        {
            Transform boneTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (boneTarget == null)
            {
                GameObject boneTargetObject =
                    new GameObject(nameOfTarget);
                Undo.RegisterCreatedObjectUndo(boneTargetObject, "Create Bone Target " + nameOfTarget);

                Undo.SetTransformParent(boneTargetObject.transform, mainParent.transform,
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


        private static bool DestroyBoneTarget(GameObject mainParent, string nameOfTarget)
        {
            Transform handTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (handTarget != null)
            {
                Undo.DestroyObjectImmediate(handTarget);
                return true;
            }
            return false;
        }

        private static bool DestroyTwoBoneIKConstraint(GameObject rigObject, string name)
        {
            Transform twoBoneIKConstraintObjTransform = rigObject.transform.Find(name);
            if (twoBoneIKConstraintObjTransform != null)
            {
                Undo.DestroyObjectImmediate(twoBoneIKConstraintObjTransform);
                return true;
            }
            return false;
        }

        private static void AddFullBodyHandDeformation(GameObject mainParent,
            Animator animatorComp, OVRSkeleton skeletalComponent)
        {
            FullBodyHandDeformation fullBodyHandDeformation =
                mainParent.GetComponent<FullBodyHandDeformation>();
            if (fullBodyHandDeformation == null)
            {
                fullBodyHandDeformation = mainParent.AddComponent<FullBodyHandDeformation>();
                Undo.RegisterCreatedObjectUndo(fullBodyHandDeformation, "Add full body hand deformation");
            }

            fullBodyHandDeformation.AnimatorComp = animatorComp;
            fullBodyHandDeformation.Skeleton = skeletalComponent;
            fullBodyHandDeformation.LeftHand = animatorComp.GetBoneTransform(HumanBodyBones.LeftHand);
            fullBodyHandDeformation.RightHand = animatorComp.GetBoneTransform(HumanBodyBones.RightHand);
            fullBodyHandDeformation.FingerOffsets = new FullBodyHandDeformation.FingerOffset[0];
            fullBodyHandDeformation.CalculateFingerData();
        }

        private static void ValidGameObjectForAnimationRigging(GameObject go)
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
    }
}
