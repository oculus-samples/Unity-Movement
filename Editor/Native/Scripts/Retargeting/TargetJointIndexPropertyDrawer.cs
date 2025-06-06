// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;
using UnityEditor;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for <see cref="TargetJointIndex"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetJointIndex))]
    public class TargetJointIndexPropertyDrawer : PropertyDrawer
    {
        // Cache joint names and indices
        private static string[] _jointNames;
        private static int[] _jointIndices;
        private static int _lastFrameUpdated;
        private static GameObject _lastSelectedGameObject;

        // Add a static event handler for selection changes
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            // If the selected GameObject changed, invalidate the cache
            if (Selection.activeGameObject == _lastSelectedGameObject)
            {
                return;
            }

            _lastSelectedGameObject = Selection.activeGameObject;
            _jointNames = null;
            _jointIndices = null;
        }

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indexProp = property.FindPropertyRelative("Index");

            // Update joint names if needed (only once per frame)
            if (_jointNames == null || _jointIndices == null ||
                _jointNames.Length == 0 || Time.frameCount > _lastFrameUpdated + 60)
            {
                UpdateJointNames();
            }

            // Joint index dropdown
            EditorGUI.BeginChangeCheck();
            var newIndex = _jointNames is { Length: > 0 }
                ? EditorGUI.IntPopup(position, indexProp.intValue, _jointNames, _jointIndices)
                : EditorGUI.IntField(position, indexProp.intValue);

            if (EditorGUI.EndChangeCheck())
            {
                indexProp.intValue = newIndex;
            }

            EditorGUI.EndProperty();
        }

        private static void UpdateJointNames()
        {
            var retargeter = Selection.activeGameObject?.GetComponent<CharacterRetargeter>();
            if (retargeter == null || retargeter.Config == null)
            {
                return;
            }

            CreateOrUpdateHandle(retargeter.Config, out var handle);
            try
            {
                GetJointNames(handle, SkeletonType.TargetSkeleton, out _jointNames);

                if (_jointNames is not { Length: > 0 })
                {
                    return;
                }

                _jointIndices = new int[_jointNames.Length];
                for (var i = 0; i < _jointIndices.Length; i++)
                {
                    _jointIndices[i] = i;
                }

                _lastFrameUpdated = Time.frameCount;
                _lastSelectedGameObject = Selection.activeGameObject;
            }
            finally
            {
                DestroyHandle(handle);
            }
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
