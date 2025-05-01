// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Movement.Editor;
using Meta.XR.Movement.Retargeting.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Editor
{
    /// <summary>
    /// Custom editor for all network character retargeter spawners.
    /// </summary>
    public class NetworkCharacterSpawnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _loadCharacterWhenConnectedProperty;
        private SerializedProperty _characterRetargeterPrefabsProperty;
        private SerializedProperty _networkedCharacterHandlerProperty;
        private SerializedProperty _selectedCharacterIndexProperty;

        protected virtual string GetCharacterHandlerPrefabName()
        {
            return string.Empty;
        }

        private void OnEnable()
        {
            _loadCharacterWhenConnectedProperty = serializedObject.FindProperty("_loadCharacterWhenConnected");
            _characterRetargeterPrefabsProperty = serializedObject.FindProperty("_characterRetargeterPrefabs");
            _networkedCharacterHandlerProperty = serializedObject.FindProperty("_networkedCharacterHandler");
            _selectedCharacterIndexProperty = serializedObject.FindProperty("_selectedCharacterIndex");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var prefabsSize = _characterRetargeterPrefabsProperty.arraySize;
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
                        _characterRetargeterPrefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (prefab == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox($"Requires a prefab at index {i}!", MessageType.Error);
                        GUILayout.EndVertical();
                        continue;
                    }

                    var config = prefab.GetComponentInChildren<NetworkCharacterRetargeter>(true);
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
                        config = AddNetworkCharacterRetargeter(prefab);
                    }

                    if (config != null)
                    {
                        using var serializedConfig = new SerializedObject(config);
                        CharacterRetargeterConfigEditor.LoadConfig(serializedConfig, config);
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
                    _characterRetargeterPrefabsProperty.InsertArrayElementAtIndex(prefabsSize);
                    _characterRetargeterPrefabsProperty.GetArrayElementAtIndex(prefabsSize).objectReferenceValue = prefab;
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
            _characterRetargeterPrefabsProperty.isExpanded = true;
            EditorGUILayout.PropertyField(_characterRetargeterPrefabsProperty);
            GUI.enabled = false;
            if (_networkedCharacterHandlerProperty.objectReferenceValue == null)
            {
                _networkedCharacterHandlerProperty.objectReferenceValue =
                    MSDKUtilityEditor.FindPrefab(GetCharacterHandlerPrefabName());
            }

            EditorGUILayout.PropertyField(_networkedCharacterHandlerProperty);
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
                "Network" + asset.name,
                "prefab",
                "Character Prefab Save Location",
                assetPath.Replace("Assets", Application.dataPath));

            if (string.IsNullOrEmpty(variantAssetPath))
            {
                return null;
            }

            var assetPrefab = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            AddNetworkCharacterRetargeter(assetPrefab);
            var variantPrefab = PrefabUtility.SaveAsPrefabAsset(assetPrefab, variantAssetPath);
            DestroyImmediate(assetPrefab);
            AssetDatabase.Refresh();
            return variantPrefab;
        }

        protected virtual void ValidateNetworkingSettings()
        {
        }

        private static NetworkCharacterRetargeter AddNetworkCharacterRetargeter(GameObject prefab)
        {
            // Get metadata.
            var asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);
            var metadataObj = MSDKUtilityEditor.GetOrCreateMetadata(asset, prefab);

            // Add the retargeter and config.
            var networkCharacterRetargeter = MSDKUtilityEditor.GetOrAddComponent<NetworkCharacterRetargeter>(prefab);
            networkCharacterRetargeter.ConfigAsset = metadataObj.ConfigJson;
            CharacterRetargeterConfigEditor.LoadConfig(new SerializedObject(networkCharacterRetargeter), networkCharacterRetargeter);
            var ovrBody = MSDKUtilityEditor.GetOrAddComponent<OVRBody>(prefab);
            ovrBody.ProvidedSkeletonType = OVRPlugin.BodyJointSet.FullBody;
            using var serializedObject = new SerializedObject(networkCharacterRetargeter);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            // Open editor.
            MSDKUtilityEditor.VerifyAndOpenRetargetingEditor(networkCharacterRetargeter);
            return networkCharacterRetargeter;
        }
    }
}
