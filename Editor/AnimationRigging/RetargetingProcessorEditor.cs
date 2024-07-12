// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for <see cref="RetargetingProcessor"/>.
    /// </summary>
    [CustomEditor(typeof(RetargetingProcessor), true), CanEditMultipleObjects]
    public class RetargetingProcessorEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var retargetingProcessor = serializedObject.targetObject as RetargetingProcessor;
            base.OnInspectorGUI();

            if (GUILayout.Button("Save to JSON"))
            {
                SaveToJSON(retargetingProcessor);
            }
            if (GUILayout.Button("Read from JSON"))
            {
                ReadFromJSON(retargetingProcessor);
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void SaveToJSON(RetargetingProcessor processor)
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
                var jsonResult = processor.GetJSONConfig();
                File.WriteAllText(jsonPath, jsonResult);
                Debug.Log($"Wrote JSON config to path {jsonPath}.");
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save retargeting processor to JSON, exception: {exception}");
            }
        }

        private void ReadFromJSON(RetargetingProcessor processor)
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
                Undo.RecordObject(processor, "Read processor data");
                processor.ReadJSONConfigFromFile(jsonPath);
                EditorUtility.SetDirty(target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not read retargeting processor from JSON, exception: {exception}");
            }
        }
    }
}
