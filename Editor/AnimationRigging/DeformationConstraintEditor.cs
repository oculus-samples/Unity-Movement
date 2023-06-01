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
                }
                if (GUILayout.Button("Find Animator"))
                {
                    Undo.RecordObject(constraint, "Find Animator");
                    var animator = constraint.GetComponentInParent<Animator>();
                    constraint.data.AssignAnimator(animator);
                }
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }
            DrawDefaultInspector();
        }
    }
}
