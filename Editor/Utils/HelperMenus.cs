// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Tracking;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.RetargetedBoneTargets;

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
            retargetingLayer.RetargetingConstraint = retargetConstraint;

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(activeGameObject, retargetingLayer, rigBuilder,
                new RetargetingAnimationConstraint[] { retargetConstraint }, retargetingLayer);

            Undo.SetCurrentGroupName("Setup Animation Rigging Retargeting");
        }

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

            DeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp);
            constraintMonos.Add(deformationConstraint);

            Transform leftHandTarget = AddHandTarget(rigObject, "LeftHandTarget");
            TwoBoneIKConstraint leftTwoBoneIKConstraint =
                AddTwoBoneIKConstraint(rigObject, "LeftArmIK",
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftHand),
                    leftHandTarget);

            Transform rightHandTarget = AddHandTarget(rigObject, "RightHandTarget");
            TwoBoneIKConstraint rightTwoBoneIKConstraint =
                AddTwoBoneIKConstraint(rigObject, "RightArmIK",
                    animatorComp.GetBoneTransform(HumanBodyBones.RightUpperArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.RightLowerArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.RightHand),
                    rightHandTarget);

            RetargetedBoneTarget leftHandRTTarget = new RetargetedBoneTarget();
            leftHandRTTarget.BoneId = OVRSkeleton.BoneId.Body_LeftHandWrist;
            leftHandRTTarget.Target = leftHandTarget;
            leftHandRTTarget.HumanBodyBone = HumanBodyBones.LeftHand;
            RetargetedBoneTarget rightHandRTTarget = new RetargetedBoneTarget();
            rightHandRTTarget.BoneId = OVRSkeleton.BoneId.Body_RightHandWrist;
            rightHandRTTarget.Target = rightHandTarget;
            rightHandRTTarget.HumanBodyBone = HumanBodyBones.RightHand;
            AddRetargetedBoneTargetComponent(activeGameObject,
                new RetargetedBoneTarget[]
                {
                    leftHandRTTarget, rightHandRTTarget
                });

            AddHandBlendConstraint(activeGameObject, new MonoBehaviour[] { leftTwoBoneIKConstraint },
                retargetingLayer, CustomMappings.BodyTrackingBoneId.Body_LeftHandWrist,
                animatorComp.GetBoneTransform(HumanBodyBones.Head));

            AddHandBlendConstraint(activeGameObject, new MonoBehaviour[] { rightTwoBoneIKConstraint },
                retargetingLayer, CustomMappings.BodyTrackingBoneId.Body_RightHandWrist,
                animatorComp.GetBoneTransform(HumanBodyBones.Head));

            // Add final components to tie everything together.
            AddAnimationRiggingLayer(activeGameObject, retargetingLayer, rigBuilder,
                constraintMonos.ToArray(), retargetingLayer);

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

                // keep retargeter disabled until it initializes properly
                retargetConstraint.gameObject.SetActive(false);
            }
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
            Undo.RegisterCreatedObjectUndo(rigSetup, "Create Anim Rig Setup");
        }

        private static DeformationConstraint AddDeformationConstraint(
            GameObject rigObject, Animator animator)
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

                deformationConstraint.data.SpineTranslationCorrectionTypeField
                    = DeformationData.SpineTranslationCorrectionType.SkipHips;
                deformationConstraint.data.ApplyToArms = true;
                deformationConstraint.data.ApplyToHands = true;
                deformationConstraint.data.ArmWeight = 1.0f;
                deformationConstraint.data.HandWeight = 1;

                deformationConstraint.data.AssignAnimator(animator);
                deformationConstraint.data.SetUpHipsAndHeadBones();
                deformationConstraint.data.SetUpLeftArmData();
                deformationConstraint.data.SetUpRightArmData();
                deformationConstraint.data.SetUpBonePairs();
                deformationConstraint.data.InitializeStartingScale();
            }
            return deformationConstraint;
        }

        private static Transform AddHandTarget(GameObject mainParent, string nameOfTarget)
        {
            Transform handTarget =
                mainParent.transform.Find(nameOfTarget);
            if (handTarget == null)
            {
                GameObject handTargetObject =
                    new GameObject(nameOfTarget);
                Undo.RegisterCreatedObjectUndo(handTargetObject, "Create Hand Target " + nameOfTarget);

                Undo.SetTransformParent(handTargetObject.transform, mainParent.transform,
                    $"Add Hand Target {nameOfTarget} To Main Parent");
                Undo.RegisterCompleteObjectUndo(handTargetObject,
                    $"Hand Target {nameOfTarget} Transform Init");
                handTarget = handTargetObject.transform;
                handTarget.transform.localPosition = Vector3.zero;
                handTarget.transform.localRotation = Quaternion.identity;
                handTarget.transform.localScale = Vector3.one;
            }
            return handTarget;
        }

        private static RetargetedBoneTargets AddRetargetedBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            RetargetedBoneTargets retargetedBoneTargets =
                mainParent.AddComponent<RetargetedBoneTargets>();
            Undo.RegisterCreatedObjectUndo(retargetedBoneTargets, "Add RT bone targets");

            retargetedBoneTargets.AutoAddTo = mainParent.GetComponent<MonoBehaviour>();
            retargetedBoneTargets.RetargetedBoneTargetsArray = boneTargetsArray;

            return retargetedBoneTargets;
        }

        private static TwoBoneIKConstraint AddTwoBoneIKConstraint(
            GameObject rigObject, string name, Transform root,
            Transform mid, Transform tip, Transform target)
        {
            TwoBoneIKConstraint twoBoneIKConstraint = null;
            Transform twoBoneIKConstraintObjTransform = rigObject.transform.Find(name);
            if (twoBoneIKConstraintObjTransform == null)
            {
                GameObject twoBoneIKConstraintObj =
                    new GameObject(name);
                twoBoneIKConstraint =
                    twoBoneIKConstraintObj.AddComponent<TwoBoneIKConstraint>();
                twoBoneIKConstraint.data.root = root;
                twoBoneIKConstraint.data.mid = mid;
                twoBoneIKConstraint.data.tip = tip;
                twoBoneIKConstraint.data.target = target;
                twoBoneIKConstraint.data.maintainTargetPositionOffset = false;
                twoBoneIKConstraint.data.maintainTargetRotationOffset = false;
                twoBoneIKConstraint.data.targetRotationWeight = 0.0f;
                twoBoneIKConstraint.data.targetPositionWeight = 1.0f;
                twoBoneIKConstraint.data.hintWeight = 1.0f;
                Undo.RegisterCreatedObjectUndo(twoBoneIKConstraintObj, "Create Two Bone IK " + name);

                Undo.SetTransformParent(twoBoneIKConstraintObj.transform, rigObject.transform,
                    $"Add TwoBone IK {name} Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(twoBoneIKConstraintObj,
                    $"TwoBone IK Constraint {name} Transform Init");
                twoBoneIKConstraintObj.transform.localPosition = Vector3.zero;
                twoBoneIKConstraintObj.transform.localRotation = Quaternion.identity;
                twoBoneIKConstraintObj.transform.localScale = Vector3.one;
            }
            return twoBoneIKConstraint;
        }

        private static BlendHandConstraints AddHandBlendConstraint(
            GameObject mainParent, MonoBehaviour[] constraints, RetargetingLayer retargetingLayer,
            CustomMappings.BodyTrackingBoneId boneIdToTest, Transform headTransform)
        {
            BlendHandConstraints blendConstraint =
                mainParent.AddComponent<BlendHandConstraints>();
            Undo.RegisterCreatedObjectUndo(blendConstraint, "Add blend constraint");

            foreach (MonoBehaviour constraint in constraints)
            {
                blendConstraint.AddSkeletalConstraint(constraint);
            }
            blendConstraint.RetargetingLayerComp = retargetingLayer;
            blendConstraint.BoneIdToTest = boneIdToTest;
            blendConstraint.HeadTransform = headTransform;
            blendConstraint.AutoAddTo = mainParent.GetComponent<MonoBehaviour>();
            blendConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

            return blendConstraint;
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

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map Correctives blendshapes");
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for Correctives Tracking");
        }
    }
}
