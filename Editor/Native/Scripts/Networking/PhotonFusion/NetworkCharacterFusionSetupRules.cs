// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if FUSION_WEAVER && FUSION2
using System.Linq;
using Fusion;
using Fusion.Editor;
using UnityEditor;

namespace Meta.XR.Movement.Networking.Fusion.Editor
{
    [InitializeOnLoad]
    internal static class NetworkCharacterFusionSetupRules
    {
        private const string _fusionAssemblyName = "Meta.XR.Movement.Networking.Fusion";

        static NetworkCharacterFusionSetupRules()
        {
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Features,
                isDone: _ =>
                    NetworkProjectConfig.Global.AssembliesToWeave.Contains(_fusionAssemblyName),
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
            for (var i = 0; i < current.Length; i++)
            {
                NetworkProjectConfig.Global.AssembliesToWeave[i] = current[i];
            }

            NetworkProjectConfig.Global.AssembliesToWeave[current.Length] = _fusionAssemblyName;
            NetworkProjectConfigUtilities.SaveGlobalConfig();
        }
    }
}
#endif // FUSION_WEAVER && FUSION2
