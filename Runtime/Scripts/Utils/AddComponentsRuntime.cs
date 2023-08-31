// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine;
using Oculus.Movement.Tracking;

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
        public static void SetupCharacterForRetargeting(GameObject selectedGameObject)
        {
            selectedGameObject.AddComponent<OVRBody>();
            selectedGameObject.AddComponent<OVRUnityHumanoidSkeletonRetargeter>();
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
        public static void SetupCharacterForAnimationRiggingRetargeting(
            GameObject selectedGameObject)
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
            RetargetingLayer retargetingLayer = AddMainRetargetingComponents(mainParent);

            GameObject rigObject;
            RigBuilder rigBuilder;
            (rigBuilder, rigObject) = AddBasicAnimationRiggingComponents(mainParent);

            RetargetingAnimationConstraint retargetConstraint =
                AddRetargetingConstraint(rigObject, retargetingLayer);
            retargetingLayer.RetargetingConstraint = retargetConstraint;

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(mainParent, retargetingLayer, rigBuilder,
                retargetConstraint, retargetingLayer);
            selectedGameObject.SetActive(true);
        }

        private static RetargetingLayer AddMainRetargetingComponents(GameObject mainParent)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
            }

            OVRBody bodyComp = mainParent.GetComponent<OVRBody>();
            if (!bodyComp)
            {
                bodyComp = mainParent.AddComponent<OVRBody>();
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
            }

            RigBuilder rigBuilder = mainParent.GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                rigBuilder = mainParent.AddComponent<RigBuilder>();
                rigBuilder.layers = new System.Collections.Generic.List<RigLayer>
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
            var animatorComponent = mainParent.GetComponent<Animator>();
            rigSetup = mainParent.AddComponent<AnimationRigSetup>();
            rigSetup.Skeleton = skeletalComponent;
            rigSetup.AnimatorComp = animatorComponent;
            rigSetup.RigbuilderComp = rigBuilder;
            if (constraintComponent != null)
            {
                rigSetup.AddSkeletalConstraint(constraintComponent);
            }

            rigSetup.RebindAnimator = true;
            rigSetup.ReEnableRig = true;
            rigSetup.RetargetingLayerComp = retargetingLayer;
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
