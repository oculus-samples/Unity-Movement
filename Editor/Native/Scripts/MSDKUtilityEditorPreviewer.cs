// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Linq;
using Meta.XR.Movement.Retargeting.Editor;
using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Utils;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Previewer for the Movement SDK utility editor.
    /// </summary>
    [Serializable]
    public class MSDKUtilityEditorPreviewer : ScriptableObject
    {
        /// <summary>
        /// Gets or sets the editor window.
        /// </summary>
        public MSDKUtilityEditorWindow Window
        {
            get => _window;
            set => _window = value;
        }
        /// <summary>
        /// Gets or sets the character in the scene view.
        /// </summary>
        public GameObject SceneViewCharacter
        {
            get => _sceneViewCharacter;
            set => _sceneViewCharacter = value;
        }
        /// <summary>
        /// Gets the skeleton draw for the source T-pose.
        /// </summary>
        public SkeletonDraw SourceSkeletonDrawTPose => _skeletonDraws[0];

        /// <summary>
        /// Gets the skeleton draw for the target T-pose.
        /// </summary>
        public SkeletonDraw TargetSkeletonDrawTPose => _skeletonDraws[1];

        /// <summary>
        /// Gets the skeleton draw for the source preview pose.
        /// </summary>
        public SkeletonDraw SourceSkeletonDrawPreviewPose => _skeletonDraws[2];

        /// <summary>
        /// Gets the skeleton draw for the target preview pose.
        /// </summary>
        public SkeletonDraw TargetSkeletonDrawPreviewPose => _skeletonDraws[3];

        /// <summary>
        /// Gets or sets the preview character retargeter.
        /// </summary>
        public CharacterRetargeter PreviewRetargeter
        {
            get => _previewRetargeter;
            set => _previewRetargeter = value;
        }

        [SerializeField]
        private MSDKUtilityEditorWindow _window;

        [SerializeField]
        private GameObject _sceneViewCharacter;

        [SerializeField]
        private CharacterRetargeter _previewRetargeter;

        private Vector3 _previewPosition = Vector3.forward;

        [SerializeField]
        private SkeletonDraw[] _skeletonDraws = new SkeletonDraw[4];

        /// <summary>
        /// Initializes the skeleton draws.
        /// </summary>
        public void InitializeSkeletonDraws()
        {
            var sourceColor = Color.white;
            var targetColor = Color.green;
            targetColor.a = 0.5f;

            for (var i = 0; i < _skeletonDraws.Length; i++)
            {
                _skeletonDraws[i] = new SkeletonDraw();
            }

            SourceSkeletonDrawTPose.InitDraw(sourceColor, 0.002f);
            SourceSkeletonDrawPreviewPose.InitDraw(sourceColor, 0.002f);
            TargetSkeletonDrawTPose.InitDraw(targetColor, 0.005f);
            TargetSkeletonDrawPreviewPose.InitDraw(targetColor, 0.005f);
        }

        /// <summary>
        /// Instantiates the preview character retargeter.
        /// </summary>
        public void InstantiatePreviewCharacterRetargeter()
        {
            // Instantiate preview character.
            if (_previewRetargeter != null)
            {
                DestroyImmediate(_previewRetargeter.gameObject);
            }

            var previewCharacter = Instantiate(_sceneViewCharacter);
            previewCharacter.name = "Preview-" + _sceneViewCharacter.name;
            previewCharacter.transform.position = _previewPosition;
            SceneManager.MoveGameObjectToScene(previewCharacter, _window.PreviewStage.scene);
            _previewRetargeter = previewCharacter.AddComponent<CharacterRetargeter>();
            _previewRetargeter.ConfigAsset = _window.UtilityConfig.EditorMetadataObject.ConfigJson;
            CharacterRetargeterConfigEditor.LoadConfig(new SerializedObject(_previewRetargeter), _previewRetargeter);
            _previewRetargeter.Retargeting.ApplyScale = true;
            _previewRetargeter.Retargeting.ScaleRange = new Vector2(0.5f, 2.0f);
            _previewRetargeter.Start();

            var serializedConfig = new SerializedObject(_previewRetargeter);
            CharacterRetargeterConfigEditor.LoadConfig(serializedConfig, _previewRetargeter);
        }

        /// <summary>
        /// Loads the skeleton draw with the specified configuration.
        /// </summary>
        /// <param name="drawer">The skeleton drawer to load.</param>
        /// <param name="config">The configuration to use.</param>
        public void LoadDraw(SkeletonDraw drawer, MSDKUtilityEditorConfig config)
        {
            drawer.LoadDraw(config.SkeletonInfo.JointCount, config.ParentIndices,
                config.ReferencePose.ToArray(), config.JointNames, config.SkeletonJoints);
        }

        /// <summary>
        /// Updates the target draw.
        /// </summary>
        /// <param name="previewRetargeter">The preview character retargeter.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration.</param>
        public void UpdateTargetDraw(CharacterRetargeter previewRetargeter, MSDKUtilityEditorConfig utilityConfig, MSDKUtilityEditorMetadata editorMetadataObject, MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            if (TargetSkeletonDrawTPose == null || target == null)
            {
                return;
            }

            if (_window.Overlay.ShouldAutoUpdateMappings)
            {
                JointMappingUtility.UpdateJointMapping(_window, previewRetargeter, this, utilityConfig, editorMetadataObject, source, target);
            }

            JointAlignmentUtility.UpdateTPoseData(target);
            LoadDraw(TargetSkeletonDrawTPose, target);
        }

        /// <summary>
        /// Draws the preview character.
        /// </summary>
        public void DrawPreviewCharacter()
        {
            if (_window.FileReader == null || !_window.FileReader.BodyPose.IsCreated)
            {
                DestroyPreviewCharacterRetargeter();
                return;
            }

            var sourcePose = new NativeArray<NativeTransform>(
                _window.SourceInfo.SkeletonInfo.JointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var sourceTPose = new NativeArray<NativeTransform>(
                _window.SourceInfo.SkeletonInfo.JointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            sourceTPose.CopyFrom(_window.SourceInfo.ReferencePose);
            sourcePose.CopyFrom(_window.FileReader.BodyPose);

            // Scale the TPose and the body pose.
            var scalePercentage = _window.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => 0.0f,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => 1.0f,
                _ => _window.UtilityConfig.ScaleSize / 100.0f
            };
            var scale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo, _window.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            });
            if (_window.Step == MSDKUtilityEditorConfig.EditorStep.Review)
            {
                var minPoseScale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo, SkeletonTPoseType.MinTPose);
                var maxPoseScale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo, SkeletonTPoseType.MaxTPose);
                scale = Mathf.Lerp(minPoseScale, maxPoseScale, _window.UtilityConfig.ScaleSize / 100.0f);
            }

            CalculateSkeletonTPoseByRef(_window.SourceInfo.ConfigHandle, SkeletonType.SourceSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace, scalePercentage, ref sourceTPose);
            MSDKUtilityHelper.ScaleSkeletonPose(_window.SourceInfo.ConfigHandle, SkeletonType.SourceSkeleton, ref sourcePose, scale);

            // Run preview retargeter.
            _previewRetargeter.DebugDrawSourceSkeleton = _window.Overlay.ShouldDrawPreview;
            _previewRetargeter.DebugDrawTargetSkeleton = _window.Overlay.ShouldDrawPreview;
            _previewRetargeter.DebugDrawTransform = _previewRetargeter.transform;
            _previewRetargeter.UpdateTPose(sourceTPose);
            _previewRetargeter.CalculatePose(sourcePose);
            _previewRetargeter.UpdatePose();
        }

        /// <summary>
        /// Destroys the preview character retargeter.
        /// </summary>
        public void DestroyPreviewCharacterRetargeter()
        {
            if (_previewRetargeter == null)
            {
                return;
            }

            DestroyImmediate(_previewRetargeter.gameObject);
            if (_window.FileReader.IsPlaying)
            {
                _window.FileReader.ClosePlaybackFile();
            }
        }

        /// <summary>
        /// Associates the scene character with the target configuration.
        /// </summary>
        /// <param name="target">The target configuration.</param>
        public void AssociateSceneCharacter(MSDKUtilityEditorConfig target)
        {
            var targetJointCount = target.SkeletonInfo.JointCount;

            if (target.SkeletonJoints == null || target.SkeletonJoints.Length != targetJointCount)
            {
                target.SkeletonJoints = new Transform[targetJointCount];
            }

            var rootSearchTransform = _sceneViewCharacter.transform;
            for (var i = 0; i < _sceneViewCharacter.transform.childCount; i++)
            {
                var child = _sceneViewCharacter.transform.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() == null && child.childCount >= 1 &&
                    child.GetChild(0).GetComponent<SkinnedMeshRenderer>() == null)
                {
                    rootSearchTransform = child;
                }
            }

            for (var i = 0; i < targetJointCount; i++)
            {
                if (target.SkeletonJoints[i] != null)
                {
                    continue;
                }

                var jointName = target.JointNames[i];
                var joint = MSDKUtilityHelper.FindChildRecursiveExact(rootSearchTransform, jointName);
                target.SkeletonJoints[i] = joint;
            }

            // Associate root joint.
            MSDKUtilityHelper.GetRootJoint(target.ConfigHandle, _sceneViewCharacter.transform, out var index,
                out var rootJoint);
            target.SkeletonJoints[index] = rootJoint;

            // Associate known joints.
            target.KnownSkeletonJoints = new Transform[(int)KnownJointType.KnownJointCount];
            for (var i = KnownJointType.Root; i < KnownJointType.KnownJointCount; i++)
            {
                if (GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton, i, out index))
                {
                    if (index == -1)
                    {
                        continue;
                    }

                    target.KnownSkeletonJoints[(int)i] = target.SkeletonJoints[index];
                }
            }
        }

        /// <summary>
        /// Reloads the character with the target configuration.
        /// </summary>
        /// <param name="target">The target configuration.</param>
        public void ReloadCharacter(MSDKUtilityEditorConfig target)
        {
            if (_sceneViewCharacter == null ||
                target.SkeletonJoints.Length == 0 ||
                target.SkeletonJoints[0] == null)
            {
                return;
            }

            Undo.RecordObject(_sceneViewCharacter.transform, "Reload Character");

            // First, do scale.
            if (!GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton,
                    KnownJointType.Root, out var rootJointIndex) || rootJointIndex == -1)
            {
                rootJointIndex = 0;
            }

            // Set the positions/rotations for the root first.
            var rootJoint = target.SkeletonJoints[rootJointIndex];
            var rootTPose = target.ReferencePose[rootJointIndex];
            _sceneViewCharacter.transform.localScale = Vector3.Scale(rootTPose.Scale, _window.UtilityConfig.RootScale);
            rootJoint.SetPositionAndRotation(rootTPose.Position, rootTPose.Orientation);
            Undo.RecordObject(rootJoint, "Reload Character");

            for (var i = 0; i < target.SkeletonJoints.Length; i++)
            {
                var joint = target.SkeletonJoints[i];
                var tPose = target.ReferencePose[i];
                joint.localScale = tPose.Scale;
            }

            // Second, set positions/rotations.
            for (var i = 0; i < target.SkeletonJoints.Length; i++)
            {
                if (i == rootJointIndex)
                {
                    continue;
                }

                var joint = target.SkeletonJoints[i];
                var tPose = target.ReferencePose[i];
                Undo.RecordObject(joint, "Reload Character");
                joint.SetPositionAndRotation(tPose.Position, tPose.Orientation);
            }
        }

        /// <summary>
        /// Scales the character.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration.</param>
        /// <param name="scaleCharacter">Whether to scale the character.</param>
        public void ScaleCharacter(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target, bool scaleCharacter)
        {
            // 1. Align Y-axis.
            var tPoseType = _window.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            };
            var yScaleRatio = JointAlignmentUtility.GetDesiredScale(source, target, tPoseType, true);
            if (scaleCharacter)
            {
                var root = JointAlignmentUtility.GetTargetTransformFromKnownJoint(target, KnownJointType.Root);
                Undo.RecordObject(root, "Scale Character");
                _sceneViewCharacter.transform.localScale *= yScaleRatio;
                JointAlignmentUtility.UpdateTPoseData(target);
            }
            else
            {
                for (var i = 0; i < target.ReferencePose.Length; i++)
                {
                    var joint = target.ReferencePose[i];
                    joint.Position *= yScaleRatio;
                    target.ReferencePose[i] = joint;
                }

                ReloadCharacter(target);
            }
        }
    }
}
