// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Custom editor for late mirrored skeleton.
    /// </summary>
    [CustomEditor(typeof(LateMirroredSkeleton)), CanEditMultipleObjects]
    public class LateMirroredSkeletonEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var mirroredSkeleton = (LateMirroredSkeleton)target;
            if (mirroredSkeleton.MirroredSkeleton != null && mirroredSkeleton.OriginalSkeleton != null)
            {
                if (GUILayout.Button("Get Mirrored Bone Pairs"))
                {
                    var mirroredBonePairs = serializedObject.FindProperty("_mirroredBonePairs");
                    mirroredBonePairs.ClearArray();
                    Undo.RecordObject(mirroredSkeleton, "Get Mirrored Bone Pairs");

                    // Assign eyes.
                    var eyes = mirroredSkeleton.OriginalSkeleton.GetComponentsInChildren<OVREyeGaze>();
                    var eyeTransforms = eyes.Select(e => e.transform).ToList();
                    AssignMirroredBones(eyeTransforms, mirroredSkeleton, mirroredBonePairs);

                    // Find twists and legs.
                    var twistTransforms = new List<Transform>();
                    var legTransforms = new List<Transform>();
                    var childTransforms = mirroredSkeleton.OriginalSkeleton.CustomBones[0].GetComponentsInChildren<Transform>(true);
                    foreach (var childTransform in childTransforms)
                    {
                        if (childTransform.name.Contains("Twist"))
                        {
                            twistTransforms.Add(childTransform);
                        }
                        else if (childTransform.name.Contains("Leg") || childTransform.name.Contains("Foot"))
                        {
                            legTransforms.Add(childTransform);
                        }
                    }

                    // Assign twists.
                    AssignMirroredBones(twistTransforms, mirroredSkeleton, mirroredBonePairs);

                    // Assign legs.
                    AssignMirroredBones(legTransforms, mirroredSkeleton, mirroredBonePairs);

                    serializedObject.ApplyModifiedProperties();
                }
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }
            DrawDefaultInspector();
        }

        private void AssignMirroredBones(List<Transform> originalBones, LateMirroredSkeleton mirroredSkeleton, SerializedProperty mirroredBonePairs)
        {
            foreach (var originalBone in originalBones)
            {
                var mirroredTransform =
                    mirroredSkeleton.MirroredSkeleton.transform.FindChildRecursive(originalBone.name);
                if (mirroredTransform != null)
                {
                    mirroredBonePairs.InsertArrayElementAtIndex(0);
                    var mirroredBonePair = mirroredBonePairs.GetArrayElementAtIndex(0);
                    mirroredBonePair.FindPropertyRelative("OriginalBone").objectReferenceValue = originalBone;
                    mirroredBonePair.FindPropertyRelative("MirroredBone").objectReferenceValue = mirroredTransform;
                    mirroredBonePair.FindPropertyRelative("Name").stringValue = originalBone.name;

                    // Mark these bones to be re-parented.
                    if (originalBone.name == "LeftLegUpper" || originalBone.name == "RightLegUpper")
                    {
                        mirroredBonePair.FindPropertyRelative("ShouldBeReparented").boolValue = true;
                    }
                    else
                    {
                        mirroredBonePair.FindPropertyRelative("ShouldBeReparented").boolValue = false;
                    }
                }
                else
                {
                    Debug.LogError($"Missing a mirrored transform for: {originalBone.name}");
                }
            }
        }
    }
}
