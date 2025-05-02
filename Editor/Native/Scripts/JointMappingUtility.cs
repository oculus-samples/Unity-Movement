// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Movement.Retargeting;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Utility class for mapping joints between source and target skeletons.
    /// </summary>
    public class JointMappingUtility
    {
        /// <summary>
        /// Aligns finger bones between source and target skeletons.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration.</param>
        /// <param name="startBoneIds">The array of starting bone IDs for fingers.</param>
        /// <param name="endBoneIds">The array of ending bone IDs for fingers.</param>
        /// <param name="wristType">The wrist joint type.</param>
        public static void PerformFingerMatching(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            MSDKUtilityEditorMetadata editorMetadataObject, MSDKUtilityEditorConfig source,
            MSDKUtilityEditorConfig target, FullBodyTrackingBoneId[] startBoneIds,
            FullBodyTrackingBoneId[] endBoneIds,
            KnownJointType wristType)
        {
            var fingerNames = new[]
            {
                new[] { "thumb" },
                new[] { "index" },
                new[] { "middle" },
                new[] { "ring" },
                new[] { "pinky" }
            };
            var startNameExtensions = new[]
            {
                "metacarpal", "proximal", "1", "2"
            };
            var endNameExtensions = new[]
            {
                "distal", "3", "4"
            };

            var startBones = new List<Transform>();
            var endBones = new List<Transform>();
            var wrist = JointAlignmentUtility.GetTargetTransformFromKnownJoint(target, wristType);
            var wristBones = wrist.GetAllChildren();
            foreach (var fingerNameSet in fingerNames)
            {
                foreach (var fingerName in fingerNameSet)
                {
                    var startBonesToCheck = startNameExtensions.Select(e => fingerName + e);
                    var endBonesToCheck = endNameExtensions.Select(e => fingerName + e);
                    var startBone =
                        MSDKUtilityHelper.FindClosestMatches(wristBones.ToArray(), startBonesToCheck.ToArray())[0]
                            .Item1;
                    var endBone =
                        MSDKUtilityHelper.FindClosestMatches(wristBones.ToArray(), endBonesToCheck.ToArray())[0]
                            .Item1;
                    startBones.Add(startBone);
                    endBones.Add(endBone);
                }
            }

            for (var i = 0; i < startBoneIds.Length; i++)
            {
                var startPos = source.ReferencePose[(int)startBoneIds[i]].Position;
                var endPos = source.ReferencePose[(int)endBoneIds[i]].Position;
                var startBone = startBones[i];
                var endBone = endBones[i];
                startBone.position = startPos;

                var targetDirection = endPos - startPos;
                var targetLength = targetDirection.magnitude;
                targetDirection.Normalize();
                var currentDirection = endBone.position - startBone.position;
                var currentLength = currentDirection.magnitude;
                currentDirection.Normalize();

                var scaleFactor = targetLength / currentLength;
                var rotation = Quaternion.FromToRotation(currentDirection, targetDirection);
                startBone.rotation = rotation * startBone.rotation;
                JointAlignmentUtility.UpdateTPoseData(target);
                JointAlignmentUtility.ApplyScalingToJointIndexAndChildren(target,
                    Array.IndexOf(target.JointNames, startBone.name), scaleFactor);
                JointAlignmentUtility.UpdateTPoseData(target);
            }

            UpdateJointMapping(win, previewRetargeter, previewer, utilityConfig, editorMetadataObject, source, target);
        }


        /// <summary>
        /// Aligns wrist joints between source and target skeletons.
        /// </summary>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration.</param>
        public static void PerformWristMatching(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            // Rotate to match left/right hand.
            var sourceRightHand = source.ReferencePose[(int)FullBodyTrackingBoneId.RightHandWrist];
            var sourceLeftHand = source.ReferencePose[(int)FullBodyTrackingBoneId.LeftHandWrist];
            var leftArmIndex = JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.LeftUpperArm);
            var rightArmIndex = JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.RightUpperArm);

            var leftUpperArm = target.SkeletonJoints[leftArmIndex];
            var leftHand =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.LeftWrist)];
            leftUpperArm.rotation =
                JointAlignmentUtility.GetBestRotation(leftUpperArm, leftHand, sourceLeftHand.Position);

            var rightUpperArm = target.SkeletonJoints[rightArmIndex];
            var rightHand =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.RightWrist)];
            rightUpperArm.rotation =
                JointAlignmentUtility.GetBestRotation(rightUpperArm, rightHand, sourceRightHand.Position);

            JointAlignmentUtility.UpdateTPoseData(target);
        }

        /// <summary>
        /// Aligns leg joints between source and target skeletons.
        /// </summary>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration.</param>
        public static void PerformLegMatching(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            var sourceRightFoot = source.ReferencePose[(int)FullBodyTrackingBoneId.RightFootAnkle];
            var sourceLeftFoot = source.ReferencePose[(int)FullBodyTrackingBoneId.LeftFootAnkle];

            // Rotate to match left/right foot.
            var leftUpperLeg =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.LeftUpperLeg)];
            var leftFoot =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.LeftAnkle)];
            var leftFootRotation = leftFoot.rotation;
            leftUpperLeg.rotation =
                JointAlignmentUtility.GetBestRotation(leftUpperLeg, leftFoot, sourceLeftFoot.Position);
            leftFoot.rotation = leftFootRotation;
            var rightUpperLeg =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.RightUpperLeg)];
            var rightFoot =
                target.SkeletonJoints[JointAlignmentUtility.GetKnownJointIndex(target, KnownJointType.RightAnkle)];
            var rightFootRotation = rightFoot.rotation;
            rightUpperLeg.rotation =
                JointAlignmentUtility.GetBestRotation(rightUpperLeg, rightFoot, sourceRightFoot.Position);
            rightFoot.rotation = rightFootRotation;

            JointAlignmentUtility.UpdateTPoseData(target);
        }

        /// <summary>
        /// Performs automatic mapping between source and target skeletons.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration to be updated.</param>
        public static void AutoMapping(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig, MSDKUtilityEditorConfig source,
            ref MSDKUtilityEditorConfig target)
        {
            // Load unscaled t-pose and reset character.
            MSDKUtilityEditorConfig.LoadConfig(win, previewRetargeter, previewer, utilityConfig,
                SkeletonTPoseType.UnscaledTPose);

            target.SkeletonTPoseType = win.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => target.SkeletonTPoseType
            };

            // 1. Scale the character such that the wrists heights match.
            win.Previewer.ScaleCharacter(source, target, true);

            // 2. Move character such that arms align.
            JointAlignmentUtility.AlignRoot(source, target);

            // 3. Align the wrists.
            JointAlignmentUtility.AlignWrists(source, target);

            // Restart playback file
            if (win.FileReader.IsPlaying)
            {
                win.OpenPlaybackFile(win.CurrentPreviewPose);
            }
        }

        /// <summary>
        /// Updates joint mappings between source and target skeletons.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration.</param>
        /// <param name="updateReferenceTPose">Whether to update the reference T-pose.</param>
        public static void UpdateJointMapping(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            MSDKUtilityEditorMetadata editorMetadataObject, MSDKUtilityEditorConfig source,
            MSDKUtilityEditorConfig target, bool updateReferenceTPose = false)
        {
            if (win.Step is MSDKUtilityEditorConfig.EditorStep.Configuration
                or MSDKUtilityEditorConfig.EditorStep.Review)
            {
                return;
            }

            // 1. Update T-Pose.
            JointAlignmentUtility.UpdateTPoseData(target);

            // 2. Generate mapping weights.
            var mapping = new List<JointMapping>();
            var mappingEntries = new List<JointMappingEntry>();
            JointMappingGeneration.GenerateJointWeights(win, ref mapping, ref mappingEntries);

            // 3. Record mappings.
            target.JointMappings = mapping.ToArray();
            target.JointMappingEntries = mappingEntries.ToArray();

            // 4. Update the config
            MSDKUtilityEditorConfig.UpdateConfig(win, previewRetargeter, previewer, utilityConfig, editorMetadataObject,
                false, updateReferenceTPose);
        }
    }
}
