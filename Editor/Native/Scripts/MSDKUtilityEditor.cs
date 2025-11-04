// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Retargeting.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Utility class for Movement SDK editor functionality.
    /// </summary>
    public abstract class MSDKUtilityEditor
    {
        /// <summary>
        /// Prefix for movement samples one-click menus.
        /// </summary>
        private const string _movementMenuPath = "GameObject/Movement SDK/";

        private const string _movementAssetsPath = "Assets/Movement SDK/";

        /// <summary>
        /// Gets or creates metadata for a target GameObject.
        /// </summary>\
        /// <param name="asset">The asset GameObject.</param>
        /// <param name="target">The target GameObject.</param>
        /// <param name="customDataSourceName">Optional custom data source name.</param>
        /// <returns>The metadata object for the target.</returns>
        public static MSDKUtilityEditorMetadata GetOrCreateMetadata(GameObject asset, GameObject target,
            string customDataSourceName = null)
        {
            // Get or create metadata asset.
            var assetPath = AssetDatabase.GetAssetPath(asset);

            // If the asset path is empty (not a project asset), try to find a valid humanoid rig
            if (string.IsNullOrEmpty(assetPath) && target != null)
            {
                // Check every direct child to see if it's a prefab or has an animator
                asset = null;
                foreach (Transform child in target.transform)
                {
                    var childGameObject = child.gameObject;

                    // Check if it's a prefab or has an animator
                    var isPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childGameObject) != null;
                    var hasAnimator = childGameObject.GetComponent<Animator>() != null &&
                                      childGameObject.GetComponent<Animator>().avatar != null;

                    if (!isPrefab && !hasAnimator)
                    {
                        continue;
                    }

                    // Get the corresponding source model asset
                    var sourceModelAsset = isPrefab
                        ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(childGameObject)
                        : childGameObject;

                    // Check if it's a valid humanoid hierarchy
                    if (sourceModelAsset == null || !IsValidHumanoidRig(sourceModelAsset))
                    {
                        continue;
                    }

                    // Use this object to create the metadata
                    assetPath = AssetDatabase.GetAssetPath(sourceModelAsset);
                    asset = sourceModelAsset;
                    break;
                }

                if (asset == null)
                {
                    throw new Exception("No asset path found for asset.");
                }
            }

            var assetPathExtension = Path.GetExtension(assetPath);
            var assetPathSansExtension = assetPath?.Replace(assetPathExtension, string.Empty);
            var metadataAssetPath = assetPathSansExtension + "-metadata.asset";
            var metadataObj =
                AssetDatabase.LoadAssetAtPath<MSDKUtilityEditorMetadata>(metadataAssetPath);

            if (metadataObj == null)
            {
                metadataObj = MSDKUtilityEditorMetadata.FindMetadataAsset(target);
                if (metadataObj == null)
                {
                    metadataObj = ScriptableObject.CreateInstance<MSDKUtilityEditorMetadata>();
                    metadataObj.Model =
                        AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    AssetDatabase.CreateAsset(metadataObj, metadataAssetPath);
                }
            }

            if (metadataObj.ConfigJson != null)
            {
                return metadataObj;
            }

            var jsonPath = Path.ChangeExtension(metadataAssetPath.Replace("-metadata", string.Empty), ".json");
            metadataObj.ConfigJson = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (metadataObj.ConfigJson == null)
            {
                // Create config immediately
                Debug.Log($"Config JSON not found at {jsonPath}. Creating config now.");

                var sourceData = !string.IsNullOrEmpty(customDataSourceName)
                    ? FindSourceSkeletonData(customDataSourceName)
                    : null;

                var configAsset = CreateRetargetingConfig(
                    targetObject: target,
                    customSourceData: sourceData,
                    customTargetData: null,
                    configPath: jsonPath,
                    promptForPath: false);

                if (configAsset != null)
                {
                    metadataObj.ConfigJson = configAsset;
                    EditorUtility.SetDirty(metadataObj);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Successfully created config at {jsonPath}");
                }
                else
                {
                    Debug.LogError($"Failed to create config at {jsonPath}");
                }
            }
            else
            {
                // Config already exists
                Debug.Log("Using existing config.");
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
                Debug.LogWarning(
                    "Character joints must be uniform scale for retargeting! Setting root joint scale to uniform scale.");
                rootJoint.localScale = Vector3.one;
            }

            // Open alignment scene.
            CreateAlignmentSceneFromHierarchy(retargeter.gameObject);
        }

        /// <summary>
        /// Adds a character retargeter to the active GameObject.
        /// </summary>
        public static void AddCharacterRetargeter(GameObject activeObject)
        {
            if (activeObject == null || !IsValidHumanoidRig(activeObject))
            {
                // Show dialog asking if user wants to choose a model
                var shouldChooseModel = EditorUtility.DisplayDialog(
                    "No Valid Character Model",
                    "The selected object is not a valid character rig. Would you like to choose a character model to use for retargeting and retry?",
                    "Choose Model",
                    "Cancel");

                if (!shouldChooseModel)
                {
                    throw new Exception(
                        "Character retargeter installation was cancelled: a model was not selected.");
                }

                // Open file selection panel when user chooses to select a model
                var selectedModel = SelectModelForRetargeting();
                if (selectedModel == null)
                {
                    throw new Exception(
                        "Character retargeter installation was cancelled: no valid model found or cancelled selection.");
                }

                // Set the selected game object to the instantiated one
                if (activeObject != null)
                {
                    selectedModel.transform.SetParent(activeObject.transform);
                }

                activeObject = selectedModel;
                Selection.activeGameObject = activeObject;
                EditorUtility.DisplayDialog("Added Character Model",
                    "The character model has been added to the scene. Please retry the building block installation again on the newly instantiated character model.",
                    "OK");

                // Throw exception to retry the building block installation
                throw new Exception(
                    "Please retry the building block installation again with the newly instantiated character model.");
            }

            // Check if the selected object corresponds to a model, then use the corresponding object from original source
            var asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(activeObject);

            // If it's not linked to a character model asset, proceed anyway
            if (asset == null)
            {
                Debug.LogWarning(
                    "Selected object isn't linked to a project asset. Proceeding with the current object.");
                asset = activeObject; // Use the current object as the asset
            }

            // Get metadata.
            var metadataObj = GetOrCreateMetadata(asset, activeObject);

            // Register undo for the complete object hierarchy before making changes
            Undo.RegisterCompleteObjectUndo(activeObject, "Add Character Retargeter");

            // Check if components already exist to register undo properly
            var hasCharacterRetargeter = activeObject.GetComponent<CharacterRetargeter>() != null;
            var hasDataProvider = activeObject.GetComponent<MetaSourceDataProvider>() != null;

            // Add the retargeter and config.
            var characterRetargeter = GetOrAddComponent<CharacterRetargeter>(activeObject);

            // Register the component addition with undo if it was newly created
            if (!hasCharacterRetargeter)
            {
                Undo.RegisterCreatedObjectUndo(characterRetargeter, "Add CharacterRetargeter Component");
            }

            characterRetargeter.ConfigAsset = metadataObj.ConfigJson;
            CharacterRetargeterConfigEditor.LoadConfig(new SerializedObject(characterRetargeter), characterRetargeter);

            var dataProvider = GetOrAddComponent<MetaSourceDataProvider>(activeObject);
            // Register the component addition with undo if it was newly created
            if (!hasDataProvider)
            {
                Undo.RegisterCreatedObjectUndo(dataProvider, "Add MetaSourceDataProvider Component");
            }

            dataProvider.ProvidedSkeletonType = OVRPlugin.BodyJointSet.FullBody;

            // Mark objects as dirty for serialization
            EditorUtility.SetDirty(characterRetargeter);
            EditorUtility.SetDirty(dataProvider);
            EditorUtility.SetDirty(activeObject);

            using var serializedObject = new SerializedObject(characterRetargeter);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        /// <summary>
        /// Opens a file selection panel to select a model for retargeting and creates an instance in the scene.
        /// </summary>
        /// <returns>The instantiated model GameObject, or null if selection was cancelled or failed.</returns>
        public static GameObject SelectModelForRetargeting()
        {
            var assetPath = EditorUtility.OpenFilePanel("Select Model for Retargeting",
                Application.dataPath,
                "fbx,obj,3ds,glb,prefab");
            if (string.IsNullOrEmpty(assetPath))
            {
                return null; // User cancelled selection
            }

            // Convert absolute path to project-relative path
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError($"Selected file is outside the project directory: {assetPath}");
                return null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"Could not load asset at path {assetPath}");
                return null;
            }

            // Instantiate the model in the scene
            var instanceName = "Retargeting_" + asset.name;
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            modelInstance.name = instanceName;
            // Register undo operation for the instantiation
            Undo.RegisterCreatedObjectUndo(modelInstance, "Add Character Retargeter to " + instanceName);
            // Select the new instance in the hierarchy
            Selection.activeGameObject = modelInstance;
            Debug.Log($"Created retargeting instance '{instanceName}' from model '{asset.name}'");
            return modelInstance;
        }

        /// <summary>
        /// Creates a retargeting configuration for the selected GameObject.
        /// </summary>
        [MenuItem(_movementMenuPath + "Body Tracking/Create Retargeting Configuration")]
        public static void CreateRetargetingConfig()
        {
            CreateRetargetingConfig(targetObject: Selection.activeGameObject);
        }

        /// <summary>
        /// Creates an alignment scene for the selected GameObject.
        /// </summary>
        [MenuItem(_movementAssetsPath + "Body Tracking/Open Retargeting Configuration Editor")]
        public static void CreateAlignmentScene()
        {
            var selectedMoreThanOneObject = Selection.objects.Length > 1;
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
            var selectedMoreThanOneObject = Selection.objects.Length > 1;
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

            // Check if the character has a retargeter component and config
            var characterRetargeter = activeObject.GetComponent<CharacterRetargeter>();
            if (characterRetargeter == null || characterRetargeter.ConfigAsset == null)
            {
                Debug.Log("Character doesn't have a retargeting configuration. Creating one...");

                // Create the retargeter and config first
                AddCharacterRetargeter(activeObject);
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
        /// Gets skeleton retargeting data by asset name or path.
        /// </summary>
        /// <param name="assetNameOrPath">The name or path of the skeleton data asset.</param>
        /// <returns>The skeleton data, or null if not found.</returns>
        public static SkeletonData FindSourceSkeletonData(string assetNameOrPath)
        {
            if (string.IsNullOrEmpty(assetNameOrPath))
            {
                return null;
            }

            // First, try to load by path if it looks like a path (contains '/' or '\')
            var projectPath = Path.GetFullPath(Application.dataPath + "/..");
            if (assetNameOrPath.Contains('/') || assetNameOrPath.Contains('\\'))
            {
                // Convert absolute path to project-relative path if needed
                var projectRelativePath = assetNameOrPath;

                // Check if this is an absolute path
                if (Path.IsPathRooted(assetNameOrPath))
                {
                    // Try to convert to a project-relative path
                    projectPath = projectPath.Replace('\\', '/');
                    var fullPath = Path.GetFullPath(assetNameOrPath).Replace('\\', '/');

                    if (fullPath.StartsWith(projectPath))
                    {
                        // Convert to project-relative path
                        projectRelativePath = fullPath.Substring(projectPath.Length).TrimStart('/');
                        Debug.Log($"Converting absolute path to project-relative path: {projectRelativePath}");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Path '{assetNameOrPath}' is outside the Unity project and cannot be loaded directly.");
                        // Fall through to name-based search
                        projectRelativePath = null;
                    }
                }

                if (projectRelativePath != null)
                {
                    var skeletonDataText = AssetDatabase.LoadAssetAtPath<TextAsset>(projectRelativePath);
                    if (skeletonDataText != null)
                    {
                        return SkeletonData.CreateFromConfig(skeletonDataText.text,
                            MSDKUtility.SkeletonType.SourceSkeleton);
                    }

                    // If path lookup failed, fall through to name-based search
                    Debug.LogWarning(
                        $"Could not load asset at path '{projectRelativePath}', falling back to name-based search.");
                }
            }

            // Fall back to name-based search using AssetDatabase.FindAssets, filtering for JSON files only
            var guids = AssetDatabase.FindAssets(assetNameOrPath + " t:TextAsset");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset '{assetNameOrPath}' cannot be found by name or path.");
                return null;
            }

            // If multiple assets found, filter by .json extension and prefer JSON files
            foreach (var guid in guids)
            {
                var pathToAsset = AssetDatabase.GUIDToAssetPath(guid);

                // Only process files with .json extension
                if (!pathToAsset.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(pathToAsset);
                if (asset != null)
                {
                    return SkeletonData.CreateFromConfig(asset.text, MSDKUtility.SkeletonType.SourceSkeleton);
                }
            }

            Debug.LogError($"No TextAsset asset found for '{assetNameOrPath}'.");
            return null;
        }

        /// <summary>
        /// Creates a retargeting configuration asset for a target GameObject.
        /// This unified method handles both source data and custom target data scenarios.
        /// </summary>
        /// <param name="targetObject">The target GameObject to create configuration for.</param>
        /// <param name="customSourceData">Optional source skeleton data. If null, will use default "OVRSkeletonRetargetingData".</param>
        /// <param name="customTargetData">Optional custom target skeleton data. If null, will generate from targetObject.</param>
        /// <param name="configPath">Optional path for saving the configuration. If null, will auto-generate or prompt user.</param>
        /// <param name="promptForPath">Whether to prompt user for save path when configPath is not provided.</param>
        /// <returns>The created retargeting configuration as a TextAsset.</returns>
        public static TextAsset CreateRetargetingConfig(
            GameObject targetObject,
            SkeletonData customSourceData = null,
            SkeletonData customTargetData = null,
            string configPath = null,
            bool promptForPath = true)
        {
            if (targetObject == null)
            {
                Debug.LogError("Cannot create config: Target object is null");
                return null;
            }
            targetObject.transform.GetPositionAndRotation(out var originalPos, out var originalRot);
            targetObject.transform.position = Vector3.zero;
            targetObject.transform.rotation = Quaternion.identity;

            // Get or create source data
            if (customSourceData == null)
            {
                // If we have an existing config, extract source data from it instead of searching for the source config
                if (!string.IsNullOrEmpty(configPath))
                {
                    var existingConfigAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
                    if (existingConfigAsset != null)
                    {
                        customSourceData = SkeletonData.CreateFromConfig(existingConfigAsset.text,
                            MSDKUtility.SkeletonType.SourceSkeleton);
                    }
                }

                // Only search for SkeletonData asset if we don't have existing config or extraction failed
                if (customSourceData == null)
                {
                    customSourceData = FindSourceSkeletonData("OVRSkeletonData");
                    if (customSourceData == null)
                    {
                        Debug.LogError($"Failed to load source skeleton data.");
                        return null;
                    }
                }
            }

            // Get or create target data
            var targetData = customTargetData ?? SkeletonData.CreateFromTransform(targetObject.transform);

            // Get blendshape data
            var skinnedMeshRenderers = GetValidSkinnedMeshRenderers(targetObject);
            var blendshapeNames = GetBlendshapeNames(skinnedMeshRenderers);

            // Create the configuration JSON
            var configJsonString = MSDKUtilityHelper.CreateRetargetingConfig(
                blendshapeNames, customSourceData, targetData, targetObject.name);
            if (string.IsNullOrEmpty(configJsonString))
            {
                Debug.LogError("Error in creating retargeting config!");
                return null;
            }

            // Determine save path
            var finalConfigPath = DetermineConfigPath(targetObject, configPath, promptForPath);
            if (string.IsNullOrEmpty(finalConfigPath))
            {
                return null; // User cancelled or path determination failed
            }

            // Write and create asset
            File.WriteAllText(finalConfigPath, configJsonString);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(finalConfigPath);
            if (configAsset != null)
            {
                EditorUtility.SetDirty(configAsset);
                AssetDatabase.SaveAssets();
            }

            targetObject.transform.SetPositionAndRotation(originalPos, originalRot);
            return configAsset;
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
        /// Gets a processor from a CharacterRetargeter at the index specified in the property path.
        /// </summary>
        /// <typeparam name="T">The type of processor to get.</typeparam>
        /// <param name="property">The serialized property containing the processor.</param>
        /// <param name="isSourceProcessor">Whether the processor is a source processor (true) or target processor (false).</param>
        /// <param name="processor">The processor if found, otherwise null.</param>
        /// <returns>True if the processor was found, otherwise false.</returns>
        public static bool TryGetProcessorAtPropertyPathIndex<T>(SerializedProperty property, bool isSourceProcessor,
            out T processor) where T : class
        {
            processor = null;
            var retargeter = Selection.activeGameObject?.GetComponent<CharacterRetargeter>();
            if (retargeter == null)
            {
                Debug.LogError("Cannot find CharacterRetargeter component on the selected GameObject.");
                return false;
            }

            int indexOfSerializedProperty = MSDKUtilityHelper.GetIndexFromPropertyPath(property.propertyPath);
            var sourceContainers = retargeter.SourceProcessorContainers;
            var targetContainers = retargeter.TargetProcessorContainers;
            int numProcessors = isSourceProcessor ? sourceContainers.Length : targetContainers.Length;

            if (indexOfSerializedProperty < 0 || indexOfSerializedProperty >= numProcessors)
            {
                Debug.LogError($"Index of serialized processor is invalid: {indexOfSerializedProperty}. " +
                               $"Valid range is 0-{numProcessors - 1}.");
                return false;
            }

            if (isSourceProcessor)
            {
                var sourceCurrentProcessor = sourceContainers[indexOfSerializedProperty].GetCurrentProcessor();
                if (sourceCurrentProcessor is not T sourceTypedProcessor)
                {
                    Debug.LogError($"Processor at {indexOfSerializedProperty} is not of type {typeof(T).Name}.");
                    return false;
                }

                processor = sourceTypedProcessor;
                return true;
            }
            else
            {
                var currentTargetProcessor = targetContainers[indexOfSerializedProperty].GetCurrentProcessor();
                if (currentTargetProcessor is not T typedProcessor)
                {
                    Debug.LogError($"Processor at {indexOfSerializedProperty} is not of type {typeof(T).Name}.");
                    return false;
                }

                processor = typedProcessor;
            }

            return true;
        }

        /// <summary>
        /// Finds an ISDK hand GameObject based on handedness using hierarchy-based name detection.
        /// </summary>
        /// <param name="isLeftHand">True to find left hand, false to find right hand.</param>
        /// <returns>The found hand GameObject, or null if not found.</returns>
        public static GameObject FindISDKHand(bool isLeftHand)
        {
#if ISDK_DEFINED
            // First search for HandVisual components
            var handVisuals = Object.FindObjectsByType<Oculus.Interaction.HandVisual>(FindObjectsSortMode.None);
            foreach (var handVisual in handVisuals)
            {
                if (isLeftHand && IsLeftHand(handVisual.gameObject))
                {
                    return handVisual.gameObject;
                }
                else if (!isLeftHand && IsRightHand(handVisual.gameObject))
                {
                    return handVisual.gameObject;
                }
            }

            // If we didn't find the hand from HandVisual, search for SyntheticHand
            var synthHands = Object.FindObjectsByType<Oculus.Interaction.Input.SyntheticHand>(FindObjectsSortMode.None);
            foreach (var synthHand in synthHands)
            {
                if (isLeftHand && IsLeftHand(synthHand.gameObject))
                {
                    return synthHand.gameObject;
                }

                if (!isLeftHand && IsRightHand(synthHand.gameObject))
                {
                    return synthHand.gameObject;
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// Validates if the selected object or its children contain a valid humanoid animation rig.
        /// Looks for any transform with 3 children that each have at least 2 levels of hierarchy depth.
        /// </summary>
        /// <param name="targetObject">The GameObject to validate.</param>
        /// <returns>True if the object contains a valid humanoid rig structure.</returns>
        private static bool IsValidHumanoidRig(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return false;
            }

            // Recursively search through all transforms in the hierarchy
            return ValidateHumanoidHierarchyRecursive(targetObject.transform);
        }

        /// <summary>
        /// Recursively validates transforms in the hierarchy for humanoid rig structure.
        /// Checks for structural hierarchy without relying on naming conventions.
        /// </summary>
        /// <param name="transform">The transform to check and recurse through.</param>
        /// <returns>True if this transform or any of its children has a valid humanoid hierarchy.</returns>
        private static bool ValidateHumanoidHierarchyRecursive(Transform transform)
        {
            // Check if current transform has greater 3 children with required depth
            if (transform.childCount >= 3)
            {
                int validChildrenCount = 0;
                foreach (Transform child in transform)
                {
                    if (HasMinimumHierarchyDepth(child, 2))
                    {
                        validChildrenCount++;
                    }
                }

                // If all 3 children have the required hierarchy depth
                if (validChildrenCount >= 3)
                {
                    return true;
                }
            }

            // Recursively check all children
            foreach (Transform child in transform)
            {
                if (ValidateHumanoidHierarchyRecursive(child))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a transform has the minimum required hierarchy depth.
        /// </summary>
        /// <param name="transform">The transform to check.</param>
        /// <param name="minDepth">The minimum depth required (1 = has child, 2 = has child and grandchild, etc.).</param>
        /// <returns>True if the transform has at least the minimum hierarchy depth.</returns>
        private static bool HasMinimumHierarchyDepth(Transform transform, int minDepth)
        {
            if (minDepth <= 0)
            {
                return true;
            }

            if (transform.childCount == 0)
            {
                return false;
            }

            if (minDepth == 1)
            {
                return true;
            }

            // Check if any child has the remaining depth
            foreach (Transform child in transform)
            {
                if (HasMinimumHierarchyDepth(child, minDepth - 1))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a GameObject represents a left hand based on hierarchy naming.
        /// </summary>
        /// <param name="handObject">The GameObject to check.</param>
        /// <returns>True if the GameObject represents a left hand.</returns>
        private static bool IsLeftHand(GameObject handObject)
        {
            return ContainsLeftInHierarchy(handObject);
        }

        /// <summary>
        /// Determines if a GameObject represents a right hand based on hierarchy naming.
        /// </summary>
        /// <param name="handObject">The GameObject to check.</param>
        /// <returns>True if the GameObject represents a right hand.</returns>
        private static bool IsRightHand(GameObject handObject)
        {
            return ContainsRightInHierarchy(handObject);
        }

        /// <summary>
        /// Checks if "left" appears anywhere in the GameObject's hierarchy (self, parents, or children).
        /// </summary>
        /// <param name="obj">The GameObject to check.</param>
        /// <returns>True if "left" is found in the hierarchy.</returns>
        private static bool ContainsLeftInHierarchy(GameObject obj)
        {
            // Check the object's name
            if (obj.name.ToLower().Contains("left"))
                return true;

            // Check parent names
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("left"))
                    return true;
                parent = parent.parent;
            }

            // Check child names
            return CheckChildrenForLeft(obj.transform);
        }

        /// <summary>
        /// Checks if "right" appears anywhere in the GameObject's hierarchy (self, parents, or children).
        /// </summary>
        /// <param name="obj">The GameObject to check.</param>
        /// <returns>True if "right" is found in the hierarchy.</returns>
        private static bool ContainsRightInHierarchy(GameObject obj)
        {
            // Check the object's name
            if (obj.name.ToLower().Contains("right"))
                return true;

            // Check parent names
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("right"))
                    return true;
                parent = parent.parent;
            }

            // Check child names
            return CheckChildrenForRight(obj.transform);
        }

        /// <summary>
        /// Recursively checks children for "left" in their names.
        /// </summary>
        /// <param name="parent">The parent transform to check children of.</param>
        /// <returns>True if "left" is found in any child name.</returns>
        private static bool CheckChildrenForLeft(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLower().Contains("left"))
                    return true;
                if (CheckChildrenForLeft(child))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Recursively checks children for "right" in their names.
        /// </summary>
        /// <param name="parent">The parent transform to check children of.</param>
        /// <returns>True if "right" is found in any child name.</returns>
        private static bool CheckChildrenForRight(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLower().Contains("right"))
                    return true;
                if (CheckChildrenForRight(child))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an alignment scene from the hierarchy for a target GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        private static void CreateAlignmentSceneFromHierarchy(GameObject target)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(target);
            if (prefab == null)
            {
                Debug.LogError(
                    "Selected object isn't linked to a project asset. Please create a prefab from the object, and try again on the created prefab.");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(prefab);
            EnterAlignmentScene(assetPath);
        }

        /// <summary>
        /// Determines the configuration path for saving, handling auto-generation and user prompts.
        /// </summary>
        /// <param name="targetObject">The target GameObject.</param>
        /// <param name="configPath">Optional explicit config path.</param>
        /// <param name="promptForPath">Whether to prompt user for path selection.</param>
        /// <returns>The determined config path, or null if cancelled/failed.</returns>
        private static string DetermineConfigPath(GameObject targetObject, string configPath, bool promptForPath)
        {
            // If explicit path provided, use it
            if (!string.IsNullOrEmpty(configPath))
            {
                return configPath;
            }

            // Get prefab asset path for context
            var prefabAssetPath = AssetDatabase.GetAssetPath(targetObject);
            if (string.IsNullOrEmpty(prefabAssetPath))
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(targetObject);
                if (prefab != null)
                {
                    prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
                }
            }

            // If not prompting, auto-generate path
            if (!promptForPath)
            {
                if (!string.IsNullOrEmpty(prefabAssetPath))
                {
                    return Path.ChangeExtension(prefabAssetPath, ".json");
                }
                else
                {
                    // Fallback to Assets folder with object name
                    return Path.Combine("Assets", targetObject.name + ".json");
                }
            }

            // Prompt user for path
            string directoryPath;
            string defaultFileName;

            if (string.IsNullOrEmpty(prefabAssetPath))
            {
                directoryPath = Application.dataPath;
                defaultFileName = targetObject.name;
            }
            else
            {
                // Construct full path and split into directory/file
                var fullPath = Application.dataPath.Replace("Assets", "") + prefabAssetPath;
                directoryPath = Path.GetDirectoryName(fullPath);
                defaultFileName = Path.GetFileNameWithoutExtension(fullPath);
            }

            return EditorUtility.SaveFilePanelInProject(
                "Save Retargeting Config",
                defaultFileName,
                "json",
                "Save Retargeting Config",
                directoryPath);
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
            HashSet<string> blendshapeNames = new HashSet<string>();
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
