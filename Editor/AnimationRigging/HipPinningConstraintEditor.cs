// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the hip pinning constraint.
    /// </summary>
    [CustomEditor(typeof(HipPinningConstraint)), CanEditMultipleObjects]
    public class HipPinningConstraintEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var constraint = (HipPinningConstraint)target;
            IHipPinningData constraintData = constraint.data;
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
