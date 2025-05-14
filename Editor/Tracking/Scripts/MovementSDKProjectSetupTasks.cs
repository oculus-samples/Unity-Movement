// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Linq;
using Meta.XR.Movement.FaceTracking;
using Meta.XR.Movement.FaceTracking.Samples;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Add project setup validation tasks for MovementSDK Samples.
    /// </summary>
    [InitializeOnLoad]
    internal static class MovementSDKProjectSetupTasks
    {
        /// <summary>
        /// The HiddenMesh layer is required for RecalculateNormals to function correctly.
        /// </summary>
        private static readonly string _hiddenMeshLayerName = "HiddenMesh";

        private const OVRProjectSetup.TaskGroup _group = OVRProjectSetup.TaskGroup.Features;

        static MovementSDKProjectSetupTasks()
        {
            // Skin weights settings.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: _group,
                platform: BuildTargetGroup.Unknown,
                isDone: group => QualitySettings.skinWeights == SkinWeights.FourBones,
                message: "Four skin weights should be used to avoid skinning problems.",
                fix: group =>
                {
                    QualitySettings.skinWeights = SkinWeights.FourBones;
                },
                fixMessage: "Set quality settings skin weights to four bones."
            );

            // Body tracking settings.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: _group,
                platform: BuildTargetGroup.Unknown,
                isDone: group =>
                    OVRRuntimeSettings.Instance.BodyTrackingFidelity == OVRPlugin.BodyTrackingFidelity2.High,
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
                platform: BuildTargetGroup.Unknown,
                isDone: group =>
                    OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet == OVRPlugin.BodyJointSet.FullBody,
                message: "Body Tracking Joint Set should be set to Full Body.",
                fix: group =>
                {
                    OVRRuntimeSettings.Instance.BodyTrackingJointSet = OVRPlugin.BodyJointSet.FullBody;
                    OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
                },
                fixMessage: "Set OVRRuntimeSettings.BodyTrackingJointSet = FullBody"
            );

            // Face tracking settings.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: _group,
                platform: BuildTargetGroup.Unknown,
                isDone: group => FindComponentInScene<FaceDriver>() == null ||
                                 OVRRuntimeSettings.GetRuntimeSettings().RequestsAudioFaceTracking,
                message: "The audio to expression feature should be enabled.",
                fix: group =>
                {
                    OVRRuntimeSettings.Instance.RequestsAudioFaceTracking = true;
                    OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
                },
                fixMessage: "Set OVRRuntimeSettings.RequestsAudioFaceTracking = true"
            );

            // Test layers.
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: _group,
                platform: BuildTargetGroup.Unknown,
                isDone: group => FindComponentInScene<RecalculateNormals>() == null ||
                                  LayerMask.NameToLayer(_hiddenMeshLayerName) != -1,
                message:
                $"The layer '{_hiddenMeshLayerName}' needs to exist for Recalculate Normals to function properly.",
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
