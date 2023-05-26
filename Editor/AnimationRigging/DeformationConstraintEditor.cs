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
            if (constraintData.ConstraintSkeleton == null)
            {
                if (GUILayout.Button("Find OVR Skeleton"))
                {
                    Undo.RecordObject(constraint, "Find OVR Skeleton");
                    var skeleton = constraint.GetComponentInParent<OVRSkeleton>();
                    constraint.data.AssignOVRSkeleton(skeleton);
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
