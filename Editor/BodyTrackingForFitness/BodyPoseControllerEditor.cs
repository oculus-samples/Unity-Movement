// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Movement.Utils;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Adds button features in the inspector:
    /// * Refresh bone pose data from the controller's data source
    /// * Refresh bone pose data from the static T-Pose
    /// * Export bone pose data to a new ScriptableObject in the Assets folder
    /// </summary>
    [CustomEditor(typeof(BodyPoseController))]
    public class BodyPoseControllerEditor : Editor
    {
        /// <summary>
        /// Internal wrapper for treating <see cref="IBodyPose"/> as <see cref="IBody"/>, for
        /// <see cref="ScriptableObject"/> serialization.
        /// </summary>
        private class IBodyWrapperForIBodyPose : IBody
        {
            public IBodyPose _iBodyPose;

            public event Action WhenBodyUpdated = delegate { };

            public ISkeletonMapping SkeletonMapping => new FullBodySkeletonTPose();

            public bool IsConnected => true;

            public bool IsHighConfidence => true;

            public bool IsTrackedDataValid => true;

            public float Scale => 1;

            public int CurrentDataVersion => 1;

            public IBodyWrapperForIBodyPose(IBodyPose iBodyPose) => _iBodyPose = iBodyPose;

            public bool GetRootPose(out Pose pose)
            {
                pose = new Pose(Vector3.zero, Quaternion.identity);
                return true;
            }

            public bool GetJointPose(BodyJointId bodyJointId, out Pose pose) =>
                _iBodyPose.GetJointPoseFromRoot(bodyJointId, out pose);

            public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
                _iBodyPose.GetJointPoseLocal(bodyJointId, out pose);

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
            EditorBodyPoseLineSkeleton.RefreshSystem();
        }
    }
}
