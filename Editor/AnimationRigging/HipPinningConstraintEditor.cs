// Copyright (c) Meta Platforms, Inc. and affiliates.

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
            if (constraintData.ConstraintSkeleton == null &&
                constraintData.AnimatorComponent == null)
            {
                if (GUILayout.Button("Find OVR Skeleton"))
                {
                    Undo.RecordObject(constraint, "Find OVR Skeleton");
                    var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
                    constraint.data.AssignOVRSkeleton(skeleton);
                    EditorUtility.SetDirty(target);
                }
                if (GUILayout.Button("Find Animator"))
                {
                    Undo.RecordObject(constraint, "Find Animator");
                    var animatorComp = constraint.GetComponentInParent<Animator>();
                    constraint.data.AssignAnimator(animatorComp);
                    EditorUtility.SetDirty(target);
                }
            }
            else if (!constraintData.ObtainedProperReferences)
            {
                if (GUILayout.Button("Set up data"))
                {
                    Undo.RecordObject(constraint, "Set up data");
                    constraint.data.SetUpBoneReferences();
                    EditorUtility.SetDirty(target);
                }
            }
            else if (constraintData.ObtainedProperReferences)
            {
                if (GUILayout.Button("Clear data"))
                {
                    Undo.RecordObject(constraint, "Clear data");
                    constraint.data.ClearSetupReferences();
                    EditorUtility.SetDirty(target);
                }
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawDefaultInspector();
        }
    }
}
