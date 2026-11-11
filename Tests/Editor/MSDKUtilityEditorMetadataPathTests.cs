// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Editor.Tests
{
    /// <summary>
    /// Verifies that creating MSDK retargeting metadata never overwrites an unrelated asset
    /// that already occupies the model's default "&lt;Model&gt;-metadata.asset" path.
    /// </summary>
    internal class MSDKUtilityEditorMetadataPathTests
    {
        private const string _folder = "Assets/MSDKMetadataPathTests_Temp";
        private const string _modelPath = _folder + "/Character.prefab";
        private const string _decoyPath = _folder + "/Character-metadata.asset";
        private const string _jsonPath = _folder + "/Character.json";
        private const string _payload = "user data";

        private GameObject _model;
        private GameObject _sceneInstance;
        private string _decoyGuid;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));

            var source = new GameObject("Character");
            _model = PrefabUtility.SaveAsPrefabAsset(source, _modelPath);
            Object.DestroyImmediate(source);

            var decoy = ScriptableObject.CreateInstance<MetadataDecoyAsset>();
            decoy.Payload = _payload;
            AssetDatabase.CreateAsset(decoy, _decoyPath);
            AssetDatabase.SaveAssets();
            _decoyGuid = AssetDatabase.AssetPathToGUID(_decoyPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneInstance != null)
            {
                Object.DestroyImmediate(_sceneInstance);
            }
            AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void FindOrCreateMetadataAsset_PathTakenByOtherAsset_KeepsOtherAsset()
        {
            var first = InvokeFindOrCreateMetadataAsset(_model);
            var second = InvokeFindOrCreateMetadataAsset(_model);

            AssertDecoyIntact();
            AssertNewMetadata(first);
            Assert.AreSame(first, second, "Second run should reuse the metadata created by the first run.");
        }

        [Test]
        public void GetOrCreateMetadata_PathTakenByOtherAsset_KeepsOtherAsset()
        {
            File.WriteAllText(_jsonPath, "{}");
            AssetDatabase.ImportAsset(_jsonPath);
            _sceneInstance = (GameObject)PrefabUtility.InstantiatePrefab(_model);

            var first = MSDKUtilityEditor.GetOrCreateMetadata(_model, _sceneInstance);
            var second = MSDKUtilityEditor.GetOrCreateMetadata(_model, _sceneInstance);

            AssertDecoyIntact();
            AssertNewMetadata(first);
            Assert.AreSame(first, second, "Second run should reuse the metadata created by the first run.");
        }

        private static MSDKUtilityEditorMetadata InvokeFindOrCreateMetadataAsset(GameObject asset)
        {
            var method = typeof(MSDKUtilityEditor).GetMethod("FindOrCreateMetadataAsset",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "MSDKUtilityEditor.FindOrCreateMetadataAsset not found.");
            return (MSDKUtilityEditorMetadata)method.Invoke(null, new object[] { asset });
        }

        private void AssertDecoyIntact()
        {
            var decoy = AssetDatabase.LoadAssetAtPath<MetadataDecoyAsset>(_decoyPath);
            Assert.IsNotNull(decoy, $"{_decoyPath} was replaced by a different asset type.");
            Assert.AreEqual(_payload, decoy.Payload);
            Assert.AreEqual(_decoyGuid, AssetDatabase.AssetPathToGUID(_decoyPath));
        }

        private void AssertNewMetadata(MSDKUtilityEditorMetadata metadata)
        {
            Assert.IsNotNull(metadata);
            Assert.AreNotEqual(_decoyPath, AssetDatabase.GetAssetPath(metadata));
            Assert.AreEqual(_model, metadata.Model);
            Assert.AreEqual(1, AssetDatabase.FindAssets($"t:{nameof(MSDKUtilityEditorMetadata)}", new[] { _folder }).Length,
                "Expected exactly one metadata asset for the model.");
        }
    }
}
