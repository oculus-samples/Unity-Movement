// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.RetargetedBoneTargets;
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
            RetargetingLayer retargetingLayer = AddMainRetargetingComponents(activeGameObject);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) = AddBasicAnimationRiggingComponents(activeGameObject);

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            Animator animatorComp = activeGameObject.GetComponent<Animator>();

            // Destroy old components
            DestroyBoneTarget(rigObject, "LeftHandTarget");
            DestroyBoneTarget(rigObject, "RightHandTarget");
            DestroyBoneTarget(rigObject, "LeftElbowTarget");
            DestroyBoneTarget(rigObject, "RightElbowTarget");
            DestroyTwoBoneIKConstraint(rigObject, "LeftArmIK");
            DestroyTwoBoneIKConstraint(rigObject, "RightArmIK");

            // Full body deformation.
            RetargetedBoneTarget[] spineBoneTargets = AddSpineBoneTargets(rigObject, animatorComp);
            FullBodyDeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp, spineBoneTargets);
            constraintMonos.Add(deformationConstraint);

            // Setup retargeted bone targets.
            AddRetargetedBoneTargetComponent(activeGameObject, spineBoneTargets);

            // Hand blend constraints.
            AddHandBlendConstraint(activeGameObject, null,
                retargetingLayer, OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                animatorComp.GetBoneTransform(HumanBodyBones.Head));

            AddHandBlendConstraint(activeGameObject, null,
                retargetingLayer, OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                animatorComp.GetBoneTransform(HumanBodyBones.Head));

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(activeGameObject, retargetingLayer, rigBuilder,
                constraintMonos.ToArray(), retargetingLayer);

            // Add retargeting processors to the retargeting layer.
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

        private static RetargetedBoneTarget[] AddSpineBoneTargets(GameObject rigObject, Animator animator)
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
                new(OVRSkeleton.BoneId.FullBody_Hips, hipsTarget),
                new(OVRSkeleton.BoneId.FullBody_SpineLower, spineLowerTarget),
                new(OVRSkeleton.BoneId.FullBody_SpineUpper, spineUpperTarget),
                new(OVRSkeleton.BoneId.FullBody_Chest, chestTarget),
                new(OVRSkeleton.BoneId.FullBody_Neck, neckTarget),
                new(OVRSkeleton.BoneId.FullBody_Head, headTarget),
            };

            foreach (var boneToRetarget in bonesToRetarget)
            {
                RetargetedBoneTarget boneRTTarget = new RetargetedBoneTarget();
                boneRTTarget.BoneId = boneToRetarget.Item1;
                boneRTTarget.Target = boneToRetarget.Item2;
                boneRTTarget.HumanBodyBone = OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[boneRTTarget.BoneId];
                boneTargets.Add(boneRTTarget);
            }
            return boneTargets.ToArray();
        }

        private static RetargetingLayer AddMainRetargetingComponents(GameObject mainParent)
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

            var bodySectionToPosition =
                typeof(OVRUnityHumanoidSkeletonRetargeter).GetField(
                    "_fullBodySectionToPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);

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
                    OVRHumanBodyBonesMappings.BodySection.Head,
                    OVRHumanBodyBonesMappings.BodySection.LeftLeg,
                    OVRHumanBodyBonesMappings.BodySection.LeftFoot,
                    OVRHumanBodyBonesMappings.BodySection.RightLeg,
                    OVRHumanBodyBonesMappings.BodySection.RightFoot
                });
            }

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
            GameObject rigObject, Animator animator, RetargetedBoneTarget[] spineBoneTargets)
        {
            FullBodyDeformationConstraint deformationConstraint =
                rigObject.GetComponentInChildren<FullBodyDeformationConstraint>();
            DeformationConstraint baseDeformationConstraint =
                rigObject.GetComponentInChildren<DeformationConstraint>();
            if (deformationConstraint == null && baseDeformationConstraint != null)
            {
                Undo.DestroyObjectImmediate(baseDeformationConstraint.gameObject);
            }
            if (deformationConstraint == null)
            {
                GameObject deformationConstraintObject =
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

                foreach (var spineBoneTarget in spineBoneTargets)
                {
                    Undo.SetTransformParent(spineBoneTarget.Target, deformationConstraintObject.transform,
                        $"Parent Spine Bone Target {spineBoneTarget.Target.name} to Deformation.");
                    Undo.RegisterCompleteObjectUndo(spineBoneTarget.Target,
                        $"Spine Bone Target {spineBoneTarget.Target.name} Init");
                }
            }

            deformationConstraint.data.SpineTranslationCorrectionTypeField
                = FullBodyDeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead;
            deformationConstraint.data.SpineLowerAlignmentWeight = 1.0f;
            deformationConstraint.data.SpineUpperAlignmentWeight = 0.5f;
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

            deformationConstraint.data.AssignAnimator(animator);
            deformationConstraint.data.SetUpLeftArmData();
            deformationConstraint.data.SetUpRightArmData();
            deformationConstraint.data.SetUpLeftLegData();
            deformationConstraint.data.SetUpRightLegData();
            deformationConstraint.data.SetUpHipsAndHeadBones();
            deformationConstraint.data.SetUpBonePairs();
            deformationConstraint.data.SetUpBoneTargets(deformationConstraint.transform);
            deformationConstraint.data.InitializeStartingScale();

            PrefabUtility.RecordPrefabInstancePropertyModifications(deformationConstraint);

            return deformationConstraint;
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
                Undo.RegisterCreatedObjectUndo(spineTargetObject, "Create Spine Target " + nameOfTarget);

                Undo.SetTransformParent(spineTargetObject.transform, mainParent.transform,
                    $"Add Spine Target {nameOfTarget} To Main Parent");
                Undo.RegisterCompleteObjectUndo(spineTargetObject,
                    $"Spine Target {nameOfTarget} Transform Init");
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

        private static FullBodyRetargetedBoneTargets AddRetargetedBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            var boneTargets = mainParent.GetComponent<FullBodyRetargetedBoneTargets>();
            var baseBoneTargets = mainParent.GetComponent<RetargetedBoneTargets>();
            if (boneTargets == null && baseBoneTargets != null)
            {
                Undo.DestroyObjectImmediate(baseBoneTargets);
            }
            if (boneTargets != null)
            {
                boneTargets.AutoAdd = mainParent.GetComponent<RetargetingLayer>();
                boneTargets.RetargetedBoneTargets = boneTargetsArray;
                PrefabUtility.RecordPrefabInstancePropertyModifications(boneTargets);
                return boneTargets;
            }
            FullBodyRetargetedBoneTargets retargetedBoneTargets =
                mainParent.AddComponent<FullBodyRetargetedBoneTargets>();
            Undo.RegisterCreatedObjectUndo(retargetedBoneTargets, "Add RT bone targets");

            retargetedBoneTargets.AutoAdd = mainParent.GetComponent<RetargetingLayer>();
            retargetedBoneTargets.RetargetedBoneTargets = boneTargetsArray;

            return retargetedBoneTargets;
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

        private static BlendHandConstraintsFullBody AddHandBlendConstraint(
            GameObject mainParent, MonoBehaviour[] constraints, RetargetingLayer retargetingLayer,
            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId boneIdToTest, Transform headTransform)
        {
            var blendHandConstraints = mainParent.GetComponents<BlendHandConstraintsFullBody>();
            var baseBlendHandConstraints = mainParent.GetComponents<BlendHandConstraints>();
            if (blendHandConstraints.Length == 0 && baseBlendHandConstraints.Length > 0)
            {
                for (int i = 0; i < baseBlendHandConstraints.Length; i++)
                {
                    Undo.DestroyObjectImmediate(baseBlendHandConstraints[i]);
                }
            }
            foreach (var blendHandConstraint in blendHandConstraints)
            {
                if (blendHandConstraint.BoneIdToTest == boneIdToTest)
                {
                    blendHandConstraint.Constraints = null;
                    blendHandConstraint.RetargetingLayerComp = retargetingLayer;
                    blendHandConstraint.BoneIdToTest = boneIdToTest;
                    blendHandConstraint.HeadTransform = headTransform;
                    blendHandConstraint.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
                    blendHandConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(blendHandConstraint);
                    return blendHandConstraint;
                }
            }
            BlendHandConstraintsFullBody blendConstraint =
                mainParent.AddComponent<BlendHandConstraintsFullBody>();
            Undo.RegisterCreatedObjectUndo(blendConstraint, "Add blend constraint");

            blendConstraint.Constraints = null;
            blendConstraint.RetargetingLayerComp = retargetingLayer;
            blendConstraint.BoneIdToTest = boneIdToTest;
            blendConstraint.HeadTransform = headTransform;
            blendConstraint.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
            blendConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

            return blendConstraint;
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
