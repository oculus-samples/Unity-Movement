// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.ExternalBoneTargets;

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
                HelperMenusCommon.AddMainRetargetingComponent(activeGameObject, true);

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

            // Full body deformation.
            BoneTarget[] boneTargets = HelperMenusCommon.AddBoneTargets(rigObject, animatorComp, true);
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
            HelperMenusCommon.AddCorrectBonesRetargetingProcessor(retargetingLayer);
            HelperMenusCommon.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            HelperMenusCommon.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            HelperMenusCommon.AddHandDeformationRetargetingProcessor(retargetingLayer);

            activeGameObject.transform.SetPositionAndRotation(
                previousPositionAndRotation.translation, previousPositionAndRotation.rotation);
            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
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
                    if (boneTarget.Target == null)
                    {
                        continue;
                    }
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
            deformationConstraint.data.AlignLeftLegWeight = 1.0f;
            deformationConstraint.data.AlignRightLegWeight = 1.0f;
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
    }
}
