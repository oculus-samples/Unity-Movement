// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Oculus.Movement.Utils;
using UnityEditor;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Adds button features in the inspector:
    /// * Generate and/or refresh positions of bone visuals childed to bone transforms
    /// </summary>
    [CustomEditor(typeof(BodyPoseBoneTransforms))]
    public class BodyPoseBoneTransformsEditor : UnityEditor.Editor
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

        private bool IsShowingRefreshButton()
        {
#if ISDK_DEFINED
            return Target.BodyPose != null;
#else
            return false;
#endif
        }

        private bool IsAbleToTPose() => true;

        private bool IsAbleToSave()
        {
#if ISDK_DEFINED
            return Target.BoneContainer != null;
#else
            return false;
#endif
        }

        private void Refresh()
        {
#if ISDK_DEFINED
            Target.RefreshHierarchyDuringEditor();
#endif
            EditorTransformAwareness.RefreshSystem();
        }

        private void RefreshTPose()
        {
#if ISDK_DEFINED
            Target.RefreshTPose();
#endif
        }

        private void SaveAsset()
        {
#if ISDK_DEFINED
            BodyPoseControllerEditor.SaveAsset(Target);
#endif
        }

    }
}
