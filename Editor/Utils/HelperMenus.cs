// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Tracking;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.ExternalBoneTargets;

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
                HelperMenusCommon.ValidGameObjectForAnimationRigging(activeGameObject);
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
            RetargetingLayer retargetingLayer =
                HelperMenusCommon.AddMainRetargetingComponent(activeGameObject, false);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) =
                HelperMenusCommon.AddBasicAnimationRiggingComponents(activeGameObject);

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                HelperMenusCommon.AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            // Destroy old components
            HelperMenusCommon.DestroyLegacyComponents(rigObject, activeGameObject);

            // Body deformation.
            BoneTarget[] spineBoneTargets = HelperMenusCommon.AddBoneTargets(rigObject, animatorComp, false);
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
            HelperMenusCommon.AddJointAdjustments(animatorComp, retargetingLayer);
            HelperMenusCommon.AddRetargetingAnimationRig(
                retargetingLayer, rigBuilder, constraintMonos.ToArray());

            // Add retargeting processors to the retargeting layer.
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            HelperMenusCommon.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            HelperMenusCommon.AddCorrectBonesRetargetingProcessor(retargetingLayer);
            HelperMenusCommon.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            HelperMenusCommon.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            HelperMenusCommon.AddHandDeformationRetargetingProcessor(retargetingLayer);

            activeGameObject.transform.SetPositionAndRotation(
                previousPositionAndRotation.translation, previousPositionAndRotation.rotation);
            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
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
