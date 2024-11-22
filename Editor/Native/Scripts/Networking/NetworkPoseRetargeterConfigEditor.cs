// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Retargeting.Editor;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.NativeUtilityPlugin;

namespace Meta.XR.Movement.Networking.Editor
{
    /// <summary>
    /// Custom editor for all network pose retargeter configs.
    /// </summary>
    [CustomEditor(typeof(NetworkPoseRetargeterConfig), true), CanEditMultipleObjects]
    public class NetworkPoseRetargeterConfigEditor : UnityEditor.Editor
    {
        // Pose retargeter config fields.
        private SerializedProperty _config;
        private SerializedProperty _jointPairs;
        private SerializedProperty _shapePoseData;

        // Ownership.
        private SerializedProperty _ownership;

        // Host information.
        private SerializedProperty _compressionType;
        private SerializedProperty _useDeltaCompression;
        private SerializedProperty _useSyncInterval;
        private SerializedProperty _intervalToSendData;
        private SerializedProperty _intervalToSyncData;
        private SerializedProperty _positionThreshold;
        private SerializedProperty _rotationAngleThreshold;
        private SerializedProperty _shapeThreshold;
        private SerializedProperty _bodyIndicesToSync;
        private SerializedProperty _bodyIndicesToSend;
        private SerializedProperty _faceIndicesToSend;

        // Client information.
        private SerializedProperty _useInterpolation;
        private SerializedProperty _maxBufferSize;

        // General information.
        private SerializedProperty _objectsToHideUntilValid;


        private void OnEnable()
        {
            _config = serializedObject.FindProperty("_config");
            _jointPairs = serializedObject.FindProperty("_jointPairs");
            _shapePoseData = serializedObject.FindProperty("_shapePoseData");

            _ownership = serializedObject.FindProperty("_ownership");

            _compressionType = serializedObject.FindProperty("_compressionType");
            _useDeltaCompression = serializedObject.FindProperty("_useDeltaCompression");
            _useSyncInterval = serializedObject.FindProperty("_useSyncInterval");
            _intervalToSendData = serializedObject.FindProperty("_intervalToSendData");
            _intervalToSyncData = serializedObject.FindProperty("_intervalToSyncData");
            _positionThreshold = serializedObject.FindProperty("_positionThreshold");
            _rotationAngleThreshold = serializedObject.FindProperty("_rotationAngleThreshold");
            _shapeThreshold = serializedObject.FindProperty("_shapeThreshold");
            _bodyIndicesToSync = serializedObject.FindProperty("_bodyIndicesToSync");
            _bodyIndicesToSend = serializedObject.FindProperty("_bodyIndicesToSend");
            _faceIndicesToSend = serializedObject.FindProperty("_faceIndicesToSend");

            _useInterpolation = serializedObject.FindProperty("_useInterpolation");
            _maxBufferSize = serializedObject.FindProperty("_maxBufferSize");

            _objectsToHideUntilValid = serializedObject.FindProperty("_objectsToHideUntilValid");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var networkConfig = target as NetworkPoseRetargeterConfig;
            if (networkConfig == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_config);
            EditorGUILayout.PropertyField(_jointPairs);
            EditorGUILayout.PropertyField(_shapePoseData);
            if (GUILayout.Button("Load Config"))
            {
                PoseRetargeterConfigEditor.LoadConfig(serializedObject, networkConfig as PoseRetargeterConfig);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Networking", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_ownership);

            if (_ownership.intValue == (int)NetworkPoseRetargeterConfig.Ownership.Host ||
                _ownership.intValue == 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Delivery", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_compressionType);
                EditorGUILayout.PropertyField(_useDeltaCompression);
                EditorGUILayout.PropertyField(_useSyncInterval);
                EditorGUILayout.PropertyField(_intervalToSendData);
                EditorGUILayout.PropertyField(_intervalToSyncData);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_positionThreshold);
                EditorGUILayout.Slider(_rotationAngleThreshold, 0.0f, 360.0f);
                EditorGUILayout.PropertyField(_shapeThreshold);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Send Config", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_bodyIndicesToSync);
                EditorGUILayout.PropertyField(_bodyIndicesToSend);
                EditorGUILayout.PropertyField(_faceIndicesToSend);

                if ((_bodyIndicesToSend.arraySize == 0 && _bodyIndicesToSync.arraySize == 0 &&
                    _faceIndicesToSend.arraySize == 0) ||
                    GUILayout.Button("Fill default send indices"))
                {
                    if (networkConfig.NumberOfJoints > 0 && _bodyIndicesToSend != null &&
                        _bodyIndicesToSync != null)
                    {
                        _bodyIndicesToSync.arraySize = networkConfig.NumberOfJoints;
                        _bodyIndicesToSend.arraySize = networkConfig.NumberOfJoints;
                        for (var i = 0; i < networkConfig.NumberOfJoints; i++)
                        {
                            _bodyIndicesToSend.GetArrayElementAtIndex(i).intValue = i;
                            _bodyIndicesToSync.GetArrayElementAtIndex(i).intValue = i;
                        }
                    }

                    if (networkConfig.NumberOfShapes > 0)
                    {
                        var numShapes = networkConfig.NumberOfShapes;
                        _faceIndicesToSend.arraySize = numShapes;
                        for (int i = 0; i < numShapes; i++)
                        {
                            _faceIndicesToSend.GetArrayElementAtIndex(i).intValue = i;
                        }
                    }
                }
            }

            if (_ownership.intValue == (int)NetworkPoseRetargeterConfig.Ownership.Client || _ownership.intValue == 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Interpolation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_useInterpolation);
                EditorGUILayout.PropertyField(_maxBufferSize);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_objectsToHideUntilValid);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}
