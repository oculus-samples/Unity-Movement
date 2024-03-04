// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.AnimationRigging.Deprecated;
using Oculus.Movement.Tracking;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
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
    internal static class HelperMenus
    {
        private const string _MOVEMENT_SAMPLES_MENU =
            "GameObject/Movement Samples/";
        private const string _MOVEMENT_SAMPLES_BT_MENU =
            "Body Tracking/";
        private const string _ANIM_RIGGING_RETARGETING_MENU =
            "Animation Rigging Retargeting";
        private const string _CONSTRAINTS_SUFFIX =
            " (constraints)";

        private const string _MOVEMENT_SAMPLES_FT_MENU =
            "Face Tracking/";
        private const string _CORRECTIVES_FACE_MENU =
            "Correctives Face";
        private const string _ARKIT_FACE_MENU =
            "ARKit Face";
        private const string _NO_DUPLICATES_SUFFIX =
            " (duplicate mapping off)";

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_BT_MENU + _ANIM_RIGGING_RETARGETING_MENU + _CONSTRAINTS_SUFFIX)]
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

            // Store the previous transform data.
            var previousPositionAndRotation =
                new AffineTransform(activeGameObject.transform.position, activeGameObject.transform.rotation);

            // Bring the object to root so that the auto adjustments calculations are correct.
            activeGameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

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
            HelperMenusCommon.DestroyLegacyComponents<BlendHandConstraints>(activeGameObject);
            HelperMenusCommon.DestroyLegacyComponents<RetargetedBoneTargets>(activeGameObject);
            HelperMenusCommon.DestroyLegacyComponents<AnimationRigSetup>(activeGameObject);
            HelperMenusCommon.DestroyLegacyComponents<FullBodyHandDeformation>(activeGameObject);

            // Body deformation.
            BoneTarget[] spineBoneTargets = AddSpineBoneTargets(rigObject, animatorComp);
            DeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp, spineBoneTargets);
            constraintMonos.Add(deformationConstraint);

            // Setup retargeted bone targets.
            HelperMenusCommon.SetupExternalBoneTargets(retargetingLayer, false, spineBoneTargets);

            // Disable root motion.
            animatorComp.applyRootMotion = false;
            Debug.Log($"Disabling root motion on the {animatorComp.gameObject.name} animator.");
            EditorUtility.SetDirty(animatorComp);

            // Add retargeting animation rig to tie everything together.
            HelperMenusCommon.AddRetargetingAnimationRig(
                retargetingLayer, rigBuilder, constraintMonos.ToArray());

            // Add retargeting processors to the retargeting layer.
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            AddCorrectBonesRetargetingProcessor(retargetingLayer);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            HelperMenusCommon.AddHandDeformationRetargetingProcessor(retargetingLayer);

            activeGameObject.transform.SetPositionAndRotation(
                previousPositionAndRotation.translation, previousPositionAndRotation.rotation);
            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
        }

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _CORRECTIVES_FACE_MENU)]
        private static void SetupCharacterForCorrectivesFace()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForCorrectivesFace(activeGameObject);
        }

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _CORRECTIVES_FACE_MENU +
            _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForCorrectivesFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForCorrectivesFace(activeGameObject, false);
        }

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU)]
        private static void SetupCharacterForARKitFace()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForARKitFace(activeGameObject);
        }

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU
            + _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForARKitFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForARKitFace(activeGameObject, false);
        }

        private static RetargetingLayer AddMainRetargetingComponents(Animator animator, GameObject mainParent)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
                Undo.RegisterCreatedObjectUndo(retargetingLayer, "Add Retargeting Layer");
            }
            retargetingLayer.EnableTrackingByProxy = true;

            var bodySectionToPosition =
                typeof(OVRUnityHumanoidSkeletonRetargeter).GetField(
                    "_bodySectionToPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (bodySectionToPosition != null)
            {
                bodySectionToPosition.SetValue(retargetingLayer, new[]
                {
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.LeftArm,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.RightArm,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.LeftHand,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.RightHand,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.Hips,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.Back,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.Neck,
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.Head
                });
            }

            HelperMenusCommon.AddJointAdjustments(animator, retargetingLayer);

            EditorUtility.SetDirty(retargetingLayer);

            OVRBody bodyComp = mainParent.GetComponent<OVRBody>();
            if (!bodyComp)
            {
                bodyComp = mainParent.AddComponent<OVRBody>();
                Undo.RegisterCreatedObjectUndo(bodyComp, "Add OVRBody component");
            }

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
                rigBuilder.layers = new List<RigLayer>
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
                retargetConstraint.RetargetingLayerComp = retargetingLayer;
                retargetConstraint.data.AllowDynamicAdjustmentsRuntime = true;
                Undo.RegisterCreatedObjectUndo(retargetingAnimConstraintObj, "Create Retargeting Constraint");

                Undo.SetTransformParent(retargetingAnimConstraintObj.transform, rigObject.transform, "Add Retarget Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(retargetingAnimConstraintObj, "Retarget Constraint Transform Init");
                retargetConstraint.transform.localPosition = Vector3.zero;
                retargetConstraint.transform.localRotation = Quaternion.identity;
                retargetConstraint.transform.localScale = Vector3.one;
            }

            // Keep retargeter disabled until it initializes properly.
            retargetConstraint.gameObject.SetActive(false);
            return retargetConstraint;
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

        private static BoneTarget[] AddSpineBoneTargets(GameObject rigObject,
            Animator animator)
        {
            var boneTargets = new List<BoneTarget>();
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
                BoneTarget boneRTTarget = new BoneTarget();
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
                Undo.RegisterCreatedObjectUndo(spineTargetObject,
                    $"Create Spine Target {nameOfTarget}");
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

        private static DeformationConstraint AddDeformationConstraint(
            GameObject rigObject, Animator animator, BoneTarget[] spineBoneTargets)
        {
            DeformationConstraint deformationConstraint =
                rigObject.GetComponentInChildren<DeformationConstraint>();
            if (deformationConstraint == null)
            {
                GameObject deformationConstraintObject =
                    new GameObject("Deformation");
                deformationConstraint =
                    deformationConstraintObject.AddComponent<DeformationConstraint>();

                Undo.RegisterCreatedObjectUndo(deformationConstraintObject, "Create Deformation");
                Undo.SetTransformParent(deformationConstraintObject.transform, rigObject.transform,
                    $"Add Deformation Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(deformationConstraintObject,
                    $"Deformation Constraint Transform Init");
                deformationConstraintObject.transform.localPosition = Vector3.zero;
                deformationConstraintObject.transform.localRotation = Quaternion.identity;
                deformationConstraintObject.transform.localScale = Vector3.one;
            }

            foreach (var spineBoneTarget in spineBoneTargets)
            {
                if (spineBoneTarget.Target == null)
                {
                    continue;
                }
                Undo.SetTransformParent(spineBoneTarget.Target, deformationConstraint.transform,
                    $"Parent Spine Bone Target {spineBoneTarget.Target.name} to Deformation.");
                Undo.RegisterCompleteObjectUndo(spineBoneTarget.Target,
                    $"Spine Bone Target {spineBoneTarget.Target.name} Init");
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

            deformationConstraint.data.AssignAnimator(animator);
            deformationConstraint.data.SetUpLeftArmData();
            deformationConstraint.data.SetUpRightArmData();
            deformationConstraint.data.SetUpHipsToHeadBones();
            deformationConstraint.data.SetUpHipsToHeadBoneTargets(deformationConstraint.transform);
            deformationConstraint.data.SetUpBonePairs();
            deformationConstraint.data.InitializeStartingScale();

            PrefabUtility.RecordPrefabInstancePropertyModifications(deformationConstraint);
            return deformationConstraint;
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

        private static void SetUpCharacterForCorrectivesFace(GameObject gameObject,
            bool allowDuplicates = true)
        {
            Undo.IncrementCurrentGroup();

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
                Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
            }

            var face = gameObject.GetComponent<CorrectivesFace>();
            if (!face)
            {
                face = gameObject.AddComponent<CorrectivesFace>();
                face.FaceExpressions = faceExpressions;
                Undo.RegisterCreatedObjectUndo(face, "Create CorrectivesFace component");
            }

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
                Undo.RecordObject(face, "Assign to BlendshapeModifier field");
            }

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map Correctives blendshapes");
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for Correctives Tracking");
        }

        private static void SetUpCharacterForARKitFace(GameObject gameObject,
            bool allowDuplicates = true)
        {
            Undo.IncrementCurrentGroup();

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
                Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
            }

            var face = gameObject.GetComponent<ARKitFace>();
            if (!face)
            {
                face = gameObject.AddComponent<ARKitFace>();
                face.FaceExpressions = faceExpressions;
                Undo.RegisterCreatedObjectUndo(face, "Create ARKit component");
            }
            face.RetargetingTypeField = OVRCustomFace.RetargetingType.Custom;

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
                Undo.RecordObject(face, "Assign to BlendshapeModifier field");
            }

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map ARKit blendshapes");
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for ARKit Tracking");
        }
    }
}
