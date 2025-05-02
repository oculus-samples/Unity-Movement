// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Retargeting.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Utility class for Movement SDK editor functionality.
    /// </summary>
    public class MSDKUtilityEditor
    {
        /// <summary>
        /// Prefix for movement samples one-click menus.
        /// </summary>
        private const string _movementMenuPath =
            "GameObject/Movement SDK/";

        private const string _movementAssetsPath =
            "Assets/Movement SDK/";

        /// <summary>
        /// Gets or creates metadata for a target GameObject.
        /// </summary>
        /// <param name="asset">The asset GameObject.</param>
        /// <param name="target">The target GameObject.</param>
        /// <returns>The metadata object for the target.</returns>
        public static MSDKUtilityEditorMetadata GetOrCreateMetadata(GameObject asset, GameObject target)
        {
            // Create metadata.
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var assetPathSansExtension = assetPath.Replace(Path.GetExtension(assetPath), string.Empty);
            var metadataAssetPath = assetPathSansExtension + "-metadata.asset";
            var metadataObj =
                AssetDatabase.LoadAssetAtPath<MSDKUtilityEditorMetadata>(metadataAssetPath);

            if (metadataObj == null)
            {
                metadataObj = MSDKUtilityEditorMetadata.FindMetadataObject(target);
                if (metadataObj == null)
                {
                    metadataObj = ScriptableObject.CreateInstance<MSDKUtilityEditorMetadata>();
                    metadataObj.Model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    AssetDatabase.CreateAsset(metadataObj, metadataAssetPath);
                }
            }

            if (metadataObj.ConfigJson == null)
            {
                var jsonPath = Path.ChangeExtension(metadataAssetPath.Replace("-metadata", string.Empty), ".json");
                metadataObj.ConfigJson = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                metadataObj.ConfigJson = CreateRetargetingConfig(
                    GetSkeletonRetargetingData("OVRSkeletonRetargetingData"),
                    metadataObj.Model,
                    null,
                    jsonPath);
                EditorUtility.SetDirty(metadataObj);
                AssetDatabase.SaveAssets();
            }

            return metadataObj;
        }

        /// <summary>
        /// Verifies the character retargeter and opens the retargeting editor.
        /// </summary>
        /// <param name="retargeter">The character retargeter to verify and edit.</param>
        public static void VerifyAndOpenRetargetingEditor(CharacterRetargeter retargeter)
        {
            // Spit out warning if the character isn't uniform scale.
            var rootJoint = retargeter.JointPairs[0].Joint;
            if (rootJoint.localScale != Vector3.one)
            {
                Debug.LogWarning("Character joints must be uniform scale for retargeting! Setting root joint scale to uniform scale.");
                rootJoint.localScale = Vector3.one;
            }

            // Open alignment scene.
            CreateAlignmentSceneFromHierarchy(retargeter.gameObject);
        }

        /// <summary>
        /// Adds a character retargeter to the selected GameObject.
        /// </summary>
        public static void AddCharacterRetargeter()
        {
            bool selectedMoreThanOneObject = Selection.objects.Length > 1;
            if (selectedMoreThanOneObject)
            {
                Debug.LogError("Can only create a retargeting configuration for a single object.");
                return;
            }

            var activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                Debug.LogError("An object must be selected.");
                return;
            }

            var asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(activeObject);
            if (asset == null)
            {
                Debug.LogError("Selected object isn't linked to a project asset.");
                return;
            }

            // Get metadata.
            var metadataObj = GetOrCreateMetadata(asset, activeObject);

            // Add the retargeter and config.
            var characterRetargeter = GetOrAddComponent<CharacterRetargeter>(activeObject);
            characterRetargeter.ConfigAsset = metadataObj.ConfigJson;
            CharacterRetargeterConfigEditor.LoadConfig(new SerializedObject(characterRetargeter), characterRetargeter);
            var ovrBody = GetOrAddComponent<OVRBody>(activeObject);
            ovrBody.ProvidedSkeletonType = OVRPlugin.BodyJointSet.FullBody;
            Undo.RecordObject(activeObject, "Add skeleton retargeting");
            using var serializedObject = new SerializedObject(characterRetargeter);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            // Open editor.
            VerifyAndOpenRetargetingEditor(characterRetargeter);
        }

        /// <summary>
        /// Creates a retargeting configuration for the selected GameObject.
        /// </summary>
        [MenuItem(_movementMenuPath + "Body Tracking/Create Retargeting Configuration")]
        public static void CreateRetargetingConfig()
        {
            CreateRetargetingConfig(
                GetSkeletonRetargetingData("OVRSkeletonRetargetingData"),
                Selection.activeGameObject);
        }

        /// <summary>
        /// Creates an alignment scene for the selected GameObject.
        /// </summary>
        [MenuItem(_movementAssetsPath + "Body Tracking/Open Retargeting Configuration Editor")]
        public static void CreateAlignmentScene()
        {
            bool selectedMoreThanOneObject = Selection.objects.Length > 1;
            if (selectedMoreThanOneObject)
            {
                Debug.LogError("Can only create alignment window for a single object.");
                return;
            }

            var activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                Debug.LogError("An object must be selected.");
                return;
            }

            CreateAlignmentScene(activeObject);
        }

        /// <summary>
        /// Creates an alignment scene from the hierarchy for the selected GameObject.
        /// </summary>
        [MenuItem(_movementMenuPath + "Body Tracking/Open Retargeting Configuration Editor")]
        public static void CreateAlignmentSceneFromHierarchy()
        {
            bool selectedMoreThanOneObject = Selection.objects.Length > 1;
            if (selectedMoreThanOneObject)
            {
                Debug.LogError("Can only edit the retargeting configuration for a single object.");
                return;
            }

            var activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                Debug.LogError("An object must be selected.");
                return;
            }

            CreateAlignmentSceneFromHierarchy(activeObject);
        }

        /// <summary>
        /// Creates an alignment scene for a target GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        /// <param name="customDataSourceName">Optional custom data source name.</param>
        public static void CreateAlignmentScene(GameObject target, string customDataSourceName = null)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            EnterAlignmentScene(assetPath, customDataSourceName);
        }

        /// <summary>
        /// Creates an alignment scene from the hierarchy for a target GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        public static void CreateAlignmentSceneFromHierarchy(GameObject target)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(target);
            if (prefab == null)
            {
                Debug.LogError("Selected object isn't linked to a project asset.");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(prefab);
            EnterAlignmentScene(assetPath);
        }

        /// <summary>
        /// Creates a retargeting configuration for a target GameObject.
        /// </summary>
        /// <param name="sourceData">The source skeleton data.</param>
        /// <param name="targetObject">The target GameObject.</param>
        /// <param name="customData">Optional custom skeleton data.</param>
        /// <param name="assetPath">Optional asset path for saving the configuration.</param>
        /// <returns>The created retargeting configuration as a TextAsset.</returns>
        public static TextAsset CreateRetargetingConfig(
            SkeletonData sourceData,
            GameObject targetObject,
            SkeletonData customData = null,
            string assetPath = "")
        {
            var target = customData;
            if (target == null)
            {
                target = CreateRetargetingData(targetObject);
                if (target == null)
                {
                    Debug.LogError("Error in creating retargeting data!");
                    return null;
                }
            }

            var skinnedMeshRenderers = GetValidSkinnedMeshRenderers(targetObject);
            var blendshapeNames = GetBlendshapeNames(skinnedMeshRenderers);

            var output = MSDKUtilityHelper.CreateRetargetingConfig(
                blendshapeNames, sourceData, target, targetObject.name);
            var prefabAssetPath = AssetDatabase.GetAssetPath(targetObject);
            if (string.IsNullOrEmpty(prefabAssetPath))
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(targetObject);
                if (prefab != null)
                {
                    prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
                }
            }
            if (string.IsNullOrEmpty(assetPath))
            {
                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    assetPath = EditorUtility.SaveFilePanelInProject(
                        "Save Retargeting Config",
                        targetObject.name,
                        "json",
                        "Save Retargeting Config");
                }
                else
                {
                    assetPath = EditorUtility.SaveFilePanelInProject(
                        "Save Retargeting Config",
                        targetObject.name,
                        "json",
                        "Save Retargeting Config",
                        prefabAssetPath);
                }
                if (string.IsNullOrEmpty(assetPath))
                {
                    return null;
                }
            }

            File.WriteAllText(assetPath, output);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        }

        /// <summary>
        /// Creates retargeting data for a target GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        /// <param name="utilityConfig">Optional utility configuration.</param>
        /// <returns>The created skeleton data.</returns>
        public static SkeletonData CreateRetargetingData(GameObject target,
            MSDKUtilityEditorConfig utilityConfig = null)
        {
            var isOvrSkeleton = target.name == "OVRSkeleton";
            var jointMapping =
                MSDKUtilityHelper.GetChildParentJointMapping(target, isOvrSkeleton, out var previousRootName);
            if (jointMapping == null)
            {
                return null;
            }

            Transform rootJoint = null;
            var index = 0;
            var jointCount = jointMapping.Keys.Count;
            var data = ScriptableObject.CreateInstance<SkeletonData>();
            data.Initialize(jointCount);

            foreach (var jointPair in jointMapping)
            {
                var joint = jointPair.Key;
                var parentJoint = jointPair.Value;
                data.Joints[index] = joint.name;
                if (parentJoint == null)
                {
                    data.ParentJoints[index] = string.Empty;
                    rootJoint = joint;
                }
                else
                {
                    data.ParentJoints[index] = parentJoint.name;
                }

                data.TPose[index] = new Pose(joint.position, joint.rotation);
                index++;
            }

            if (utilityConfig != null)
            {
                foreach (var joint in data.Joints)
                {
                    if (!utilityConfig.JointNames.Contains(joint))
                    {
                        data.RemoveJoint(joint);
                    }
                }
            }

            data.TPoseMin = data.TPose;
            data.TPoseMax = data.TPose;

            // Check if it's OVRSkeleton - if so, we should sort the array.
            if (isOvrSkeleton)
            {
                data.SortData();
            }
            else
            {
                // Restore the name of the root joint to its previous name.
                if (rootJoint != null)
                {
                    rootJoint.name = previousRootName;
                }
            }

            return data;
        }

        /// <summary>
        /// Finds a prefab by name in the project.
        /// </summary>
        /// <param name="assetName">The name of the prefab to find.</param>
        /// <returns>The found prefab GameObject, or null if not found.</returns>
        public static GameObject FindPrefab(string assetName)
        {
            var guids = AssetDatabase.FindAssets(assetName + " t: GameObject",
                new[] { "Assets", "Packages" });
            return guids is not { Length: > 0 }
                ? null
                : guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .FirstOrDefault(loadedAsset => loadedAsset.name == assetName);
        }

        /// <summary>
        /// Gets an existing component or adds a new one if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of component to get or add.</typeparam>
        /// <param name="go">The GameObject to get or add the component to.</param>
        /// <returns>The existing or newly added component.</returns>
        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (!comp)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// Gets skeleton retargeting data by asset name.
        /// </summary>
        /// <param name="assetName">The name of the skeleton data asset.</param>
        /// <returns>The skeleton data, or null if not found.</returns>
        public static SkeletonData GetSkeletonRetargetingData(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError("Cannot retrieve RetargetingBodyData if the asset name is null or empty.");
                return null;
            }
            var guids = AssetDatabase.FindAssets(assetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {assetName} cannot be found.");
                return null;
            }

            var pathToAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SkeletonData>(pathToAsset);
        }

        private static void EnterAlignmentScene(string assetPath, string customDataSourceName = null)
        {
            var newAlignmentStage = MSDKUtilityEditorStage.CreateInstanceOfStage(assetPath, customDataSourceName);
            StageUtility.GoToStage(newAlignmentStage, true);
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
