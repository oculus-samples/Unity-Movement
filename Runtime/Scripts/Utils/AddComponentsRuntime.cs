// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine;

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
        /// Adds Animation rigging + retargeting at runtime. Similar to the HelperMenus version except
        /// no undo actions since those are not allowed at runtime.
        /// </summary>
        /// <param name="selectedGameObject">GameObject to add animation rigging + retargeting too.</param>
        /// <param name="tPoseMask">T-pose mask, intended for
        /// <see cref="RetargetingLayer"/> component created.</param>
        public static void SetupCharacterForAnimationRiggingRetargeting(
            GameObject selectedGameObject,
            AvatarMask tPoseMask = null)
        {
            try
            {
                ValidGameObjectForAnimationRigging(selectedGameObject);
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
            if (tPoseMask != null)
            {
                retargetingLayer.MaskToSetToTPoseComp = new AvatarMask();
                retargetingLayer.MaskToSetToTPoseComp.CopyOtherMaskBodyActiveValues(tPoseMask);
            }

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
