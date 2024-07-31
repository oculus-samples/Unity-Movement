// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Oculus.Movement.Utils;
using UnityEditor;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Adds button features in the inspector:
    /// * Generate and/or refresh positions of bone visuals childed to bone transforms
    /// </summary>
    [CustomEditor(typeof(BodyPoseBoneTransforms))]
    public class BodyPoseBoneTransformsEditor : Editor
    {
        private InspectorGuiHelper[] _helpers;
        private InspectorGuiHelper[] Helpers =>
            _helpers != null ? _helpers : _helpers = new InspectorGuiHelper[]
        {
            new InspectorGuiHelper(IsShowingRefreshButton, Refresh, null,
                "Refresh Transforms", InspectorGuiHelper.OptionalIcon.None),
            new InspectorGuiHelper(IsAbleToTPose, RefreshTPose, null,
                "Refresh T-Pose", InspectorGuiHelper.OptionalIcon.None),
            new InspectorGuiHelper(IsAbleToSave, SaveAsset, null,
                "Export Asset", InspectorGuiHelper.OptionalIcon.None),
        };

        private BodyPoseBoneTransforms Target => (BodyPoseBoneTransforms)target;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            Array.ForEach(Helpers, helper => helper.DrawInInspector());
            DrawDefaultInspector();
        }

        private bool IsShowingRefreshButton() => Target.BodyPose != null;

        private bool IsAbleToTPose() => true;

        private bool IsAbleToSave() => Target.BoneContainer != null;

        private void Refresh()
        {
            Target.RefreshHierarchyDuringEditor();
            EditorTransformAwareness.RefreshSystem();
            EditorBodyPoseLineSkeleton.RefreshSystem();
        }

        private void RefreshTPose()
        {
            Target.RefreshTPose();
        }

        private void SaveAsset()
        {
            BodyPoseControllerEditor.SaveAsset(Target);
        }
    }
}
