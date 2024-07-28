// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Editor window that displays information about configuring the project.
    /// </summary>
    public class ProjectSettingsValidationWindow : EditorWindow
    {
        private static ProjectSettingsValidationWindow _projectValidationWindow;

        /// <summary>
        /// Shows the project settings validation window.
        /// </summary>
        public static void ShowProjectSettingsValidationWindow()
        {
            if (!HasOpenInstances<ProjectSettingsValidationWindow>())
            {
                _projectValidationWindow = GetWindow<ProjectSettingsValidationWindow>();
                _projectValidationWindow.titleContent = new GUIContent("Movement Validation");
                _projectValidationWindow.Focus();
            }
        }

        private void OnEnable()
        {
            EditorWindow editorWindow = this;

            Vector2 windowSize = new Vector2(600, 320);
            editorWindow.minSize = windowSize;
            editorWindow.maxSize = windowSize;
        }

        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            GUILayout.BeginVertical();
            {
                GUILayout.FlexibleSpace();
                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP,
                    "IL2CPP enabled on Android:",
                    "Enabled.",
                    "Not enabled. Please check your project's player settings.");

                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0,
                    "ARM64 enabled on Android:",
                    "Enabled.",
                    "Not enabled. Please check your project's player settings.");

                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    OVRProjectConfig.CachedProjectConfig.eyeTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                    "Eye tracking enabled:",
                    "Enabled.",
                    "Disabled. Please enable eye tracking in OVRManager if you wish to use that feature.");

                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    OVRProjectConfig.CachedProjectConfig.faceTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                    "Face tracking enabled:",
                    "Enabled.",
                    "Disabled. Please enable face tracking in OVRManager if you wish to use that feature.");

                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    OVRProjectConfig.CachedProjectConfig.bodyTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                    "Body tracking enabled:",
                    "Enabled.",
                    "Disabled. Please enable body tracking in OVRManager if you wish to use that feature.");

                DisplayHelpBoxForSettingCheck(
                    labelStyle,
                    !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android),
                    "Disable Auto Graphics API:",
                    "True.",
                    "Enabled. We recommend disabling Auto Graphics API and picking compatible APIs such as OpenGL ES 3.0 or Vulkan.");

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Check Project Setup Tool", GUILayout.Width(200.0f)))
                {
                    SettingsService.OpenProjectSettings("Project/Oculus");
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                GUILayout.FlexibleSpace();
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DisplayHelpBoxForSettingCheck(
            GUIStyle labelStyle,
            bool isSettingOn,
            string settingLabel,
            string messageIfSettingOn,
            string messageIfSettingOff)
        {
            GUI.enabled = !isSettingOn;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(settingLabel, EditorStyles.boldLabel);
                GUILayout.Label(
                    $"{(isSettingOn ? messageIfSettingOn : messageIfSettingOff)}",
                    labelStyle);
                GUILayout.Space(5);
            }
            GUILayout.EndVertical();
            GUI.enabled = true;
        }
    }
}
