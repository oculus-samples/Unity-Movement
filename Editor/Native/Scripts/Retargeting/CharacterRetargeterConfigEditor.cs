// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using Meta.XR.Movement.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.Retargeting.CharacterRetargeterConfig;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom editor for all character retargeter configs.
    /// </summary>
    [CustomEditor(typeof(CharacterRetargeterConfig), editorForChildClasses: true)]
    public class CharacterRetargeterConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _jointPairs;
        private SerializedProperty _shapePoseData;

        /// <summary>
        /// Grab serialized properties from the serialized object.
        /// </summary>
        public virtual void OnEnable()
        {
            _config = serializedObject.FindProperty("_config");
            _jointPairs = serializedObject.FindProperty("_jointPairs");
            _shapePoseData = serializedObject.FindProperty("_shapePoseData");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            RenderConfig();
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        protected void RenderConfig()
        {
            var config = target as CharacterRetargeterConfig;
            if (config == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(),
                    false);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {

                EditorGUILayout.PropertyField(_config);
                if (_config.objectReferenceValue != null)
                {
                    if (GUILayout.Button("Map") ||
                        (!string.IsNullOrEmpty(config.Config) && config.JointPairs.Length == 0))
                    {
                        LoadConfig(serializedObject, config);
                    }

                    if (config.ConfigAsset != null && GUILayout.Button("Open Editor"))
                    {
                        FindAndOpenConfigEditor(config);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_jointPairs);
            EditorGUILayout.PropertyField(_shapePoseData);
        }

        private void FindAndOpenConfigEditor(CharacterRetargeterConfig config)
        {
            var targetType = typeof(MSDKUtilityEditorMetadata);
            var guids = AssetDatabase.FindAssets($"t:{nameof(MSDKUtilityEditorMetadata)}");
            var metadataObjects = new List<MSDKUtilityEditorMetadata>();
            var metadataAssets = new List<MSDKUtilityEditorMetadata>();
            string metadataAssetPath = "";
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(config.gameObject);
            var modelAssetPath = AssetDatabase.GetAssetPath(prefab);
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, targetType) as MSDKUtilityEditorMetadata;
                if (asset == null || asset.ConfigJson == null)
                {
                    continue;
                }

                if (config.ConfigAsset == asset.ConfigJson)
                {
                    metadataObjects.Add(asset);
                }
                else if (asset?.Model && AssetDatabase.GetAssetPath(asset.Model) == modelAssetPath)
                {
                    metadataAssets.Add(asset);
                    metadataAssetPath = assetPath;
                }
            }

            if (metadataAssets.Count != 0)
            {
                bool confirmed = EditorUtility.DisplayDialog("Error", $"The config isn't linked to a config scriptable object. Opening config at {metadataAssetPath}", "Ok");
                if (confirmed)
                {
                    MSDKUtilityEditor.CreateAlignmentScene(metadataAssets[0].Model);
                }
                return;
            }
            if (metadataObjects.Count == 0)
            {
                Debug.LogError("The config isn't linked to a config scriptable object!");
                return;
            }
            else
            {
                var targetAsset = metadataObjects[0].Model;
                MSDKUtilityEditor.CreateAlignmentScene(targetAsset);
            }
        }

        /// <summary>
        /// Loads config data to Unity using the native utility plugin.
        /// </summary>
        /// <param name="serializedObject">The serialized object to be updated.</param>
        /// <param name="config">The config to be updated.</param>
        public static void LoadConfig(SerializedObject serializedObject, CharacterRetargeterConfig config)
        {
            MSDKUtility.CreateOrUpdateHandle(config.Config, out var handle);

            // Load number of joints & Shapes for the Target Skeleton.
            MSDKUtility.GetSkeletonInfo(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                out var skeletonInfo);

            // Find each joint and assign it.
            var jointPairs = serializedObject.FindProperty("_jointPairs");
            jointPairs.arraySize = skeletonInfo.JointCount;

            NativeArray<int> parentJointIndexes = new NativeArray<int>(skeletonInfo.JointCount, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            MSDKUtility.GetJointNames(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                out var jointNames);
            MSDKUtility.GetParentJointIndexesByRef(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                ref parentJointIndexes);
            Debug.Assert(jointNames.Length == skeletonInfo.JointCount &&
                         parentJointIndexes.Length == skeletonInfo.JointCount);

            // Fill out the joint associations.
            MSDKUtilityHelper.GetRootJoint(handle, config.transform, out var rootIndex, out var rootJoint);
            for (var i = 0; i < jointNames.Length; i++)
            {
                var jointName = jointNames[i];
                var joint = FindChildRecursive(jointName, rootJoint);
                if (i == rootIndex)
                {
                    joint = rootJoint;
                }

                jointPairs.GetArrayElementAtIndex(i).FindPropertyRelative("Joint").objectReferenceValue = joint;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            // Fill out the parent joint associations using the filled out joints.
            for (var i = 0; i < parentJointIndexes.Length; i++)
            {
                var parentIndex = parentJointIndexes[i];
                var jointParent = parentIndex != MSDKUtility.INVALID_JOINT_INDEX
                    ? config.JointPairs[parentIndex].Joint
                    : null;
                jointPairs.GetArrayElementAtIndex(i).FindPropertyRelative("ParentJoint").objectReferenceValue =
                    jointParent;
            }

            var skinnedMeshes = config.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            var shapePoses = serializedObject.FindProperty("_shapePoseData");

            // Given the blendshapes stored in the config, associate them with skinned mesh renderers.
            // This is organized into a collection.
            List<ShapePoseData> shapePoseDatas = new List<ShapePoseData>();
            if (skeletonInfo.BlendShapeCount != 0)
            {
                shapePoseDatas = BuildShapePoseData(handle, skinnedMeshes, skeletonInfo.BlendShapeCount);
            }


            if (shapePoseDatas.Count > 0)
            {
                var shapePoseDataField = serializedObject.FindProperty("_shapePoseData");
                shapePoses.arraySize = shapePoseDatas.Count;
                for (int i = 0; i < shapePoseDatas.Count; i++)
                {
                    var arrayElement = shapePoses.GetArrayElementAtIndex(i);
                    arrayElement.FindPropertyRelative("SkinnedMesh").objectReferenceValue =
                        shapePoseDatas[i].SkinnedMesh;
                    arrayElement.FindPropertyRelative("ShapeName").stringValue =
                        shapePoseDatas[i].ShapeName;
                    arrayElement.FindPropertyRelative("ShapeIndex").intValue =
                        shapePoseDatas[i].ShapeIndex;
                }
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            parentJointIndexes.Dispose();
            MSDKUtility.DestroyHandle(handle);
        }

        private static List<ShapePoseData> BuildShapePoseData(UInt64 handle, SkinnedMeshRenderer[] skinnedMeshes,
            int expectedShapeCount)
        {
            List<ShapePoseData> shapePoseDatas = new List<ShapePoseData>();
            int currentConfigBlendIndex = 0;
            MSDKUtility.GetBlendShapeNames(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                out var blendShapeNames);

            foreach (var skinnedMeshRenderer in skinnedMeshes)
            {
                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                var numBlendshapes = sharedMesh.blendShapeCount;
                string currentShapeNameToMatch = blendShapeNames[currentConfigBlendIndex];
                for (int blendShapeIndex = 0; blendShapeIndex < numBlendshapes; blendShapeIndex++)
                {
                    // See if the skinned mesh's blendshape matches the one in the config.
                    // If so, add that to collection.
                    var currentBlendshapeName = sharedMesh.GetBlendShapeName(blendShapeIndex);
                    if (currentBlendshapeName == currentShapeNameToMatch)
                    {
                        shapePoseDatas.Add(new ShapePoseData(skinnedMeshRenderer, currentBlendshapeName,
                            blendShapeIndex));
                        // go find the next blendshape in the config
                        currentConfigBlendIndex++;
                        // if we don't have anymore blendshapes to find based on the config, stop
                        if (currentConfigBlendIndex >= expectedShapeCount)
                        {
                            break;
                        }

                        currentShapeNameToMatch = blendShapeNames[currentConfigBlendIndex];
                    }
                }

                if (currentConfigBlendIndex >= expectedShapeCount)
                {
                    break;
                }
            }

            return shapePoseDatas;
        }

        private static Transform FindChildRecursive(string nameToFind, Transform rootJoint)
        {
            var t = FindChildRecursiveWithMatch(nameToFind, rootJoint, true);
            return t != null ? t : FindChildRecursiveWithMatch(nameToFind, rootJoint, false);
        }

        private static Transform FindChildRecursiveWithMatch(string nameToFind, Transform rootJoint, bool exactMatch)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(rootJoint); // Start with the current transform
            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                // Check for exact match or contains based on the exactMatch parameter
                if ((exactMatch && current.name == nameToFind) || (!exactMatch && current.name.Contains(nameToFind)))
                {
                    return current; // Return the matching transform
                }

                // Enqueue all children of the current transform
                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null; // Return null if no matching transform is found
        }
    }
}
