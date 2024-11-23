// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_NGO_MODULE_DEFINED
using Meta.XR.Movement.Editor;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.Networking.NGO.Editor.NetworkPoseRetargeterSpawnerNGOEditor;

namespace Meta.XR.Movement.Networking.NGO.Editor
{
    [InitializeOnLoad]
    internal static class NetworkPoseRetargeterNGOSetupRules
    {
        static NetworkPoseRetargeterNGOSetupRules()
        {
            var methodIsPrefabContainedInNetworkManagerBB = GetMethodWithReflection(
                "Meta.XR.MultiplayerBlocks.NGO.Editor",
                "BuildingBlockNGOUtils",
                "IsPrefabContainedInNetworkManagerBB");
            var methodAddPrefabsToNetworkManagerBB = GetMethodWithReflection(
                "Meta.XR.MultiplayerBlocks.NGO.Editor",
                "BuildingBlockNGOUtils",
                "AddPrefabsToNetworkManagerBB");

            var spawnerPrefab = NativeUtilityEditor.FindPrefab(SpawnerPrefabName);
            var characterPrefab = NativeUtilityEditor.FindPrefab(CharacterPrefabName);
            if (spawnerPrefab == null)
            {
                return;
            }

            if (characterPrefab == null)
            {
                return;
            }

            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Features,
                isDone: _ =>
                {
                    return (bool)methodIsPrefabContainedInNetworkManagerBB.Invoke(null,
                               new object[] { spawnerPrefab }) &&
                           (bool)methodIsPrefabContainedInNetworkManagerBB.Invoke(null,
                               new object[] { characterPrefab });
                },
                fix: _ =>
                {
                    Fix();
                },
                fixMessage: "Add the Networked Retargeted Character prefab to the network prefabs list",
                message:
                "When using the Networked Retargeted Character NGO Block, you must add its prefabs to the network prefabs list"
            );
        }

        /// <summary>
        /// Fix the project setup tool error.
        /// </summary>
        public static void Fix()
        {
            var methodAddPrefabsToNetworkManagerBB = GetMethodWithReflection(
                "Meta.XR.MultiplayerBlocks.NGO.Editor",
                "BuildingBlockNGOUtils",
                "AddPrefabsToNetworkManagerBB");
            var spawnerPrefab = NativeUtilityEditor.FindPrefab(SpawnerPrefabName);
            var characterPrefab = NativeUtilityEditor.FindPrefab(CharacterPrefabName);
            methodAddPrefabsToNetworkManagerBB.Invoke(null,
                new object[] { new[] { spawnerPrefab, characterPrefab } });
        }

        private static MethodInfo GetMethodWithReflection(string assemblyName, string typeName, string methodName,
            System.Type[] types = null)
        {
            var assembly = Assembly.Load(assemblyName);
            if (assembly == null)
            {
                Debug.LogError($"Could not load assembly {assemblyName}");
                return null;
            }

            var fullTypeName = assemblyName + "." + typeName;
            var type = assembly.GetType(fullTypeName);
            if (type == null)
            {
                Debug.LogError($"Could not find type {fullTypeName}");
                return null;
            }

            var methodInfo = types != null
                ? type.GetMethod(methodName, types)
                : type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (methodInfo != null)
            {
                return methodInfo;
            }

            Debug.LogError($"Could not find method {methodName}");
            return null;
        }
    }
}
#endif // UNITY_NGO_MODULE_DEFINED
