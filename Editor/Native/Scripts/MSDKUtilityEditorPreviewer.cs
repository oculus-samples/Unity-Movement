// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Linq;
using Meta.XR.Movement.Retargeting.Editor;
using Meta.XR.Movement.Retargeting;
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
        public CharacterRetargeter Retargeter
        {
            get => _retargeter;
            set => _retargeter = value;
        }

        [SerializeField]
        private MSDKUtilityEditorWindow _window;

        [SerializeField]
        private GameObject _sceneViewCharacter;

        [SerializeField]
        private CharacterRetargeter _retargeter;

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
            if (_retargeter != null)
            {
                DestroyImmediate(_retargeter.gameObject);
            }

            var previewCharacter = Instantiate(_sceneViewCharacter);
            previewCharacter.name = "Preview-" + _sceneViewCharacter.name;
            previewCharacter.transform.position = _previewPosition;
            SceneManager.MoveGameObjectToScene(previewCharacter, _window.PreviewStage.scene);
            _retargeter = previewCharacter.AddComponent<CharacterRetargeter>();
            _retargeter.ConfigAsset = _window.UtilityConfig.EditorMetadataObject.ConfigJson;
            CharacterRetargeterConfigEditor.LoadConfig(new SerializedObject(_retargeter), _retargeter);
            _retargeter.SkeletonRetargeter.ApplyScale = true;
            _retargeter.SkeletonRetargeter.ScaleRange = new Vector2(0.5f, 2.0f);
            _retargeter.Start();

            var serializedConfig = new SerializedObject(_retargeter);
            CharacterRetargeterConfigEditor.LoadConfig(serializedConfig, _retargeter);
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
        public void UpdateTargetDraw(MSDKUtilityEditorWindow win)
        {
            if (TargetSkeletonDrawTPose == null || win.TargetInfo == null)
            {
                return;
            }

            JointAlignmentUtility.UpdateTPoseData(win.TargetInfo);
            if (win.Step != MSDKUtilityEditorConfig.EditorStep.Configuration &&
                win.Step != MSDKUtilityEditorConfig.EditorStep.Review)
            {
                _window.ModifyConfig(_window.Overlay.ShouldAutoUpdateMappings, false);
            }

            LoadDraw(TargetSkeletonDrawTPose, win.TargetInfo);
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
            var scale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo,
                _window.Step switch
                {
                    MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                    MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                    _ => SkeletonTPoseType.UnscaledTPose
                });
            if (_window.Step == MSDKUtilityEditorConfig.EditorStep.Review)
            {
                var minPoseScale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo,
                    SkeletonTPoseType.MinTPose);
                var maxPoseScale = JointAlignmentUtility.GetDesiredScale(_window.SourceInfo, _window.TargetInfo,
                    SkeletonTPoseType.MaxTPose);
                scale = Mathf.Lerp(minPoseScale, maxPoseScale, _window.UtilityConfig.ScaleSize / 100.0f);
            }

            CalculateSkeletonTPoseByRef(_window.SourceInfo.ConfigHandle, SkeletonType.SourceSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace, scalePercentage, ref sourceTPose);
            MSDKUtilityHelper.ScaleSkeletonPose(_window.SourceInfo.ConfigHandle, SkeletonType.SourceSkeleton,
                ref sourcePose, scale);

            // Run preview retargeter.
            _retargeter.DebugDrawSourceSkeleton = _window.Overlay.ShouldDrawPreview;
            _retargeter.DebugDrawTargetSkeleton = _window.Overlay.ShouldDrawPreview;
            _retargeter.DebugDrawTransform = _retargeter.transform;
            _retargeter.UpdateTPose(sourceTPose);
            _retargeter.CalculatePose(sourcePose);
            _retargeter.UpdatePose();
        }

        /// <summary>
        /// Destroys the preview character retargeter.
        /// </summary>
        public void DestroyPreviewCharacterRetargeter()
        {
            if (_retargeter == null)
            {
                return;
            }

            DestroyImmediate(_retargeter.gameObject);
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

            if (_sceneViewCharacter == null)
            {
                return;
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

            // In the case we don't have a root, create one for previewing
            if (_sceneViewCharacter.transform == rootJoint && _window.PreviewStage.scene.IsValid())
            {
                var previewCharacterParent = new GameObject("PreviewCharacter");
                SceneManager.MoveGameObjectToScene(previewCharacterParent, _window.PreviewStage.scene);
                _sceneViewCharacter.transform.parent = previewCharacterParent.transform;
                _sceneViewCharacter = previewCharacterParent;
            }

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
            if (_sceneViewCharacter == null || target.SkeletonJoints.Length == 0 || target.SkeletonJoints[0] == null)
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
                if (joint == null)
                {
                    continue;
                }

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
                if (joint == null)
                {
                    continue;
                }

                Undo.RecordObject(joint, "Reload Character");
                joint.SetPositionAndRotation(tPose.Position, tPose.Orientation);
            }
        }
    }
}
