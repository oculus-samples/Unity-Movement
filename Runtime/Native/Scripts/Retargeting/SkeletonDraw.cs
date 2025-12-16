// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.XR.Movement.Retargeting
{
    [Serializable]
    public class SkeletonDraw
    {
        /// <summary>
        /// Gets or sets the color used for drawing the skeleton.
        /// </summary>
        public Color TintColor
        {
            get => _tintColor;
            set => _tintColor = value;
        }

        /// <summary>
        /// Gets or sets the thickness of the lines used for drawing the skeleton.
        /// </summary>
        public float LineThickness
        {
            get => _lineThickness;
            set => _lineThickness = value;
        }

        /// <summary>
        /// Gets or sets the array of parent joint positions.
        /// </summary>
        public Vector3[] ParentPositions
        {
            get => _parentPositions;
            set => _parentPositions = value;
        }

        /// <summary>
        /// Gets or sets the array of child joint positions.
        /// </summary>
        public Vector3[] ChildPositions
        {
            get => _childPositions;
            set => _childPositions = value;
        }

        /// <summary>
        /// Gets or sets the array of parent joint names.
        /// </summary>
        public string[] ParentNames
        {
            get => _parentNames;
            set => _parentNames = value;
        }

        /// <summary>
        /// Gets or sets the array of child joint names.
        /// </summary>
        public string[] ChildNames
        {
            get => _childNames;
            set => _childNames = value;
        }

        /// <summary>
        /// Gets or sets the list of joint indices to ignore when drawing the skeleton.
        /// </summary>
        public List<int> IndexesToIgnore
        {
            get => _indexesToIgnore;
            set => _indexesToIgnore = value;
        }

        /// <summary>
        /// Gets or sets whether to draw labels for the joints.
        /// </summary>
        public bool DrawLabels
        {
            get => _drawLabels;
            set => _drawLabels = value;
        }

        /// <summary>
        /// The color used for drawing the skeleton.
        /// </summary>
        [SerializeField]
        private Color _tintColor;

        /// <summary>
        /// The thickness of the lines used for drawing the skeleton.
        /// </summary>
        [SerializeField]
        private float _lineThickness;

        /// <summary>
        /// Whether to draw labels for the joints.
        /// </summary>
        [SerializeField]
        private bool _drawLabels;

        /// <summary>
        /// Array of parent joint positions.
        /// </summary>
        [SerializeField]
        private Vector3[] _parentPositions;

        /// <summary>
        /// Array of child joint positions.
        /// </summary>
        [SerializeField]
        private Vector3[] _childPositions;

        /// <summary>
        /// Array of last valid parent joint positions.
        /// </summary>
        [SerializeField]
        private Vector3[] _lastValidParentPositions;

        /// <summary>
        /// Array of last valid child joint positions.
        /// </summary>
        [SerializeField]
        private Vector3[] _lastValidChildPositions;

        /// <summary>
        /// Array of joint indices to ignore when drawing the skeleton.
        /// </summary>
        [SerializeField]
        private bool[] _runtimeIndexesToIgnore;

        /// <summary>
        /// Array of parent joint names.
        /// </summary>
        [SerializeField]
        private string[] _parentNames;

        /// <summary>
        /// Array of child joint names.
        /// </summary>
        [SerializeField]
        private string[] _childNames;

        /// <summary>
        /// List of joint indices to ignore when drawing the skeleton.
        /// </summary>
        [SerializeField]
        private List<int> _indexesToIgnore = new();

        /// <summary>
        /// Array of parent GameObjects corresponding to the joints.
        /// </summary>
        [SerializeField]
        private GameObject[] _parentObjects;

        /// <summary>
        /// Array of custom bone colors per bone index.
        /// </summary>
        private Color?[] _boneColors;

#if UNITY_EDITOR
        private static int _skeletonDrawHash = "SkeletonDrawHandle".GetHashCode();
#endif

        /// <summary>
        /// Delegate that filters bone indices. Use this to indicate if a bone
        /// should be visualized or not.
        /// </summary>
        /// <param name="boneIndex">Bone index.</param>
        /// <returns>True if index should be visualized, false if not.</returns>
        public delegate bool AllowBoneVisualDelegate(int boneIndex);

        /// <summary>
        /// Main initializer for <see cref="SkeletonDraw"/>.
        /// </summary>
        /// <param name="color">The default color.</param>
        /// <param name="thickness">Thickness.</param>
        public void InitDraw(Color color, float thickness = 0.04f)
        {
            TintColor = color;
            LineThickness = thickness;
        }

        /// <summary>
        /// Sets the bone color by index.
        /// </summary>
        /// <param name="index">Index to set the color for.</param>
        /// <param name="newColor">The new color.</param>
        public void SetBoneColor(int index, Color newColor)
        {
            if (_boneColors == null || _boneColors.Length <= index)
            {
                var oldLength = _boneColors?.Length ?? 0;
                var newColors = new Color?[Mathf.Max(index + 1, oldLength * 2)];
                if (_boneColors != null)
                {
                    Array.Copy(_boneColors, newColors, oldLength);
                }
                _boneColors = newColors;
            }

            _boneColors[index] = newColor;
        }

        /// <summary>
        /// Draws the skeleton using the current positions and settings.
        /// </summary>
        public void Draw()
        {
            if (_parentPositions == null || _childPositions == null ||
                _parentPositions.Length == 0 || _childPositions.Length == 0 ||
                _parentPositions.Length != _childPositions.Length)
            {
                _parentPositions ??= _lastValidParentPositions ?? _parentPositions;
                _childPositions ??= _lastValidChildPositions ?? _childPositions;
            }
            else
            {
                _lastValidParentPositions = _parentPositions ?? _lastValidParentPositions;
                _lastValidChildPositions = _childPositions ?? _lastValidChildPositions;
            }
            if (_parentPositions == null || _childPositions == null)
            {
                return;
            }

            for (var i = 0; i < _parentPositions.Length; i++)
            {
                if (_indexesToIgnore.Contains(i) || _runtimeIndexesToIgnore[i])
                {
                    continue;
                }

                var boneColor = _boneColors != null && i < _boneColors.Length && _boneColors[i].HasValue
                    ? _boneColors[i].Value
                    : TintColor;

                var parentPos = _parentPositions[i];
                var childPos = _childPositions[i];
                var length = Vector3.Distance(parentPos, childPos);

                if (length > 0f)
                {
                    var rotation = Quaternion.FromToRotation(Vector3.forward, childPos - parentPos);
                    MeshDraw.DrawBone(boneColor, parentPos, rotation, LineThickness, length);
                }

                if (_parentObjects != null && i < _parentObjects.Length)
                {
                    HandleMouseClick(i, _parentObjects[i], _parentPositions[i], _childPositions[i]);
                }
            }
        }

        /// <summary>
        /// Loads joints of a skeleton to be visualized. Allows filtering
        /// out bones that should not visualized via an optional delegate.
        /// </summary>
        /// <param name="jointCount">Number of joints in the skeleton.</param>
        /// <param name="parentIndices">Array of parent indices for each joint.</param>
        /// <param name="pose">Array of joint poses.</param>
        /// <param name="filterDelegate">Optional delegate to filter out joints that should not be visualized.</param>
        public void LoadDraw(
            int jointCount,
            int[] parentIndices,
            NativeArray<NativeTransform> pose,
            AllowBoneVisualDelegate filterDelegate = null)
        {
            if (_parentPositions?.Length != jointCount)
            {
                _parentPositions = new Vector3[jointCount];
            }

            if (_childPositions?.Length != jointCount)
            {
                _childPositions = new Vector3[jointCount];
            }

            if (_runtimeIndexesToIgnore?.Length != jointCount)
            {
                _runtimeIndexesToIgnore = new bool[jointCount];
            }

            for (var i = 0; i < jointCount; i++)
            {
                var childPose = pose[i];
                var parentIndex = parentIndices[i];
                var delegatePasses = filterDelegate == null || filterDelegate(i);
                if (parentIndex < 0 || !delegatePasses)
                {
                    _parentPositions[i] = Vector3.zero;
                    _runtimeIndexesToIgnore[i] = true;
                }
                else
                {
                    var parentPose = pose[parentIndex];
                    if (parentPose.Scale == Vector3.zero)
                    {
                        _runtimeIndexesToIgnore[i] = true;
                    }
                    _parentPositions[i] = parentPose.Position;
                }

                _childPositions[i] = childPose.Position;
            }
        }

        /// <summary>
        /// Loads joints of a skeleton to be visualized with names and transforms.
        /// </summary>
        /// <param name="jointCount">Number of joints in the skeleton.</param>
        /// <param name="parentIndices">Array of parent indices for each joint.</param>
        /// <param name="pose">Array of joint poses.</param>
        /// <param name="jointNames">Array of joint names.</param>
        /// <param name="joints">Array of joint transforms.</param>
        public void LoadDraw(
            int jointCount,
            int[] parentIndices,
            NativeTransform[] pose,
            string[] jointNames,
            Transform[] joints)
        {
            if (_parentPositions?.Length != jointCount)
            {
                _parentPositions = new Vector3[jointCount];
            }

            if (_childPositions?.Length != jointCount)
            {
                _childPositions = new Vector3[jointCount];
            }

            if (_runtimeIndexesToIgnore?.Length != jointCount)
            {
                _runtimeIndexesToIgnore = new bool[jointCount];
            }

            if (_parentNames?.Length != jointCount)
            {
                _parentNames = new string[jointCount];
            }

            if (_childNames?.Length != jointCount)
            {
                _childNames = new string[jointCount];
            }

            for (var i = 0; i < jointCount; i++)
            {
                var parentIndex = parentIndices[i];
                if (parentIndex < 0)
                {
                    _parentPositions[i] = Vector3.zero;
                    if (jointNames != null)
                    {
                        _parentNames[i] = "";
                    }
                }
                else
                {
                    _parentPositions[i] = pose[parentIndex].Position;
                    if (jointNames != null)
                    {
                        _parentNames[i] = jointNames[parentIndex];
                    }
                }

                if (jointNames != null)
                {
                    _childNames[i] = jointNames[i];
                }

                if (jointNames != null && _childNames[i].Contains("WristTwist"))
                {
                    _childPositions[i] = _parentPositions[i];
                }
                else
                {
                    _childPositions[i] = pose[i].Position;
                }
            }

            if (joints is { Length: > 0 })
            {
                _parentObjects = joints.Where(t => t != null).Select(t => t.gameObject).ToArray();
            }
        }


        private void HandleMouseClick(int index, GameObject parent, Vector3 parentPos, Vector3 childPos)
        {
#if UNITY_EDITOR
            Event e = Event.current;
            var id = GUIUtility.GetControlID(_skeletonDrawHash, FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.Layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(parentPos, childPos));
                    break;
                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == id)
                    {
                        HandleUtility.Repaint();
                    }

                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && e.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        Selection.activeGameObject = parent;
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && e.button is 0 or 2)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }

                    break;
            }
#endif
        }
    }

    /// <summary>
    /// Allows useful visual debugging of body tracking data.
    /// </summary>
    [Serializable]
    public class SkeletonDebugVisuals
    {
        private Transform _leftInputProxy, _rightInputProxy, _centerEyeProxy;

        /// <summary>
        /// Clean up any unmanaged objects that this class created.
        /// </summary>
        public void CleanUp()
        {
            if (_leftInputProxy != null)
            {
                Object.Destroy(_leftInputProxy);
            }

            if (_rightInputProxy != null)
            {
                Object.Destroy(_rightInputProxy);
            }

            if (_centerEyeProxy != null)
            {
                Object.Destroy(_centerEyeProxy);
            }
        }

        /// <summary>
        /// Updates the tracker proxies used to visualize tracker input positions.
        /// </summary>
        /// <param name="handle">Native handle.</param>
        /// <param name="parentTransform">Parent transform for the proxies.</param>
        /// <param name="leftInputPoseTrackingSpace">Left input pose in tracking space.</param>
        /// <param name="rightInputPoseTrackingSpace">Right input pose in tracking space.</param>
        /// <param name="centerEyePoseTrackingSpace">Center eye input pose in tracking space.</param>
        /// <param name="renderTime">Render time.</param>
        /// <param name="interpolate">Whether to interpolate or not.</param>
        /// <param name="areWorldSpacePoses">Whether the input poses are world-space or not.</param>
        public void UpdateTrackerProxies(
            ulong handle,
            Transform parentTransform,
            Pose leftInputPoseTrackingSpace,
            Pose rightInputPoseTrackingSpace,
            Pose centerEyePoseTrackingSpace,
            float renderTime,
            bool interpolate,
            bool areWorldSpacePoses)
        {
            // Whether we are interpolating or not, use the values passed in as
            // the default.
            Pose finalLeftPose = leftInputPoseTrackingSpace,
                finalRightPose = rightInputPoseTrackingSpace,
                finalCenterEyePose = centerEyePoseTrackingSpace;

            if (interpolate)
            {
                NativeTransform centerEye = new NativeTransform(),
                    leftInput = new NativeTransform(),
                    rightInput = new NativeTransform();
                if (GetInterpolatedJointPose(handle, TrackerJointType.CenterEye, ref centerEye,
                        renderTime))
                {
                    finalCenterEyePose = new Pose(centerEye.Position, centerEye.Orientation);
                }

                if (GetInterpolatedJointPose(handle, TrackerJointType.RightInput, ref rightInput,
                        renderTime))
                {
                    finalRightPose = new Pose(rightInput.Position, rightInput.Orientation);
                }

                if (GetInterpolatedJointPose(handle, TrackerJointType.LeftInput, ref leftInput,
                        renderTime))
                {
                    finalLeftPose = new Pose(leftInput.Position, leftInput.Orientation);
                }
            }

            UpdateTrackerProxyTransforms(
                parentTransform,
                finalLeftPose,
                finalRightPose,
                finalCenterEyePose,
                areWorldSpacePoses);
        }

        private void UpdateTrackerProxyTransforms(
            Transform parentTransform,
            Pose leftInputPoseTrackingSpace,
            Pose rightInputPoseTrackingSpace,
            Pose centerEyePoseTrackingSpace,
            bool areWorldSpacePoses)
        {
            var trackingSpaceTransform = SkeletonUtilities.GetTrackingSpaceTransform();
            if (trackingSpaceTransform == null)
            {
                Debug.LogWarning("Tracking space transform is null. Can't transform the input poses properly");
                return;
            }

            if (_leftInputProxy == null)
            {
                _leftInputProxy = CreateTransformProxy(parentTransform, "LeftInputProxy");
            }

            if (_rightInputProxy == null)
            {
                _rightInputProxy = CreateTransformProxy(parentTransform, "RightInputProxy");
            }

            if (_centerEyeProxy == null)
            {
                _centerEyeProxy = CreateTransformProxy(parentTransform, "CenterEyeProxy");
            }

            // if worldspace, don't transform relative to the tracking space.
            // because they are already in world space and don't need an anchor transform.
            _leftInputProxy.SetPositionAndRotation(
                areWorldSpacePoses
                    ? leftInputPoseTrackingSpace.position
                    : trackingSpaceTransform.TransformPoint(leftInputPoseTrackingSpace.position),
                areWorldSpacePoses
                    ? leftInputPoseTrackingSpace.rotation
                    : trackingSpaceTransform.rotation * leftInputPoseTrackingSpace.rotation);
            _rightInputProxy.SetPositionAndRotation(
                areWorldSpacePoses
                    ? rightInputPoseTrackingSpace.position
                    : trackingSpaceTransform.TransformPoint(rightInputPoseTrackingSpace.position),
                areWorldSpacePoses
                    ? rightInputPoseTrackingSpace.rotation
                    : trackingSpaceTransform.rotation * rightInputPoseTrackingSpace.rotation);
            _centerEyeProxy.SetPositionAndRotation(
                areWorldSpacePoses
                    ? centerEyePoseTrackingSpace.position
                    : trackingSpaceTransform.TransformPoint(centerEyePoseTrackingSpace.position),
                areWorldSpacePoses
                    ? centerEyePoseTrackingSpace.rotation
                    : trackingSpaceTransform.rotation * centerEyePoseTrackingSpace.rotation);
        }

        private Transform CreateTransformProxy(Transform parentTransform, string nameOfProxy)
        {
            var newProxy = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            newProxy.name = nameOfProxy;
            newProxy.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            newProxy.localScale = 0.05f * Vector3.one;
            newProxy.SetParent(parentTransform, true);
            return newProxy;
        }
    }
}
