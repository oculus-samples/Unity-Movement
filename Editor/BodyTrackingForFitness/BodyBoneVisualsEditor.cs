// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Oculus.Movement.Utils;
using UnityEditor;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Adds button features in the inspector:
    /// * Connect to <see cref="BodyPoseBoneTransforms"/>
    /// * Generate a default bone prefab
    /// * Generate and/or refresh positions of bone visuals childed to bone transforms
    /// * Delete all bone visuals
    /// </summary>
    [CustomEditor(typeof(BodyPoseBoneVisuals))]
    public class BodyBoneVisualsEditor : Editor
    {
        private InspectorGuiHelper[] helpers;
        private InspectorGuiHelper[] Helpers => helpers != null ? helpers : helpers = new InspectorGuiHelper[]
        {
            new InspectorGuiHelper(IsSkeletonInvalid, PopulateSkeleton, "Missing target skeleton",
                "Find Target Skeleton", InspectorGuiHelper.OptionalIcon.Warning),
            new InspectorGuiHelper(IsBoneVisualEmpty, GenerateBoneVisualPrefab, "Needs bone prefab",
                "Generate Bone Prefab", InspectorGuiHelper.OptionalIcon.Warning),
            new InspectorGuiHelper(IsShowingRefreshButton, Refresh, null,
                "Refresh Bone Visuals", InspectorGuiHelper.OptionalIcon.None),
            new InspectorGuiHelper(IsPopulatedWithBones, Clear, null,
                "Clear Bone Visuals", InspectorGuiHelper.OptionalIcon.None),
        };

        private BodyPoseBoneVisuals Target => (BodyPoseBoneVisuals)target;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            Array.ForEach(Helpers, helper => helper.DrawInInspector());
            DrawDefaultInspector();
        }

        private bool IsShowingRefreshButton() => !IsSkeletonInvalid();

        private bool IsBoneVisualEmpty() => Target.BoneVisualPrefab == null;

        private bool IsSkeletonInvalid() => Target.Skeleton == null;

        private bool IsPopulatedWithBones() => Target.BoneVisuals.Count != 0;

        private void Refresh() => Target.RefreshVisualsInEditor();

        private void GenerateBoneVisualPrefab() => Target.CreatePrimitiveCubeVisualPrefab();

        private void PopulateSkeleton() => Target.Skeleton = FindBestSkeletonOwner();

        private void Clear() => Target.ClearBoneVisuals();

        private BodyPoseBoneTransforms FindBestSkeletonOwner() =>
            FindFirstObjectByType<BodyPoseBoneTransforms>();
    }
}
