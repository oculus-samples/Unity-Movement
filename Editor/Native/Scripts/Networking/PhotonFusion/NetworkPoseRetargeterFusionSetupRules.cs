// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION_WEAVER && FUSION2
using Fusion;
using Fusion.Editor;
using Meta.XR.BuildingBlocks;
using Meta.XR.MultiplayerBlocks.Shared.Editor;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion.Editor
{
    [InitializeOnLoad]
    internal static class NetworkPoseRetargeterFusionSetupRules
    {
        private const string _fusionAssemblyName = "Meta.XR.Movement.Networking.Fusion";

        static NetworkPoseRetargeterFusionSetupRules()
        {
            var blocksInScene = Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.InstanceID).ToList();
            var methodGetInstallationRoutine = GetMethodWithReflection(
                "Meta.XR.BuildingBlocks.Editor",
                "Utils",
                "GetInstallationRoutine",
                new[] { typeof(string) });

            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Features,
                isDone: _ =>
                    NetworkProjectConfig.Global.AssembliesToWeave.Contains(_fusionAssemblyName) ||
                    !blocksInScene.Any(block =>
                    {
                        var routineId = block.InstallationRoutineCheckpoint?.InstallationRoutineId;
                        if (string.IsNullOrEmpty(routineId) ||
                            methodGetInstallationRoutine.Invoke(null, new object[] { routineId }) is
                                not NetworkInstallationRoutine routine)
                        {
                            return false;
                        }

                        var field = typeof(NetworkInstallationRoutine)
                            .GetField("implementation", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            return (int)field.GetValue(routine) == 1;
                        }

                        return false;
                    }),
                message:
                "When using the Networked Retargeted Character Fusion Block in your project it's required to add the assembly to Fusion AssembliesToWeave",
                fix: _ =>
                {
                    Fix();
                },
                fixMessage:
                $"Add assembly {_fusionAssemblyName} to Fusion project config's AssembliesToWeave"
            );
        }

        /// <summary>
        /// Fix the project setup tool error.
        /// </summary>
        public static void Fix()
        {
            var current = NetworkProjectConfig.Global.AssembliesToWeave;
            foreach (var assemblyName in current)
            {
                if (assemblyName == _fusionAssemblyName)
                {
                    return;
                }
            }
            NetworkProjectConfig.Global.AssembliesToWeave = new string[current.Length + 1];
            for (int i = 0; i < current.Length; i++)
            {
                NetworkProjectConfig.Global.AssembliesToWeave[i] = current[i];
            }

            NetworkProjectConfig.Global.AssembliesToWeave[current.Length] = _fusionAssemblyName;
            NetworkProjectConfigUtilities.SaveGlobalConfig();
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
#endif // FUSION_WEAVER && FUSION2
