// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Add project setup validation tasks for MovementSDK Samples.
    /// </summary>
    [InitializeOnLoad]
    internal static class OVRProjectSetupMovementSDKSamplesTasks
    {
        /// <summary>
        /// All character and mirrored character layers should exist based on their indices.
        /// That is because the scene assets are assigned based on layer index.
        /// </summary>
        private static readonly int[] _expectedLayersIndices =
        {
            10, 11
        };

        /// <summary>
        /// The HiddenMesh layer is required for RecalculateNormals to function correctly.
        /// </summary>
        private static readonly string _hiddenMeshLayerName = "HiddenMesh";

        private const OVRProjectSetup.TaskGroup _group = OVRProjectSetup.TaskGroup.Features;

        static OVRProjectSetupMovementSDKSamplesTasks()
        {
            // Body tracking settings.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: _group,
                platform: BuildTargetGroup.Android,
                isDone: group => OVRRuntimeSettings.Instance.BodyTrackingFidelity == OVRPlugin.BodyTrackingFidelity2.High,
                message: "Body Tracking Fidelity should be set to High.",
                fix: group =>
                {
                    OVRRuntimeSettings.Instance.BodyTrackingFidelity = OVRPlugin.BodyTrackingFidelity2.High;
                    OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
                },
                fixMessage: "Set OVRRuntimeSettings.BodyTrackingFidelity = High"
            );

            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: _group,
                platform: BuildTargetGroup.Android,
                isDone: group => OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet == OVRPlugin.BodyJointSet.FullBody,
                message: "Body Tracking Joint Set should be set to Full Body.",
                fix: group =>
                {
                    OVRRuntimeSettings.Instance.BodyTrackingJointSet = OVRPlugin.BodyJointSet.FullBody;
                    OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
                },
                fixMessage: "Set OVRRuntimeSettings.BodyTrackingJointSet = FullBody"
            );

            // Test layers.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: _group,
                platform: BuildTargetGroup.Android,
                isDone: group => LayerMask.NameToLayer(_hiddenMeshLayerName) != -1,
                message: $"The layer '{_hiddenMeshLayerName}' needs to exist for Recalculate Normals to function properly.",
                fix: group =>
                {
                    int unusedLayer = -1;
                    for (int i = 0; i < 32; i++)
                    {
                        if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                        {
                            unusedLayer = i;
                            break;
                        }
                    }
                    if (LayerMask.NameToLayer(_hiddenMeshLayerName) == -1)
                    {
                        if (unusedLayer == -1)
                        {
                            Debug.LogError("All Layers are Used!");
                        }
                        else
                        {
                            SetLayerName(unusedLayer, "HiddenMesh");
                        }
                    }
                },
                fixMessage: $"Set an unused layer to '{_hiddenMeshLayerName}'."
            );
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: _group,
                platform: BuildTargetGroup.Android,
                isDone: group =>
                {
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
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));

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
    }
}
