// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Custom editor for late mirrored object.
    /// </summary>
    [CustomEditor(typeof(LateMirroredObject)), CanEditMultipleObjects]
    public class LateMirroredObjectEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var mirroredObject = (LateMirroredObject)target;
            if (GUILayout.Button("Get Mirrored Transform Pairs"))
            {
                var mirroredTransformPairs = serializedObject.FindProperty("_mirroredTransformPairs");
                mirroredTransformPairs.ClearArray();
                Undo.RecordObject(mirroredObject, "Get Mirrored Transform Pairs");

                var childTransforms = new List<Transform>(mirroredObject.OriginalTransform.GetComponentsInChildren<Transform>(true));
                foreach (var transform in childTransforms)
                {
                    var mirroredTransform =
                        mirroredObject.MirroredTransform.transform.FindChildRecursive(transform.name);
                    if (mirroredTransform != null)
                    {
                        mirroredTransformPairs.InsertArrayElementAtIndex(0);
                        var mirroredTransformPair = mirroredTransformPairs.GetArrayElementAtIndex(0);
                        mirroredTransformPair.FindPropertyRelative("OriginalTransform").objectReferenceValue = transform;
                        mirroredTransformPair.FindPropertyRelative("MirroredTransform").objectReferenceValue = mirroredTransform;
                        mirroredTransformPair.FindPropertyRelative("Name").stringValue = transform.name;
                    }
                    else
                    {
                        Debug.LogError($"Missing a mirrored transform for: {transform.name}");
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawDefaultInspector();
        }
    }
}
