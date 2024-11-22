// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

#define ENABLE_NATIVE_PLUGIN_API_TEST

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Meta.XR.Movement.Editor
{
    public class NativeUtilityEditor
    {
        /// <summary>
        /// Prefix for movement samples one-click menus.
        /// </summary>
        private const string _movementMenuPath =
            "GameObject/Movement SDK/";

        private const string _movementAssetsPath =
            "Assets/Movement SDK/";

        [MenuItem(_movementMenuPath + "Create Retargeting Config")]
        public static void CreateRetargetingConfig()
        {
            CreateRetargetingConfig(Selection.activeGameObject);
        }

        [MenuItem(_movementMenuPath + "Create Retargeting Data")]
        public static void CreateRetargetingDataAndSave()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("The gameObject with the skeleton for retargeting data must be selected!");
                return;
            }

            var data = CreateRetargetingData(Selection.activeGameObject);

            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Retargeting Data",
                Selection.activeGameObject.name,
                "asset",
                "Save Retargeted Data");
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        public static TextAsset CreateRetargetingConfig(GameObject targetObject)
        {
            var target = CreateRetargetingData(targetObject);
            if (target == null)
            {
                Debug.LogError("Error in creating retargeting data!");
                return null;
            }

            var source = GetOVRSkeletonRetargetingData();
            var skinnedMeshRenderers = GetValidSkinnedMeshRenderers(targetObject);
            var blendshapeNames = GetBlendshapeNames(skinnedMeshRenderers);

            var output = NativeUtilityHelper.CreateRetargetingConfig(
                blendshapeNames, source, target, targetObject.name);
            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Retargeting Config",
                targetObject.name,
                "json",
                "Save Retargeting Config");
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }
            File.WriteAllText(assetPath, output);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        }

        public static GameObject FindPrefab(string assetName)
        {
            var guids = AssetDatabase.FindAssets(assetName + " t: GameObject",
                new[] { "Assets", "Packages" });
            return guids is not { Length: > 0 }
                ? null
                : guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .FirstOrDefault(loadedAsset => loadedAsset.name == assetName);
        }

        private static RetargetingBodyData CreateRetargetingData(GameObject target)
        {
            var jointMapping =
                NativeUtilityHelper.GetChildParentJointMapping(target);
            if (jointMapping == null)
            {
                return null;
            }

            var data = ScriptableObject.CreateInstance<RetargetingBodyData>();
            var index = 0;
            var jointCount = jointMapping.Keys.Count;
            data.Initialize(jointCount);
            foreach (var jointPair in jointMapping)
            {
                var joint = jointPair.Key;
                var parentJoint = jointPair.Value;
                data.Joints[index] = joint.name;
                data.ParentJoints[index] = parentJoint == null ? string.Empty : parentJoint.name;
                data.TPose[index] = new Pose(joint.localPosition, joint.localRotation);
                index++;
            }
            // TODO: Replace with min T-Pose information when available from editor.
            data.TPoseMin = data.TPose;
            // TODO: Replace with max T-Pose information when available from editor.
            data.TPoseMax = data.TPose;

            // Check if it's OVRSkeleton - if so, we should sort the array.
            if (target.name == "OVRSkeleton")
            {
                data.SortData();
            }
            return data;
        }

        private static RetargetingBodyData GetOVRSkeletonRetargetingData()
        {
            var assetName = "OVRSkeletonRetargetingData";
            var guids = AssetDatabase.FindAssets(assetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {assetName} cannot be found.");
                return null;
            }
            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RetargetingBodyData>(pathToAsset);
        }

        private static SkinnedMeshRenderer[] GetValidSkinnedMeshRenderers(GameObject gameObject)
        {
            var allSkinnedMeshes = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<SkinnedMeshRenderer> skinnedMeshesWithBlendshapes = new List<SkinnedMeshRenderer>();
            foreach (var skinnedMesh in allSkinnedMeshes)
            {
                if (skinnedMesh.sharedMesh.blendShapeCount > 0)
                {
                    skinnedMeshesWithBlendshapes.Add(skinnedMesh);
                }
            }
            return skinnedMeshesWithBlendshapes.ToArray();
        }

        private static string[] GetBlendshapeNames(SkinnedMeshRenderer[] skinnedMeshes)
        {
            List<string> blendshapeNames = new List<string>();
            foreach (var skinnedMesh in skinnedMeshes)
            {
                int numCurrentBlendshapes = skinnedMesh.sharedMesh.blendShapeCount;
                for (int blendIndex = 0; blendIndex < numCurrentBlendshapes; blendIndex++)
                {
                    blendshapeNames.Add(skinnedMesh.sharedMesh.GetBlendShapeName(blendIndex));
                }
            }
            return blendshapeNames.ToArray();
        }

    }
}
