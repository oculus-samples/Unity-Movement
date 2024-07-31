// Copyright (c) Meta Platforms, Inc. and affiliates.

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
                Undo.RecordObject(mirroredObject, "Get Mirrored Transform Pairs");
                mirroredObject.SetUpMirroredTransformPairs(mirroredObject.OriginalTransform,
                    mirroredObject.MirroredTransform);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawDefaultInspector();
        }
    }
}
