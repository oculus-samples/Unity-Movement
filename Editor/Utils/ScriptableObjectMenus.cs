// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    internal static class ScriptableObjectMenus
    {
        private const string _MOVEMENT_SAMPLES_MENU =
            "GameObject/Movement Samples/";
        private const string _SCRIPTABLE_OBJECTS_MENU =
            "Scriptable Objects/";

        private const string _SAVE_HUMANOID_REFERENCE_POSE =
            "Save Humanoid Reference Pose";

        private const string _SAVE_CUSTOM_HUMANOID_REFERENCE_POSE =
            "Save Custom Humanoid Reference Pose";

        private const string _MOVEMENT_SCRIPTABLE_OBJECTS_DIRECTORY =
            "MovementSDKSampleScriptableObjects";

        private const string _HUMANOID_REFERENCE_POSE_ASSET_NAME_SUFFIX =
            "_HumanoidReferencePose.asset";

        private const string _CUSTOM_HUMANOID_REFERENCE_POSE_ASSET_NAME_SUFFIX =
            "_CustomBindPose.asset";

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _SCRIPTABLE_OBJECTS_MENU + _SAVE_HUMANOID_REFERENCE_POSE)]
        private static void SaveHumanoidReferencePose()
        {
            try
            {
                var activeGameObject = Selection.activeGameObject;
                var activeObjectAnimator = activeGameObject.GetComponent<Animator>();
                if (activeObjectAnimator == null)
                {
                    throw new Exception($"Cannot create humanoid reference pose if {activeGameObject} " +
                        "does not have an animator");
                }

                var poseDataAsset = ScriptableObject.CreateInstance<RestPoseObjectHumanoid>();
                poseDataAsset.InitializePoseDataFromAnimator(activeObjectAnimator);

                string parentDirectory = Path.Combine("Assets", _MOVEMENT_SCRIPTABLE_OBJECTS_DIRECTORY);
                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                string selectedObjectName = activeGameObject.name;
                var assetPath = Path.Combine(parentDirectory,
                    $"{selectedObjectName}{_HUMANOID_REFERENCE_POSE_ASSET_NAME_SUFFIX}");

                if (File.Exists(assetPath))
                {
                    Debug.LogWarning($"{assetPath} already exists. Will overwrite it.");
                }

                AssetDatabase.CreateAsset(poseDataAsset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Saved reference pose as {assetPath}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save humanoid reference pose, reason: {exception}.");
            }
        }

        [MenuItem(_MOVEMENT_SAMPLES_MENU + _SCRIPTABLE_OBJECTS_MENU + _SAVE_CUSTOM_HUMANOID_REFERENCE_POSE)]
        private static void SaveCustomHumanoidReferencePose()
        {
            try
            {
                var activeGameObject = Selection.activeGameObject;
                var activeObjectAnimator = activeGameObject.GetComponent<Animator>();
                if (activeObjectAnimator == null)
                {
                    throw new Exception($"Cannot create custom humanoid reference pose if {activeGameObject} " +
                                        "does not have an animator");
                }

                var poseDataAsset = ScriptableObject.CreateInstance<BindPoseObjectSkeleton>();

                string parentDirectory = Path.Combine("Assets", _MOVEMENT_SCRIPTABLE_OBJECTS_DIRECTORY);
                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                string selectedObjectName = activeGameObject.name;
                var assetPath = Path.Combine(parentDirectory,
                    $"{selectedObjectName}{_CUSTOM_HUMANOID_REFERENCE_POSE_ASSET_NAME_SUFFIX}");

                if (File.Exists(assetPath))
                {
                    Debug.LogWarning($"{assetPath} already exists. Will overwrite it.");
                }

                AssetDatabase.CreateAsset(poseDataAsset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Saved reference pose as {assetPath}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save custom humanoid reference pose, reason: {exception}.");
            }
        }
    }
}
