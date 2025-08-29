// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking
{
    /// <summary>
    /// Custom Editor for <see cref="VisemeDriver">.
    /// </summary>
    /// <remarks>
    /// Custom Editor for <see cref="VisemeDriver"> that:
    /// - Allows users to access and modify <see cref="SkinnedMeshRenderer"/> <see cref="FaceViseme[]"/> <see cref="OVRFaceExpressions"/> in <see cref="VisemeDriver"/>.
    /// - Creates buttons to auto-generate and clear blendshape mappings.
    /// - Allows users to modify the <see cref="FaceViseme"> for the blendshapes in the <see cref="SkinnedMeshRenderer"/>.
    /// </remarks>
    [CustomEditor(typeof(VisemeDriver))]
    public class VisemeDriverEditor : UnityEditor.Editor
    {
        private SerializedProperty _mappings;
        private SerializedProperty _mesh;
        private SerializedProperty _ovrExpressions;
        private bool _showBlendshapes = true;
        private bool _logErrorShown = false;
        private VisemeDriver _visemeDriver;

        protected virtual void OnEnable()
        {
            _visemeDriver = (VisemeDriver)target;
            _visemeDriver.VisemeMesh = _visemeDriver.GetComponent<SkinnedMeshRenderer>();

            _mappings = serializedObject.FindProperty("_visemeMapping");
            _mesh = serializedObject.FindProperty("_mesh");
            _ovrExpressions = serializedObject.FindProperty("_ovrFaceExpressions");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            var renderer = _visemeDriver.VisemeMesh;

            EditorGUILayout.PropertyField(_ovrExpressions, new GUIContent(nameof(OVRFaceExpressions)));
            EditorGUILayout.PropertyField(_mesh, new GUIContent(nameof(SkinnedMeshRenderer)));

            if (renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
            {
                if (_logErrorShown == false)
                {
                    Debug.LogError($"The mesh renderer on {target.name} must have blendshapes.");
                    _logErrorShown = true;
                }
                return;
            }
            else
            {
                if (_logErrorShown == true)
                {
                    _logErrorShown = false;
                }
            }

            if (_mappings.arraySize != renderer.sharedMesh.blendShapeCount)
            {
                _mappings.ClearArray();
                _mappings.arraySize = renderer.sharedMesh.blendShapeCount;
#if META_XR_CORE_V78_MIN
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
                {
                    _mappings.GetArrayElementAtIndex(i).intValue = (int)OVRFaceExpressions.FaceViseme.Invalid;
                }
#endif
            }

            _showBlendshapes = EditorGUILayout.BeginFoldoutHeaderGroup(_showBlendshapes, "Blendshapes");

            if (_showBlendshapes)
            {
                if (GUILayout.Button("Auto Generate Mapping"))
                {
                    _visemeDriver.AutoMapBlendshapes();
                    Refresh(_visemeDriver);
                }

                if (GUILayout.Button("Clear Mapping"))
                {
                    _visemeDriver.ClearBlendshapes();
                    Refresh(_visemeDriver);
                }

                EditorGUILayout.Space();

                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
                {
                    EditorGUILayout.PropertyField(_mappings.GetArrayElementAtIndex(i),
                        new GUIContent(renderer.sharedMesh.GetBlendShapeName(i)));
                }
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            static void Refresh(VisemeDriver face)
            {
                EditorUtility.SetDirty(face);
                PrefabUtility.RecordPrefabInstancePropertyModifications(face);
                EditorSceneManager.MarkSceneDirty(face.gameObject.scene);
            }
        }
    }
}
