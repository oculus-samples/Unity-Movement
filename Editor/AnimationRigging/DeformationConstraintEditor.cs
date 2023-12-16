// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the deformation constraint.
    /// </summary>
    [CustomEditor(typeof(DeformationConstraint)), CanEditMultipleObjects]
    public class DeformationConstraintEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var constraint = (DeformationConstraint)target;
            IDeformationData constraintData = constraint.data;

            if (constraintData.ConstraintCustomSkeleton == null &&
                constraintData.ConstraintAnimator == null)
            {
                if (GUILayout.Button("Find OVR Custom Skeleton"))
                {
                    Undo.RecordObject(constraint, "Find OVR Custom Skeleton");
                    var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
                    constraint.data.AssignOVRCustomSkeleton(skeleton);
                    EditorUtility.SetDirty(target);
                }
                if (GUILayout.Button("Find Animator"))
                {
                    Undo.RecordObject(constraint, "Find Animator");
                    var animator = constraint.GetComponentInParent<Animator>();
                    constraint.data.AssignAnimator(animator);
                    EditorUtility.SetDirty(target);
                }
            }
            if (constraintData.HipsToHeadBones == null ||
                constraintData.HipsToHeadBones.Length == 0)
            {
                if (GUILayout.Button("Find Hips To Head Bones"))
                {
                    Undo.RecordObject(constraint, "Find Hips To Head Bones");
                    constraint.data.SetUpHipsToHeadBones();
                    EditorUtility.SetDirty(target);
                }
            }
            if (constraintData.HipsToHeadBoneTargets == null ||
                constraintData.HipsToHeadBoneTargets.Length == 0)
            {
                if (GUILayout.Button("Find Hips To Head Bone Targets"))
                {
                    Undo.RecordObject(constraint, "Find Hips To Head Bone Targets");
                    constraint.data.SetUpHipsToHeadBoneTargets(constraint.transform);
                    EditorUtility.SetDirty(target);
                }
            }
            if (!constraintData.LeftArmDataInitialized)
            {
                if (GUILayout.Button("Initialize Left Arm"))
                {
                    Undo.RecordObject(constraint, "Initialize Left Arm");
                    constraint.data.SetUpLeftArmData();
                    EditorUtility.SetDirty(target);
                }
            }
            if (!constraintData.RightArmDataInitialized)
            {
                if (GUILayout.Button("Initialize Right Arm"))
                {
                    Undo.RecordObject(constraint, "Initialize Right Arm");
                    constraint.data.SetUpRightArmData();
                    EditorUtility.SetDirty(target);
                }
            }
            if (constraintData.BonePairs == null ||
                constraintData.BonePairs.Length == 0)
            {
                if (GUILayout.Button("Initialize Bone Pairs"))
                {
                    Undo.RecordObject(constraint, "Initialize Bone Pairs");
                    constraint.data.SetUpBonePairs();
                    EditorUtility.SetDirty(target);
                }
            }

            if (GUILayout.Button("Initialize starting scale"))
            {
                Undo.RecordObject(constraint, "Initialize starting scale");
                constraint.data.InitializeStartingScale();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Clear Transform data"))
            {
                Undo.RecordObject(constraint, "Clear Transform data");
                constraint.data.ClearTransformData();
                EditorUtility.SetDirty(target);
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            DrawDefaultInspector();
        }
    }
}
