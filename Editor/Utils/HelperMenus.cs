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
    internal static class HelperMenus
    {
        private const string _MOVEMENT_SAMPLES_BT_MENU =
            "Body Tracking/";
        private const string _ANIM_RIGGING_RETARGETING_MENU =
            "Animation Rigging Retargeting";
        private const string _CONSTRAINTS_SUFFIX =
            " (constraints)";

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_BT_MENU + _ANIM_RIGGING_RETARGETING_MENU + _CONSTRAINTS_SUFFIX)]
        private static void SetupCharacterForAnimationRiggingRetargetingConstraints()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                AddComponentsHelper.ValidGameObjectForAnimationRigging(activeGameObject);
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
                AddComponentsHelper.AddMainRetargetingComponent(activeGameObject, false);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) =
                AddComponentsHelper.AddBasicAnimationRiggingComponents(activeGameObject);

            List<MonoBehaviour> constraintMonos = new List<MonoBehaviour>();
            RetargetingAnimationConstraint retargetConstraint =
                AddComponentsHelper.AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;
            constraintMonos.Add(retargetConstraint);

            // Destroy old components
            AddComponentsHelper.DestroyLegacyComponents(rigObject, activeGameObject);

            // Body deformation.
            BoneTarget[] spineBoneTargets = AddComponentsHelper.AddBoneTargets(rigObject, animatorComp, false);
            DeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp, spineBoneTargets);
            constraintMonos.Add(deformationConstraint);

            // Setup retargeted bone targets.
            AddComponentsHelper.SetupExternalBoneTargets(retargetingLayer, false, spineBoneTargets);

            // Disable root motion.
            animatorComp.applyRootMotion = false;
            Debug.Log($"Disabling root motion on the {animatorComp.gameObject.name} animator.");
            EditorUtility.SetDirty(animatorComp);

            // Add retargeting animation rig to tie everything together.
            AddComponentsHelper.AddJointAdjustments(animatorComp, retargetingLayer);
            AddComponentsHelper.AddRetargetingAnimationRig(
                retargetingLayer, rigBuilder, constraintMonos.ToArray());

            // Add retargeting processors to the retargeting layer.
            AddComponentsHelper.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            AddComponentsHelper.AddBlendHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            AddComponentsHelper.AddCorrectBonesRetargetingProcessor(retargetingLayer);
            AddComponentsHelper.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Left);
            AddComponentsHelper.AddCorrectHandRetargetingProcessor(retargetingLayer, Handedness.Right);
            AddComponentsHelper.AddHandDeformationRetargetingProcessor(retargetingLayer);

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
    }
}
