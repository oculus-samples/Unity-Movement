// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using Oculus.Movement.Utils;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for <see cref="RetargetingLayer"/>.
    /// </summary>
    [CustomEditor(typeof(RetargetingLayer)), CanEditMultipleObjects]
    public class RetargetingLayerEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var retargetingLayer = serializedObject.targetObject as RetargetingLayer;
            var animatorComponent = retargetingLayer.GetComponent<Animator>();
            var animatorProperlyConfigured = IsAnimatorProperlyConfigured(animatorComponent);

            if (!animatorProperlyConfigured)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Requires Animator component with a humanoid " +
                    "avatar, and Translation DoF enabled in avatar's Muscles & Settings.", MessageType.Error);
                GUILayout.EndVertical();
            }

            base.OnInspectorGUI();

            if (animatorProperlyConfigured)
            {
                if (GUILayout.Button("Calculate Adjustments"))
                {
                    AddComponentsHelper.AddJointAdjustments(animatorComponent, retargetingLayer);
                    EditorUtility.SetDirty(retargetingLayer);
                }

                if (GUILayout.Button("Update to Animator Pose"))
                {
                    AnimationUtilities.UpdateToAnimatorPose(animatorComponent,
                        false);
                    EditorUtility.SetDirty(retargetingLayer);
                }
            }

            if (GUILayout.Button("Save to JSON"))
            {
                SaveToJSON(retargetingLayer);
            }
            if (GUILayout.Button("Read from JSON"))
            {
                ReadFromJSON(retargetingLayer);
            }

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();
        }

        private static bool IsAnimatorProperlyConfigured(Animator animatorComponent)
        {
            return animatorComponent != null && animatorComponent.avatar != null &&
                animatorComponent.avatar.isHuman &&
                animatorComponent.avatar.humanDescription.hasTranslationDoF;
        }

        private void SaveToJSON(RetargetingLayer retargetingLayer)
        {
            try
            {
                var jsonPath = EditorUtility.SaveFilePanel(
                    "Save configuration into JSON",
                    Application.dataPath,
                    "",
                    "json");
                if (string.IsNullOrEmpty(jsonPath))
                {
                    return;
                }
                var jsonResult = retargetingLayer.GetJSONConfig();
                File.WriteAllText(jsonPath, jsonResult);
                Debug.Log($"Wrote JSON config to path {jsonPath}.");
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save retargeting layer to JSON, exception: {exception}");
            }
        }

        private void ReadFromJSON(RetargetingLayer retargetingLayer)
        {
            try
            {
                var jsonPath = EditorUtility.OpenFilePanel(
                    "Load configuration from JSON",
                    Application.dataPath,
                    "json");
                if (string.IsNullOrEmpty(jsonPath))
                {
                    return;
                }
                Undo.RecordObject(retargetingLayer, "Read retargeting layer data");
                retargetingLayer.ReadJSONConfigFromFile(jsonPath);
                EditorUtility.SetDirty(target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not read retargeting layer from JSON, exception: {exception}");
            }
        }
    }
}
