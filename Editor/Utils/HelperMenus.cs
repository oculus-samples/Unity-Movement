// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Tracking;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        private const string _MOVEMENT_SAMPLES_FT_MENU =
            "Face Tracking/";
        private const string _CORRECTIVES_FACE_MENU =
            "Correctives Face";
        private const string _CORRECTIVES_FACE_DUPLICATE_MENU =
            "Correctives Face (allow duplicate mapping)";

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_BT_MENU + _ANIM_RIGGING_RETARGETING_MENU)]
        private static void SetupCharacterForAnimationRiggingRetargeting()
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

            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(activeGameObject, retargetingLayer, rigBuilder,
                retargetConstraint, retargetingLayer);

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

        private static void SetUpCharacterForCorrectivesFace(GameObject gameObject)
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

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map Correcives blendshapes");
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for CorrecivesFace Tracking");
        }
    }
}
