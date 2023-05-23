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
                    var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
                    constraint.data.AssignOVRSkeleton(skeleton);
                }
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }
            DrawDefaultInspector();
        }
    }
}
