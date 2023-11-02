// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the capture animation constraint.
    /// </summary>
    [CustomEditor(typeof(CaptureAnimationConstraint)), CanEditMultipleObjects]
    public class CaptureAnimationConstraintEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var constraint = (CaptureAnimationConstraint)target;
            ICaptureAnimationData constraintData = constraint.data;
            if (constraintData.ConstraintAnimator == null)
            {
                if (GUILayout.Button("Find Animator"))
                {
                    Undo.RecordObject(constraint, "Find Animator");
                    var animator = constraint.GetComponentInParent<Animator>();
                    constraint.data.AssignAnimator(animator);
                }
            }

            if (constraintData.CurrentPose == null ||
                constraintData.ReferencePose == null ||
                constraintData.CurrentPose.Length < (int)HumanBodyBones.LastBone ||
                constraintData.ReferencePose.Length < (int)HumanBodyBones.LastBone)
            {
                if (GUILayout.Button("Setup pose arrays"))
                {
                    Undo.RecordObject(constraint, "Setup pose arrays");
                    constraint.data.SetupPoseArrays();
                }
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            DrawDefaultInspector();
        }
    }
}
