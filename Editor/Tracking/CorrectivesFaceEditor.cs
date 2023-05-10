// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Custom Editor for <see cref="CorrectivesFace"/> component.
    /// </summary>
    [CustomEditor(typeof(CorrectivesFace))]
    public class CorrectivesFaceEditor : OVRCustomFaceEditor
    {
        private SerializedProperty _blendshapeModifier;
        private SerializedProperty _combinationShapesTextAsset;

        protected override void OnEnable()
        {
            base.OnEnable();
            _blendshapeModifier = serializedObject.FindProperty("_blendshapeModifier");
            _combinationShapesTextAsset = serializedObject.FindProperty("_combinationShapesTextAsset");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.ObjectField(_blendshapeModifier,
                new GUIContent("Blend shape modifiers (optional)"));
            EditorGUILayout.ObjectField(_combinationShapesTextAsset,
                new GUIContent("Combination shapes text (Optional)"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
