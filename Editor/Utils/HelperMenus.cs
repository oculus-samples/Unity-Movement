// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Tracking;
using System;
using System.Collections.Generic;
using System.Reflection;
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

            // Body deformation.
            RetargetedBoneTarget[] spineBoneTargets = AddSpineBoneTargets(rigObject, animatorComp);
            DeformationConstraint deformationConstraint =
                AddDeformationConstraint(rigObject, animatorComp, spineBoneTargets);
            constraintMonos.Add(deformationConstraint);

            Transform leftElbowTarget = AddHandTarget(rigObject, "LeftElbowTarget", animatorComp.GetBoneTransform(HumanBodyBones.LeftLowerArm));
            Transform leftHandTarget = AddHandTarget(rigObject, "LeftHandTarget", animatorComp.GetBoneTransform(HumanBodyBones.LeftHand));
            TwoBoneIKConstraint leftTwoBoneIKConstraint =
                AddTwoBoneIKConstraint(rigObject, "LeftArmIK",
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.LeftHand),
                    leftHandTarget, leftElbowTarget);
            Undo.SetTransformParent(leftHandTarget, leftTwoBoneIKConstraint.transform,
                "Parent Left Hand to Two-Bone IK");
            Undo.SetTransformParent(leftElbowTarget, leftTwoBoneIKConstraint.transform,
                "Parent Left Elbow to Two-Bone IK");

            Transform rightElbowTarget = AddHandTarget(rigObject, "RightElbowTarget", animatorComp.GetBoneTransform(HumanBodyBones.RightLowerArm));
            Transform rightHandTarget = AddHandTarget(rigObject, "RightHandTarget", animatorComp.GetBoneTransform(HumanBodyBones.RightHand));
            TwoBoneIKConstraint rightTwoBoneIKConstraint =
                AddTwoBoneIKConstraint(rigObject, "RightArmIK",
                    animatorComp.GetBoneTransform(HumanBodyBones.RightUpperArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.RightLowerArm),
                    animatorComp.GetBoneTransform(HumanBodyBones.RightHand),
                    rightHandTarget, rightElbowTarget);
            Undo.SetTransformParent(rightHandTarget, rightTwoBoneIKConstraint.transform,
                "Parent Right Hand to Two-Bone IK");
            Undo.SetTransformParent(rightElbowTarget, rightTwoBoneIKConstraint.transform,
                "Parent Right Elbow to Two-Bone IK");

            RetargetedBoneTarget leftHandRTTarget = new RetargetedBoneTarget
            {
                BoneId = OVRSkeleton.BoneId.Body_LeftHandWrist, Target = leftHandTarget,
                HumanBodyBone = HumanBodyBones.LeftHand
            };
            RetargetedBoneTarget leftElbowRTTarget = new RetargetedBoneTarget
            {
                BoneId = OVRSkeleton.BoneId.Body_LeftArmLower, Target = leftElbowTarget,
                HumanBodyBone = HumanBodyBones.LeftLowerArm
            };
            RetargetedBoneTarget rightHandRTTarget = new RetargetedBoneTarget
            {
                BoneId = OVRSkeleton.BoneId.Body_RightHandWrist, Target = rightHandTarget,
                HumanBodyBone = HumanBodyBones.RightHand
            };
            RetargetedBoneTarget rightElbowRTTarget = new RetargetedBoneTarget
            {
                BoneId = OVRSkeleton.BoneId.Body_RightArmLower, Target = rightElbowTarget,
                HumanBodyBone = HumanBodyBones.RightLowerArm
            };

            var retargetedBoneTargets = new List<RetargetedBoneTarget>
            {
                leftHandRTTarget, rightHandRTTarget, leftElbowRTTarget, rightElbowRTTarget
            };
            retargetedBoneTargets.AddRange(spineBoneTargets);
            AddRetargetedBoneTargetComponent(activeGameObject, retargetedBoneTargets.ToArray());

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

        private static RetargetingLayer AddMainRetargetingComponents(GameObject mainParent)
        {
            RetargetingLayer retargetingLayer = mainParent.GetComponent<RetargetingLayer>();
            if (!retargetingLayer)
            {
                retargetingLayer = mainParent.AddComponent<RetargetingLayer>();
                Undo.RegisterCreatedObjectUndo(retargetingLayer, "Add Retargeting Layer");
            }

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

        private static RetargetedBoneTarget[] AddSpineBoneTargets(GameObject rigObject,
            Animator animator)
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
                new(OVRSkeleton.BoneId.Body_Hips, hipsTarget),
                new(OVRSkeleton.BoneId.Body_SpineLower, spineLowerTarget),
                new(OVRSkeleton.BoneId.Body_SpineUpper, spineUpperTarget),
                new(OVRSkeleton.BoneId.Body_Chest, chestTarget),
                new(OVRSkeleton.BoneId.Body_Neck, neckTarget),
                new(OVRSkeleton.BoneId.Body_Head, headTarget),
            };

            foreach (var boneToRetarget in bonesToRetarget)
            {
                RetargetedBoneTarget boneRTTarget = new RetargetedBoneTarget();
                boneRTTarget.BoneId = boneToRetarget.Item1;
                boneRTTarget.Target = boneToRetarget.Item2;
                boneRTTarget.HumanBodyBone = CustomMappings.BoneIdToHumanBodyBone[boneRTTarget.BoneId];
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
            GameObject rigObject, Animator animator, RetargetedBoneTarget[] spineBoneTargets)
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
                Undo.SetTransformParent(spineBoneTarget.Target, deformationConstraint.transform,
                    $"Parent Spine Bone Target {spineBoneTarget.Target.name} to Deformation.");
                Undo.RegisterCompleteObjectUndo(spineBoneTarget.Target,
                    $"Spine Bone Target {spineBoneTarget.Target.name} Init");
            }

            deformationConstraint.data.SpineTranslationCorrectionTypeField
                = DeformationData.SpineTranslationCorrectionType.AccurateHipsAndHead;
            deformationConstraint.data.SpineAlignmentWeight = 1.0f;
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

        private static Transform AddHandTarget(GameObject mainParent,
            string nameOfTarget, Transform targetTransform = null)
        {
            Transform handTarget =
                mainParent.transform.FindChildRecursive(nameOfTarget);
            if (handTarget == null)
            {
                GameObject handTargetObject =
                    new GameObject(nameOfTarget);
                Undo.RegisterCreatedObjectUndo(handTargetObject,
                    $"Create Hand Target {nameOfTarget}");
                Undo.SetTransformParent(handTargetObject.transform, mainParent.transform,
                    $"Add Hand Target {nameOfTarget} To Main Parent");
                Undo.RegisterCompleteObjectUndo(handTargetObject,
                    $"Hand Target {nameOfTarget} Transform Init");
                handTarget = handTargetObject.transform;
            }
            if (targetTransform != null)
            {
                handTarget.position = targetTransform.position;
                handTarget.rotation = targetTransform.rotation;
                handTarget.localScale = targetTransform.localScale;
            }
            else
            {
                handTarget.localPosition = Vector3.zero;
                handTarget.localRotation = Quaternion.identity;
                handTarget.localScale = Vector3.one;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(handTarget);
            return handTarget;
        }

        private static RetargetedBoneTargets AddRetargetedBoneTargetComponent(GameObject mainParent,
            RetargetedBoneTarget[] boneTargetsArray)
        {
            RetargetedBoneTargets retargetedBoneTargets = mainParent.GetComponent<RetargetedBoneTargets>();
            if (retargetedBoneTargets == null)
            {
                retargetedBoneTargets =
                    mainParent.AddComponent<RetargetedBoneTargets>();
                Undo.RegisterCreatedObjectUndo(retargetedBoneTargets, "Add RT bone targets");
            }

            retargetedBoneTargets.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
            retargetedBoneTargets.RetargetedBoneTargetsArray = boneTargetsArray;

            PrefabUtility.RecordPrefabInstancePropertyModifications(retargetedBoneTargets);
            return retargetedBoneTargets;
        }

        private static TwoBoneIKConstraint AddTwoBoneIKConstraint(
            GameObject rigObject, string name, Transform root,
            Transform mid, Transform tip, Transform target, Transform hint)
        {
            TwoBoneIKConstraint twoBoneIKConstraint = null;
            Transform twoBoneIKConstraintObjTransform = rigObject.transform.Find(name);
            if (twoBoneIKConstraintObjTransform == null)
            {
                GameObject twoBoneIKConstraintObj =
                    new GameObject(name);
                twoBoneIKConstraint =
                    twoBoneIKConstraintObj.AddComponent<TwoBoneIKConstraint>();
                Undo.RegisterCreatedObjectUndo(twoBoneIKConstraintObj,
                    $"Create Two Bone IK {name}");
                Undo.SetTransformParent(twoBoneIKConstraintObj.transform, rigObject.transform,
                    $"Add TwoBone IK {name} Constraint to Rig");
                Undo.RegisterCompleteObjectUndo(twoBoneIKConstraintObj,
                    $"TwoBone IK Constraint {name} Transform Init");
                twoBoneIKConstraintObj.transform.localPosition = Vector3.zero;
                twoBoneIKConstraintObj.transform.localRotation = Quaternion.identity;
                twoBoneIKConstraintObj.transform.localScale = Vector3.one;
            }
            else
            {
                twoBoneIKConstraint = twoBoneIKConstraintObjTransform.GetComponent<TwoBoneIKConstraint>();
            }

            twoBoneIKConstraint.data.root = root;
            twoBoneIKConstraint.data.mid = mid;
            twoBoneIKConstraint.data.tip = tip;
            twoBoneIKConstraint.data.hint = hint;
            twoBoneIKConstraint.data.target = target;
            twoBoneIKConstraint.data.maintainTargetPositionOffset = false;
            twoBoneIKConstraint.data.maintainTargetRotationOffset = false;
            twoBoneIKConstraint.data.targetRotationWeight = 0.0f;
            twoBoneIKConstraint.data.targetPositionWeight = 1.0f;
            twoBoneIKConstraint.data.hintWeight = 1.0f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(twoBoneIKConstraint);
            return twoBoneIKConstraint;
        }

        private static BlendHandConstraints AddHandBlendConstraint(
            GameObject mainParent, MonoBehaviour[] constraints, RetargetingLayer retargetingLayer,
            CustomMappings.BodyTrackingBoneId boneIdToTest, Transform headTransform)
        {
            var blendHandConstraints = mainParent.GetComponentsInChildren<BlendHandConstraints>();
            foreach (var blendHandConstraint in blendHandConstraints)
            {
                if (blendHandConstraint.BoneIdToTest == boneIdToTest)
                {
                    blendHandConstraint.RetargetingLayerComp = retargetingLayer;
                    blendHandConstraint.BoneIdToTest = boneIdToTest;
                    blendHandConstraint.HeadTransform = headTransform;
                    blendHandConstraint.AutoAddTo = mainParent.GetComponent<RetargetingLayer>();
                    blendHandConstraint.BlendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(blendHandConstraint);
                    return blendHandConstraint;
                }
            }

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
