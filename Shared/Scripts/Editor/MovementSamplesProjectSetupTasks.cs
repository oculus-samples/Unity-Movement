// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement.Samples
{
    [InitializeOnLoad]
    internal static class MovementSamplesProjectSetupTasks
    {
        /// <summary>
        /// All character and mirrored character layers should exist based on their indices.
        /// That is because the scene assets are assigned based on layer index.
        /// </summary>
        private static readonly int[] _expectedLayersIndices = { 10, 11 };

        private const OVRProjectSetup.TaskGroup _group = OVRProjectSetup.TaskGroup.Features;

        static MovementSamplesProjectSetupTasks()
        {
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: _group,
                platform: BuildTargetGroup.Unknown,
                isDone: group =>
                {
                    if (FindComponentInScene<MovementSceneLoader>() == null)
                    {
                        return true;
                    }

                    foreach (var layerIndex in _expectedLayersIndices)
                    {
                        if (string.IsNullOrEmpty(LayerMask.LayerToName(layerIndex)))
                        {
                            return false;
                        }
                    }

                    return true;
                },
                message: "Layers 10 and 11 must be indexed for the samples to display correctly.",
                fix: group =>
                {
                    foreach (var expectedLayerIndex in _expectedLayersIndices)
                    {
                        if (string.IsNullOrEmpty(LayerMask.LayerToName(expectedLayerIndex)))
                        {
                            // Default layer names.
                            string newLayerName = expectedLayerIndex == 10 ? "Character" : "MirroredCharacter";
                            SetLayerName(expectedLayerIndex, newLayerName);
                        }
                    }
                },
                fixMessage: "Set layers 10 and 11 to be valid layers."
            );
        }

        private static void SetLayerName(int layer, string name)
        {
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));

            SerializedProperty it = tagManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "layers")
                {
                    it.GetArrayElementAtIndex(layer).stringValue = name;
                    break;
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        private static T FindComponentInScene<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            return rootGameObjects.FirstOrDefault(go => go.GetComponentInChildren<T>())?.GetComponentInChildren<T>();
        }
    }
}
