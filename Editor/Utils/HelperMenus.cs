// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_BT_MENU + _ANIM_RIGGING_RETARGETING_MENU)]
        private static void SetupCharacterForAnimationRiggingRetargeting()
        {
            try
            {
                ValidGameObjectForAnimationRigging(Selection.activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Retargeting setup error.", e.Message, "Ok");
                return;
            }

            Undo.IncrementCurrentGroup();

            var mainParent = Selection.activeGameObject;

            // Add the retargeting and body tracking components at root first.
            RetargetingLayer retargetingLayer = AddMainRetargetingComponents(mainParent);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) = AddBasicAnimationRiggingComponents(mainParent);

            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(mainParent, retargetingLayer, rigBuilder,
                retargetConstraint, retargetingLayer);

            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
        }

        private static RetargetingLayer AddMainRetargetingComponents(GameObject mainParent)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
                Undo.RegisterCreatedObjectUndo(retargetingLayer, "Add Retargeting Layer");
            }

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
                rigBuilder.layers = new System.Collections.Generic.List<RigLayer>
                {
                    new RigLayer(rigComponent, true)
                };
                // disabled until it needs to be used
                rigBuilder.enabled = false;
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
                rigObject.GetComponentInChildren<RetargetingAnimationConstraint>();
            if (retargetConstraint == null)
            {
                GameObject retargetingAnimConstraintObj =
                    new GameObject("RetargetingConstraint");
                retargetConstraint =
                    retargetingAnimConstraintObj.AddComponent<RetargetingAnimationConstraint>();
                retargetConstraint.RetargetingLayerComp = retargetingLayer;
                Undo.RegisterCreatedObjectUndo(retargetingAnimConstraintObj, "Create Retargeting Constraint");

                Undo.SetTransformParent(retargetingAnimConstraintObj.transform, rigObject.transform, "Add Retarget Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(retargetingAnimConstraintObj, "Retarget Constraint Transform Init");
                retargetConstraint.transform.localPosition = Vector3.zero;
                retargetConstraint.transform.localRotation = Quaternion.identity;
                retargetConstraint.transform.localScale = Vector3.one;
            }
            return retargetConstraint;
        }

        private static void AddAnimationRiggingLayer(GameObject mainParent,
            OVRSkeleton skeletalComponent, RigBuilder rigBuilder,
            MonoBehaviour constraintComponent,
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
            // disabled until skeleton is initialized
            animatorComponent.enabled = false;
            rigSetup.AnimatorComp = animatorComponent;
            rigSetup.RigbuilderComp = rigBuilder;
            if (constraintComponent != null)
            {
                rigSetup.AddSkeletalConstraint(constraintComponent);
            }
            rigSetup.RebindAnimator = true;
            rigSetup.ReEnableRig = true;
            rigSetup.RetargetingLayerComp = retargetingLayer;
            Undo.RegisterCreatedObjectUndo(rigSetup, "Create Anim Rig Setup");
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
