// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Meta.XR.Movement.Editor;
using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Retargeting.Editor;
using UnityEditor;
using UnityEngine;
#if MOVEMENT_DEFINED
using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Utils;
#endif

namespace Meta.XR.Movement.Networking.Editor
{
    /// <summary>
    /// Custom editor for all network pose retargeter spawners.
    /// </summary>
    public class NetworkPoseRetargeterSpawnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _loadCharacterWhenConnectedProperty;
        private SerializedProperty _poseRetargeterPrefabsProperty;
        private SerializedProperty _networkedCharacterProperty;
        private SerializedProperty _selectedCharacterIndexProperty;

        protected virtual string GetCharacterPrefabName()
        {
            return string.Empty;
        }

        private void OnEnable()
        {
            _loadCharacterWhenConnectedProperty = serializedObject.FindProperty("_loadCharacterWhenConnected");
            _poseRetargeterPrefabsProperty = serializedObject.FindProperty("_poseRetargeterPrefabs");
            _networkedCharacterProperty = serializedObject.FindProperty("_networkedCharacter");
            _selectedCharacterIndexProperty = serializedObject.FindProperty("_selectedCharacterIndex");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var prefabsSize = _poseRetargeterPrefabsProperty.arraySize;
            if (prefabsSize == 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Requires at least one valid character prefab!\nPlease add a valid " +
                                        "character prefab or use the button below to create one.", MessageType.Error);
                GUILayout.EndVertical();
            }
            else
            {
                for (var i = 0; i < prefabsSize; i++)
                {
                    var prefab =
                        _poseRetargeterPrefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (prefab == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox($"Requires a prefab at index {i}!", MessageType.Error);
                        GUILayout.EndVertical();
                        continue;
                    }

                    var config = prefab.GetComponentInChildren<NetworkPoseRetargeterConfig>(true);
                    if (config != null)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox($"Requires a valid retargeting config on the prefab " +
                                            $"{prefab.name} at index {i}!\n" +
                                            "Please set it up correctly or use the button below:", MessageType.Error);
                    GUILayout.EndVertical();
                    if (GUILayout.Button($"Update prefab {prefab.name} at index {i} with retargeting config"))
                    {
                        config = AddNetworkPoseRetargeterConfig(prefab);
                    }

                    if (config != null)
                    {
                        using var serializedConfig = new SerializedObject(config);
                        PoseRetargeterConfigEditor.LoadConfig(serializedConfig, config);
                    }
                }
            }

            if (GUILayout.Button("Validate networking settings"))
            {
                ValidateNetworkingSettings();
            }

            if (GUILayout.Button("Create valid character prefab from character model"))
            {
                var prefab = CreateCharacterPrefabFromModel();
                if (prefab != null)
                {
                    _poseRetargeterPrefabsProperty.InsertArrayElementAtIndex(prefabsSize);
                    _poseRetargeterPrefabsProperty.GetArrayElementAtIndex(prefabsSize).objectReferenceValue = prefab;
                }
            }

            EditorGUILayout.PropertyField(_loadCharacterWhenConnectedProperty);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_selectedCharacterIndexProperty);
            GUI.enabled = false;
            EditorGUILayout.LabelField($" Range: (0 ~ {prefabsSize - 1})");
            if (_selectedCharacterIndexProperty.intValue >= prefabsSize ||
                _selectedCharacterIndexProperty.intValue < 0)
            {
                _selectedCharacterIndexProperty.intValue = prefabsSize - 1;
            }
            GUI.enabled = true;
            GUILayout.EndVertical();
            _poseRetargeterPrefabsProperty.isExpanded = true;
            EditorGUILayout.PropertyField(_poseRetargeterPrefabsProperty);
            GUI.enabled = false;
            if (_networkedCharacterProperty.objectReferenceValue == null)
            {
                _networkedCharacterProperty.objectReferenceValue =
                    NativeUtilityEditor.FindPrefab(GetCharacterPrefabName());
            }

            EditorGUILayout.PropertyField(_networkedCharacterProperty);
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        /// <summary>
        /// Create the character prefab with all the required components from a model.
        /// </summary>
        /// <returns>The created character prefab.</returns>
        public static GameObject CreateCharacterPrefabFromModel()
        {
            var assetPath = EditorUtility.OpenFilePanel("Character Model Selection",
                Application.dataPath,
                "fbx,obj,3ds,glb,prefab");
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            assetPath = assetPath.Replace(Application.dataPath, "Assets");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"Could not load asset at path {assetPath}");
                return null;
            }
            var variantAssetPath = EditorUtility.SaveFilePanelInProject("Character Prefab Save Location",
                "Retargeted" + asset.name,
                "prefab",
                "Character Prefab Save Location");

            if (string.IsNullOrEmpty(variantAssetPath))
            {
                return null;
            }

            var assetPrefab = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            var animator = assetPrefab.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Characters require an animator!");
                return null;
            }

            if (!animator.isHuman)
            {
                Debug.LogError("Currently, only humanoid characters are supported. " +
                               "Please set the character to be imported as the Rig Type: Humanoid.");
                return null;
            }

            animator.applyRootMotion = false;
#if MOVEMENT_DEFINED
            var restPoseObjectHumanoid =
                AddComponentsHelper.GetRestPoseObject(AddComponentsHelper.CheckIfTPose(animator));
            HelperMenusBody.SetupCharacterForAnimationRiggingRetargetingConstraints(assetPrefab, restPoseObjectHumanoid,
                true, true);
            var variantAssetPrefabName = "Retargeted" + asset.name + ".prefab";
            var correctBonesProcessorPath =
                variantAssetPath.Replace(variantAssetPrefabName, asset.name + "CorrectBones.asset")
                    .Replace(Application.dataPath, "Assets");
            var correctHandsProcessorPath =
                variantAssetPath.Replace(variantAssetPrefabName, asset.name + "CorrectHands.asset")
                    .Replace(Application.dataPath, "Assets");
            var retargetingLayer = assetPrefab.GetComponent<RetargetingLayer>();
            var processors = retargetingLayer.RetargetingProcessors;
            foreach (var processor in processors)
            {
                var correctBonesProcessor = processor as RetargetingProcessorCorrectBones;
                var correctHandProcessor = processor as RetargetingProcessorCorrectHand;
                if (correctBonesProcessor != null)
                {
                    AssetDatabase.CreateAsset(correctBonesProcessor, correctBonesProcessorPath);
                }

                if (correctHandProcessor != null)
                {
                    AssetDatabase.CreateAsset(correctHandProcessor, correctHandsProcessorPath);
                }
            }

            retargetingLayer.enabled = false;
            assetPrefab.GetComponent<OVRBody>().enabled = false;
            animator.enabled = false;
#endif
            AddNetworkPoseRetargeterConfig(assetPrefab);
            var variantPrefab = PrefabUtility.SaveAsPrefabAsset(assetPrefab, variantAssetPath);
            DestroyImmediate(assetPrefab);
            AssetDatabase.Refresh();
            return variantPrefab;
        }

        protected virtual void ValidateNetworkingSettings()
        {
        }

        private static NetworkPoseRetargeterConfig AddNetworkPoseRetargeterConfig(GameObject prefab)
        {
            var networkConfig = prefab.AddComponent<NetworkPoseRetargeterConfig>();
            var configAsset = NativeUtilityEditor.CreateRetargetingConfig(prefab);
            using var serializedConfig = new SerializedObject(networkConfig);
            serializedConfig.FindProperty("_config").objectReferenceValue = configAsset;
            serializedConfig.ApplyModifiedProperties();
            PoseRetargeterConfigEditor.LoadConfig(serializedConfig, networkConfig);
            serializedConfig.ApplyModifiedProperties();
            serializedConfig.Update();
            return networkConfig;
        }
    }
}
