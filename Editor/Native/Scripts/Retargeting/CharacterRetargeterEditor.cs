// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom editor for character retargeter.
    /// </summary>
    [CustomEditor(typeof(CharacterRetargeter), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class CharacterRetargeterEditor : CharacterRetargeterConfigEditor
    {
        private SerializedProperty _debugDrawSourceSkeleton;
        private SerializedProperty _debugDrawSourceSkeletonColor;
        private SerializedProperty _debugDrawTargetSkeleton;
        private SerializedProperty _debugDrawTargetSkeletonColor;
        private SerializedProperty _skeletonRetargeter;
        private SerializedProperty _validBodyTrackingDelay;

        private SerializedProperty _sourceProcessorContainers;
        private SerializedProperty _targetProcessorContainers;

        /// <inheritdoc />
        public override void OnEnable()
        {
            base.OnEnable();
            _debugDrawSourceSkeleton = serializedObject.FindProperty("_debugDrawSourceSkeleton");
            _debugDrawSourceSkeletonColor = serializedObject.FindProperty("_debugDrawSourceSkeletonColor");
            _debugDrawTargetSkeleton = serializedObject.FindProperty("_debugDrawTargetSkeleton");
            _debugDrawTargetSkeletonColor = serializedObject.FindProperty("_debugDrawTargetSkeletonColor");
            _skeletonRetargeter = serializedObject.FindProperty("_skeletonRetargeter");
            _validBodyTrackingDelay = serializedObject.FindProperty("_validBodyTrackingDelay");
            _sourceProcessorContainers = serializedObject.FindProperty("_sourceProcessorContainers");
            _targetProcessorContainers = serializedObject.FindProperty("_targetProcessorContainers");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            RenderConfig();
            RenderRetargeting();
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        /// <summary>
        /// Renders the retargeting section of the inspector GUI, including debugging options and retargeting settings.
        /// </summary>
        protected void RenderRetargeting()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugDrawSourceSkeleton);
            if (_debugDrawSourceSkeleton.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_debugDrawSourceSkeletonColor);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_debugDrawTargetSkeleton);
            if (_debugDrawTargetSkeleton.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_debugDrawTargetSkeletonColor);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Retargeting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_skeletonRetargeter);
            EditorGUILayout.PropertyField(_validBodyTrackingDelay);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_sourceProcessorContainers);
            EditorGUILayout.PropertyField(_targetProcessorContainers);
        }
    }
}
