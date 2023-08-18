// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the twist distribution constraint.
    /// </summary>
    [CustomEditor(typeof(GroundingConstraint)), CanEditMultipleObjects]
    public class GroundingConstraintEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var constraint = (GroundingConstraint)target;
            IGroundingData groundingData = constraint.data;

            if (groundingData.ConstraintSkeleton == null &&
                groundingData.ConstraintAnimator == null)
            {
                if (GUILayout.Button("Find Animator"))
                {
                    Undo.RecordObject(constraint, "Find Animator");
                    var animator = constraint.GetComponentInParent<Animator>();
                    constraint.data.AssignAnimator(animator);
                }
                if (GUILayout.Button("Find OVR Skeleton"))
                {
                    Undo.RecordObject(constraint, "Find OVR Skeleton");
                    var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
                    constraint.data.AssignOVRSkeleton(skeleton);
                }
            }

            if (groundingData.Hips == null)
            {
                if (GUILayout.Button("Find Hips"))
                {
                    Undo.RecordObject(constraint, "Find Hips");
                    FindHips(constraint);
                }
            }

            if (GUILayout.Button("Compute Offsets"))
            {
                Undo.RecordObject(constraint, "Compute Offsets");
                constraint.data.ComputeOffsets();
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawDefaultInspector();
        }

        private void FindHips(GroundingConstraint constraint)
        {
            IGroundingData groundingData = constraint.data;
            Transform hipsTransform = null;
            if (groundingData.ConstraintSkeleton != null)
            {
                hipsTransform = RiggingUtilities.FindBoneTransformFromCustomSkeleton(
                    groundingData.ConstraintSkeleton,
                    OVRSkeleton.BoneId.Body_Hips);
            }
            else
            {
                hipsTransform = RiggingUtilities.FindBoneTransformAnimator(
                    groundingData.ConstraintAnimator,
                    OVRSkeleton.BoneId.Body_Hips);
            }
            if (hipsTransform == null)
            {
                Debug.LogWarning("Could not find hips transform.");
            }
            constraint.data.AssignHips(hipsTransform);
        }
    }
}
