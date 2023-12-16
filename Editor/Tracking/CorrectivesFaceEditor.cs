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
        private SerializedProperty _forceJawDropForTongue;
        private SerializedProperty _tongueOutThreshold;
        private SerializedProperty _minJawDrop;

        protected override void OnEnable()
        {
            base.OnEnable();
            _blendshapeModifier = serializedObject.FindProperty("_blendshapeModifier");
            _combinationShapesTextAsset = serializedObject.FindProperty("_combinationShapesTextAsset");
            _forceJawDropForTongue = serializedObject.FindProperty("_forceJawDropForTongue");
            _tongueOutThreshold = serializedObject.FindProperty("_tongueOutThreshold");
            _minJawDrop = serializedObject.FindProperty("_minJawDrop");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.ObjectField(_blendshapeModifier,
                new GUIContent("Blendshape Modifiers (Optional)"));
            EditorGUILayout.ObjectField(_combinationShapesTextAsset,
                new GUIContent("Combination Shapes Text (Optional)"));

            EditorGUILayout.PropertyField(_forceJawDropForTongue);
            if (_forceJawDropForTongue.boolValue)
            {
                EditorGUILayout.PropertyField(_tongueOutThreshold);
                EditorGUILayout.PropertyField(_minJawDrop);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
