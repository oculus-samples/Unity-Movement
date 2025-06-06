// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.IO;
using Meta.XR.Movement.Retargeting;
#if ISDK_DEFINED
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Collections;
#endif
using Oculus.Movement.Utils;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Adds button features in the inspector:
    /// * Refresh bone pose data from the controller's data source
    /// * Refresh bone pose data from the static T-Pose
    /// * Export bone pose data to a new ScriptableObject in the Assets folder
    /// </summary>
    [CustomEditor(typeof(BodyPoseController))]
    public class BodyPoseControllerEditor : UnityEditor.Editor
    {
#if ISDK_DEFINED
        /// <summary>
        /// Internal wrapper for treating <see cref="IBodyPose"/> as <see cref="IBody"/>, for
        /// <see cref="ScriptableObject"/> serialization.
        /// </summary>
        private class IBodyWrapperForIBodyPose : IBody
        {
            /// <summary>
            /// <see cref="IBodyPose"/> reference.
            /// </summary>
            public IBodyPose _iBodyPose;

            /// <inheritdoc cref="IBody.WhenBodyUpdated"/>
            public event Action WhenBodyUpdated = delegate { };

            /// <inheritdoc cref="IBody.SkeletonMapping"/>
            public ISkeletonMapping SkeletonMapping => new FullBodySkeletonTPose();

            /// <inheritdoc cref="IBody.IsConnected"/>
            public bool IsConnected => true;

            /// <inheritdoc cref="IBody.IsHighConfidence"/>
            public bool IsHighConfidence => true;

            /// <inheritdoc cref="IBody.IsTrackedDataValid"/>
            public bool IsTrackedDataValid => true;

            /// <inheritdoc cref="IBody.Scale"/>
            public float Scale => 1;

            /// <inheritdoc cref="IBody.CurrentDataVersion"/>
            public int CurrentDataVersion => 1;

            /// <summary>
            /// Main constructor.
            /// </summary>
            /// <param name="iBodyPose"><see cref="IBodyPose"/> reference.</param>
            public IBodyWrapperForIBodyPose(IBodyPose iBodyPose) => _iBodyPose = iBodyPose;

            /// <inheritdoc cref="IBody.GetRootPose"/>
            public bool GetRootPose(out Pose pose)
            {
                pose = new Pose(Vector3.zero, Quaternion.identity);
                return true;
            }

            /// <inheritdoc cref="IBody.GetJointPose"/>
            public bool GetJointPose(BodyJointId bodyJointId, out Pose pose) =>
                _iBodyPose.GetJointPoseFromRoot(bodyJointId, out pose);

            /// <inheritdoc cref="IBody.GetJointPoseLocal"/>
            public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
                _iBodyPose.GetJointPoseLocal(bodyJointId, out pose);

            /// <inheritdoc cref="IBody.GetJointPoseFromRoot"/>
            public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
                _iBodyPose.GetJointPoseFromRoot(bodyJointId, out pose);
        }

        private const string POSE_DIRECTORY = "BodyPoses";

        private InspectorGuiHelper[] helpers;

        private InspectorGuiHelper[] Helpers => helpers != null
            ? helpers
            : helpers = new InspectorGuiHelper[]
            {
                new InspectorGuiHelper(IsAbleToRefreshSource, RefreshSource, null,
                    "Refresh Source Data", InspectorGuiHelper.OptionalIcon.None),
                new InspectorGuiHelper(IsAbleToTPose, RefreshTPose, null,
                    "Refresh T-Pose", InspectorGuiHelper.OptionalIcon.None),
                new InspectorGuiHelper(IsAbleToSave, SaveAsset, null,
                    "Export Asset", InspectorGuiHelper.OptionalIcon.None),
            };

        private BodyPoseController Target => (BodyPoseController)target;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Array.ForEach(Helpers, helper => helper.DrawInInspector());
        }

        private bool IsAbleToSave() => Target.BonePoses.Length != 0;

        private bool IsAbleToTPose() => true;

        private bool IsAbleToRefreshSource() => Target.BodyPose != null;

        private void SaveAsset()
        {
            SaveAsset(Target);
        }

        internal static void SaveAsset(IBodyPose data)
        {
            string name = $"BodyPose-{System.DateTime.Now.ToString("yyyyMMdd-HHmmss")}";
            SaveAsset(data, name);
        }

        internal static void SaveAsset(IBodyPose data, string assetName)
        {
            BodyPoseData assetToWrite = GeneratePoseAsset(assetName);
            assetToWrite.SetBodyPose(new IBodyWrapperForIBodyPose(data));
            string path = AssetDatabase.GetAssetPath(assetToWrite);
            Debug.Log($"Captured Body Pose into {AssetDatabase.GetAssetPath(assetToWrite)}");
            EditorUtility.SetDirty(assetToWrite);
            AssetDatabase.SaveAssetIfDirty(assetToWrite);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(assetToWrite);
        }

        private static BodyPoseData GeneratePoseAsset(string name)
        {
            var poseDataAsset = ScriptableObject.CreateInstance<BodyPoseData>();
            string parentDir = Path.Combine("Assets", POSE_DIRECTORY);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            AssetDatabase.CreateAsset(poseDataAsset, Path.Combine(parentDir, $"{name}.asset"));
            return poseDataAsset;
        }

        private void RefreshTPose()
        {
            Target.RefreshTPose();
        }

        private void RefreshSource()
        {
            Target.RefreshFromSourceData();
            EditorTransformAwareness.RefreshSystem();
        }
#endif
    }
}
