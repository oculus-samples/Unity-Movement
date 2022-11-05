// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Assertions;
using static Oculus.Movement.Tracking.HumanBodyBonesMappings;
using static Oculus.Movement.Tracking.SkeletonMetadata;
using System;
using TMPro;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Drives a target, third-party skeleton using a source as a guide.
    /// Source is OVRSkeleton.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class DriveThirdPartySkeleton : MonoBehaviour
    {
        /// <summary>
        /// Adjustments to apply per-joint.
        /// </summary>
        [System.Serializable]
        public class JointAdjustment : ISerializationCallbackReceiver
        {
            public void OnBeforeSerialize()
            {
                // For some reason, Unity does not load the default values specified for each variable.
                // So if this the first time that these variables are being serialized, set those
                // values.
                if (!_hasBeenSerialized)
                {
                    Joint = HumanBodyBones.Hips;
                    RotationChange = Quaternion.identity;
                    JointDisplacement = 0.0f;
                    DisableRotationTransform = false;
                    DisablePositionTransform = false;
                    BoneIdOverrideValue = BodyTrackingBoneId.NoOverride;
                }
                _hasBeenSerialized = true;
            }

            public void OnAfterDeserialize()
            {
                // Nothing to implement
            }

            [SerializeField, HideInInspector]
            private bool _hasBeenSerialized = false;

            /// <summary>
            /// Joint to adjust.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.Joint)]
            public HumanBodyBones Joint;

            /// <summary>
            /// Rotation to apply to the joint, post-retargeting.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.Rotation)]
            public Quaternion RotationChange = Quaternion.identity;

            /// <summary>
            /// Amount to displace the joint, based on percentage of distance to
            /// the next joint.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.JointDisplacement)]
            public float JointDisplacement = 0.0f;

            /// <summary>
            /// Allows disable rotational transform on joint.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.DisableRotationTransform)]
            public bool DisableRotationTransform = false;

            /// <summary>
            /// Allows disable position transform on joint.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.DisablePositionTransform)]
            public bool DisablePositionTransform = false;

            /// <summary>
            /// Allows mapping this human body bone to OVRSkeleton bone different from the
            /// standard. An ignore value indicates to not override; remove means to exclude
            /// from retargeting. Cannot be changed at runtime.
            /// </summary>
            [Tooltip(DriveThirdPartySkeletonTooltips.JointAdjustmentTooltips.BoneIdOverrideValue)]
            public BodyTrackingBoneId BoneIdOverrideValue = BodyTrackingBoneId.NoOverride;
        }

        /// <summary>
        /// Animator on target character that needs to be driven. Keep disabled.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.TargetAnimator)]
        protected Animator _animatorTargetSkeleton;

        /// <summary>
        /// A list of body sections to align. While all bones should be
        /// driven, only a subset will have their axes aligned with source.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BodySectionsToAlign)]
        protected BodySection[] _bodySectionsToAlign =
        {
            BodySection.LeftArm, BodySection.RightArm,
            BodySection.LeftHand, BodySection.RightHand,
            BodySection.Hips, BodySection.Back,
            BodySection.Neck, BodySection.Head
        };

        /// <summary>
        /// A list of body sections to fix the position of by matching against
        /// the body tracking rig. The other bones can be fixed via IK. Back bones
        /// differ among rigs, so be careful about aligning those.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BodySectionsToPosition)]
        protected BodySection[] _bodySectionToPosition =
        {
            BodySection.LeftArm, BodySection.RightArm,
            BodySection.LeftHand, BodySection.RightHand,
            BodySection.Hips, BodySection.Neck,
            BodySection.Head
        };

        /// <summary>
        /// Animator of target character in T-pose.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.AnimatorTargetTPose)]
        protected Animator _animatorTargetTPose;

        /// <summary>
        /// Whether we should update the positions of target character or not.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.UpdatePositions)]
        private bool _updatePositions = true;
        protected bool UpdatePositions => _updatePositions;

        /// <summary>
        /// OVRSkeleton component to query bind pose and bone values from.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.OvrSkeleton)]
        protected OVRSkeleton _ovrSkeleton;

        /// <summary>
        /// Adjustments to apply to certain bones that need to be fixed
        /// post-retargeting.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.Adjustments)]
        protected JointAdjustment[] _adjustments;

        /// <summary>
        /// Allow visualization of gizmos for certain bones for source.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneGizmosSource)]
        protected HumanBodyBones[] _boneGizmosSource;

        /// <summary>
        /// Allow visualization of gizmos for certain bones for target.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneGizmosTarget)]
        protected HumanBodyBones[] _boneGizmosTarget;

        /// <summary>
        /// Allow visualization of gizmos for certain bones for source T-pose.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneGizmosSourceTPose)]
        protected HumanBodyBones[] _boneGizmosTPoseSource;

        /// <summary>
        /// Allow visualization of gizmos for certain bones for target T-pose.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneGizmosTargetTPose)]
        protected HumanBodyBones[] _boneGizmosTPoseTarget;

        /// <summary>
        /// Body sections to show debug text.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BodySectionsToRenderDebugText)]
        protected BodySection[] _bodySectionsToRenderDebugText =
        {
            BodySection.Back, BodySection.Hips,
            BodySection.Neck, BodySection.Head,
            BodySection.LeftLeg, BodySection.RightLeg,
            BodySection.LeftArm, BodySection.RightArm
        };

        /// <summary>
        /// Line renderer of source prefab, allows visual debugging in game view.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.LineRendererSource)]
        protected GameObject _lineRendererSourcePrefab;

        /// <summary>
        /// Line renderer of target prefab, allows visual debugging in game view.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.LineRendererTarget)]
        protected GameObject _lineRendererTargetPrefab;

        /// <summary>
        /// Joint renderer prefab used for source.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneRendererSource)]
        protected GameObject _jointRendererSourcePrefab;

        /// <summary>
        /// Joint renderer prefab used for target.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.BoneRendererTarget)]
        protected GameObject _jointRendererTargetPrefab;

        /// <summary>
        /// Axes debug prefab, source.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.AxisRendererSourcePrefab)]
        protected GameObject _axisRendererSourcePrefab;

        /// <summary>
        /// Axes debug prefab, target.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.AxisRendererTargetPrefab)]
        protected GameObject _axisRendererTargetPrefab;

        /// <summary>
        /// Show or hide debug axes.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.ShowDebugAxes)]
        protected bool _showDebugAxes = false;

        /// <summary>
        /// Show or hide skeletal debug views.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.DebugSkeletalViews)]
        protected bool _showSkeletalDebugViews = false;

        /// <summary>
        /// Renderer of target character.
        /// </summary>
        [SerializeField]
        [Tooltip(DriveThirdPartySkeletonTooltips.TargetRenderer)]
        protected Renderer _targetRenderer;

        private SkeletonMetadata _sourceSkeletonData;
        private SkeletonMetadata _sourceSkeletonTPoseData;
        private SkeletonMetadata _targetSkeletonData;
        private SkeletonMetadata _targetSkeletonTPoseData;

        private const float _FORWARD_AXIS_LEN = 0.6f;
        private const float _UP_AXIS_LEN = 0.3f;
        private const float _RIGHT_AXIS_LEN = 0.3f;
        private const float _JOINT_GIZMO_SPHERE_RAD = 0.01f;

        private const string _SOURCE_PREFIX_TEXT_GIZMO = "S";
        private const string _TARGET_PREFIX_TEXT_GIZMO = "T";

        private int _lastSkelChangeCount = -1;

        private class AnnotatedLineRenderer
        {
            public void SetActiveMode(bool active)
            {
                LineRend.gameObject.SetActive(active);
                TextMesh.gameObject.SetActive(active);
            }

            public bool GetActiveMode()
            {
                return LineRend.gameObject.activeSelf;
            }

            public LineRenderer LineRend;
            public TextMeshPro TextMesh;
        }

        private class InGameDebugRenderObjects
        {
            /// <summary>
            /// Lines are used for rendering joint pairs.
            /// </summary>
            public AnnotatedLineRenderer[] AnnotatedLineRenderers = new AnnotatedLineRenderer[4];

            /// <summary>
            /// Transforms for controlling joint render objects.
            /// </summary>
            public Transform[] JointTransforms = new Transform[4];

            /// <summary>
            /// Axes are used for rendering joint pair axes.
            /// </summary>
            public Transform[] Axes = new Transform[4];
        }

        private Dictionary<HumanBodyBones, InGameDebugRenderObjects> _humanBoneToDebugRenderObjects
            = new Dictionary<HumanBodyBones, InGameDebugRenderObjects>();
        private Dictionary<OVRSkeleton.BoneId, HumanBodyBones> _customBoneIdToHumanBodyBone =
            new Dictionary<OVRSkeleton.BoneId, HumanBodyBones>();

        private void Awake()
        {
            Assert.IsNotNull(_animatorTargetSkeleton);
            Assert.IsNotNull(_animatorTargetTPose);
            Assert.IsNotNull(_ovrSkeleton);
            Assert.IsNotNull(_lineRendererSourcePrefab);
            Assert.IsNotNull(_lineRendererTargetPrefab);

            Assert.IsNotNull(_jointRendererTargetPrefab);
            Assert.IsNotNull(_jointRendererSourcePrefab);
            Assert.IsNotNull(_axisRendererTargetPrefab);
            Assert.IsNotNull(_axisRendererSourcePrefab);
            Assert.IsNotNull(_targetRenderer);
        }

        private void Start()
        {
            CreateCustomBoneIdToHumanBodyBoneMapping();

            _targetSkeletonData = new SkeletonMetadata(_animatorTargetSkeleton);
            Debug.Log("Target party bones, based on Unity humanoid: ");
            _targetSkeletonData.PrintJointPairs();

            _targetSkeletonTPoseData = new SkeletonMetadata(_animatorTargetTPose);
            _targetSkeletonTPoseData.BuildCoordinateAxesForAllBones();
            _targetSkeletonData.BuildCoordinateAxesForAllBones(
                _targetSkeletonTPoseData.BodyToBoneData);
        }

        private void CreateCustomBoneIdToHumanBodyBoneMapping()
        {
            CopyBoneIdToHumanBodyBoneMapping();
            AdjustCustomBoneIdToHumanBodyBoneMapping();
        }

        private void CopyBoneIdToHumanBodyBoneMapping()
        {
            foreach(var keyValuePair in HumanBodyBonesMappings.BoneIdToHumanBodyBone)
            {
                _customBoneIdToHumanBodyBone.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private void AdjustCustomBoneIdToHumanBodyBoneMapping()
        {
            // if there is a mapping override that the user provided,
            // enforce it.
            foreach (var adjustment in _adjustments)
            {
                if (adjustment.BoneIdOverrideValue == BodyTrackingBoneId.NoOverride)
                {
                    continue;
                }

                if (adjustment.BoneIdOverrideValue == BodyTrackingBoneId.Remove)
                {
                    RemoveMappingCorrespondingToHumanBodyBone(adjustment.Joint);
                }
                else
                {
                    _customBoneIdToHumanBodyBone[(OVRSkeleton.BoneId)adjustment.BoneIdOverrideValue]
                        = adjustment.Joint;
                }
            }
        }

        private void RemoveMappingCorrespondingToHumanBodyBone(HumanBodyBones boneId)
        {
            OVRSkeleton.BoneId keyToRemove = OVRSkeleton.BoneId.Max;

            foreach (var key in _customBoneIdToHumanBodyBone.Keys)
            {
                var bone = _customBoneIdToHumanBodyBone[key];
                if (bone == boneId)
                {
                    keyToRemove = key;
                    break;
                }
            }

            if (keyToRemove != OVRSkeleton.BoneId.Max)
            {
                _customBoneIdToHumanBodyBone.Remove(keyToRemove);
            }
        }

        /// <summary>
        /// Toggles the ability of this script to update positions or not.
        /// </summary>
        public void ToggleUpdatePositions()
        {
            _updatePositions = !_updatePositions;
        }

        /// <summary>
        /// Toggles the ability to see skeletal debugging.
        /// </summary>
        public void ToggleSkeletalDebugViews()
        {
            _showSkeletalDebugViews = !_showSkeletalDebugViews;
        }

        /// <summary>
        /// Toggles the ability to see debug axes.
        /// </summary>
        public void ToggleDebugAxes()
        {
            _showDebugAxes = !_showDebugAxes;
        }

        private void ComputeOffsetsUsingSkeletonComponent()
        {
            if (!_ovrSkeleton.IsInitialized ||
                _ovrSkeleton.BindPoses == null || _ovrSkeleton.BindPoses.Count == 0)
            {
                return;
            }

            if (_sourceSkeletonData == null)
            {
                _sourceSkeletonData = new SkeletonMetadata(_ovrSkeleton, false, _customBoneIdToHumanBodyBone);
            }
            else
            {
                _sourceSkeletonData.BuildBoneDataSkeleton(_ovrSkeleton, false, _customBoneIdToHumanBodyBone);
            }
            _sourceSkeletonData.BuildCoordinateAxesForAllBones();

            if (_sourceSkeletonTPoseData == null)
            {
                _sourceSkeletonTPoseData = new SkeletonMetadata(_ovrSkeleton, true, _customBoneIdToHumanBodyBone);
            }
            else
            {
                _sourceSkeletonTPoseData.BuildBoneDataSkeleton(_ovrSkeleton, true, _customBoneIdToHumanBodyBone);
            }
            _sourceSkeletonTPoseData.BuildCoordinateAxesForAllBones();

            var binePoses = _ovrSkeleton.BindPoses;
            int numBones = binePoses.Count;
            var targetBoneDataMap = _targetSkeletonData.BodyToBoneData;
            for (int i = 0; i < numBones; i++)
            {
                var currBindPose = binePoses[i];
                var skelBoneId = currBindPose.Id;
                if (!_customBoneIdToHumanBodyBone.ContainsKey(skelBoneId))
                {
                    continue;
                }

                var humanBodyBone = _customBoneIdToHumanBodyBone[skelBoneId];
                if (!targetBoneDataMap.ContainsKey(humanBodyBone))
                {
                    continue;
                }

                targetBoneDataMap[humanBodyBone].CorrectionQuaternion = Quaternion.identity;
                var bodySection = BoneToBodySection[humanBodyBone];
                if (IsBodySectionInArray(bodySection, _bodySectionsToAlign) &&
                    _sourceSkeletonTPoseData.BodyToBoneData.ContainsKey(humanBodyBone))
                {
                    var sourceTPoseOrientation =
                         _sourceSkeletonTPoseData.BodyToBoneData[humanBodyBone].JointPairOrientation;
                    var targetTPoseOrientation =
                        _targetSkeletonTPoseData.BodyToBoneData[humanBodyBone].JointPairOrientation;

                    Vector3 forwardSource = sourceTPoseOrientation * Vector3.forward;
                    Vector3 forwardTarget = targetTPoseOrientation * Vector3.forward;
                    var targetToSrc = Quaternion.FromToRotation(forwardTarget,
                        forwardSource);

                    var targetJoint = _animatorTargetTPose.GetBoneTransform(humanBodyBone);
                    var sourceRotationValueInv = Quaternion.Inverse(currBindPose.Transform.rotation);
                    var targetRotationInTPose = targetJoint.rotation;
                    var targetData = targetBoneDataMap[humanBodyBone];

                    targetData.CorrectionQuaternion =
                        sourceRotationValueInv * targetToSrc * targetRotationInTPose;
                }
            }
            _lastSkelChangeCount = _ovrSkeleton.SkeletonChangedCount;
        }

        private void OnDrawGizmos()
        {
            if (!_showSkeletalDebugViews)
            {
                return;
            }

            if (_targetSkeletonData != null)
            {
                DrawGizmosForBothBoneDatasTarget(
                   _targetSkeletonData.BodyToBoneData,
                   _boneGizmosTarget);
            }

            if (_sourceSkeletonTPoseData != null)
            {
                DrawGizmosForBothBoneDatasSource(
                    _sourceSkeletonTPoseData.BodyToBoneData,
                    _boneGizmosTPoseSource);
            }

            if (_targetSkeletonTPoseData != null)
            {
                DrawGizmosForBothBoneDatasSource(
                    _targetSkeletonTPoseData.BodyToBoneData,
                    _boneGizmosTPoseTarget);
            }
        }

        private void DrawGizmosForBothBoneDatasSource(
            Dictionary<HumanBodyBones, BoneData> boneToDataSource,
            HumanBodyBones[] boneGizmosSource)
        {
            foreach (var pair in boneToDataSource)
            {
                var humanBodyBone = pair.Key;
                if (ShouldVisualizeBoneGizmos(humanBodyBone, boneGizmosSource))
                {
                    var valueSource = pair.Value;
                    DrawJointPair(pair.Key, valueSource, _SOURCE_PREFIX_TEXT_GIZMO, Color.green,
                        Color.green);
                }
            }
        }

        private void DrawGizmosForBothBoneDatasTarget(
            Dictionary<HumanBodyBones, BoneData> boneToDataTarget,
            HumanBodyBones[] boneGizmosTarget)
        {
            foreach (var pair in boneToDataTarget)
            {
                var humanBodyBone = pair.Key;

                if (ShouldVisualizeBoneGizmos(humanBodyBone, boneGizmosTarget) &&
                    !boneToDataTarget.ContainsKey(humanBodyBone))
                {
                    var valueTarget = boneToDataTarget[pair.Key];
                    DrawJointPair(pair.Key, valueTarget, _TARGET_PREFIX_TEXT_GIZMO, Color.yellow,
                        Color.yellow);
                }
            }
        }

        private bool ShouldVisualizeBoneGizmos(HumanBodyBones candidateBone,
            HumanBodyBones[] boneGizmos)
        {
            if (boneGizmos.Length == 0)
            {
                return false;
            }

            foreach (var boneId in boneGizmos)
            {
                if (candidateBone == boneId)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawJointPair(HumanBodyBones bodyBone, BoneData boneData, string prefix,
            Color labelColor, Color jointPairColor)
        {
            var startPos = boneData.OriginalJoint.position;

            var fromPosition = boneData.JointPairStart.position;
            var toPosition = Vector3.zero;
            // edge case: joint pair end is null or same node. If that's the case,
            // make joint pair end follow the axis from parent node
            if (boneData.JointPairEnd == null ||
                boneData.JointPairEnd == boneData.JointPairStart ||
                (boneData.JointPairEnd.position - boneData.JointPairStart.position).sqrMagnitude < Mathf.Epsilon)
            {
                var node1 = boneData.ParentTransform;
                var node2 = boneData.JointPairStart;
                fromPosition = boneData.OriginalJoint.position;
                toPosition = fromPosition + (node2.position - node1.position);

                Debug.LogWarning($"Degenerate joint pair for {boneData.OriginalJoint}.");
            }
            else
            {
                toPosition = boneData.JointPairEnd.position;
            }

            var jointPairLength = (toPosition - fromPosition).magnitude;

#if UNITY_EDITOR
            Handles.color = labelColor;
            Handles.Label(fromPosition, $"{prefix}-{boneData.OriginalJoint.name}-{bodyBone}");
#endif
            Gizmos.color = labelColor;
            Gizmos.DrawSphere(startPos, _JOINT_GIZMO_SPHERE_RAD);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPos,
                startPos +
                boneData.JointPairOrientation * Vector3.forward * jointPairLength * _FORWARD_AXIS_LEN);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos,
                startPos +
                boneData.JointPairOrientation * Vector3.up * jointPairLength * _UP_AXIS_LEN);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPos,
                startPos +
                boneData.JointPairOrientation * Vector3.right * jointPairLength * _RIGHT_AXIS_LEN);

            Gizmos.color = jointPairColor;
            Gizmos.DrawLine(startPos, toPosition);
        }

        private void Update()
        {
            if (_lastSkelChangeCount != _ovrSkeleton.SkeletonChangedCount)
            {
                ComputeOffsetsUsingSkeletonComponent();
            }

            if (_showSkeletalDebugViews || _showDebugAxes)
            {
                UpdateSourceAndTargetSkeletons();
                InitializeRenderObjectsDictionary();
            }

            if (_showSkeletalDebugViews)
            {
                UpdateLineRenderers();
                UpdateBoneRenderers();
            }

            if (_showDebugAxes)
            {
                UpdateAxesRenderers();
            }

            UpdateActiveStates();

            AlignTargetWithSource();
        }

        private void UpdateSourceAndTargetSkeletons()
        {
            if (_sourceSkeletonData != null)
            {
                _sourceSkeletonData.BuildCoordinateAxesForAllBones();
                DrawGizmosForBothBoneDatasSource(
                    _sourceSkeletonData.BodyToBoneData,
                    _boneGizmosSource);
            }

            if (_targetSkeletonData != null && _sourceSkeletonData != null)
            {
                _targetSkeletonData.BuildCoordinateAxesForAllBones(
                    _sourceSkeletonData.BodyToBoneData);
            }
        }

        private void InitializeRenderObjectsDictionary()
        {
            if (_humanBoneToDebugRenderObjects.Count > 0)
            {
                return;
            }

            HumanBodyBones[] boneEnumValues = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
            foreach (var boneEnum in boneEnumValues)
            {
                if (!_humanBoneToDebugRenderObjects.ContainsKey(boneEnum))
                {
                    InGameDebugRenderObjects newRenderObjectType = new InGameDebugRenderObjects();
                    _humanBoneToDebugRenderObjects[boneEnum] = newRenderObjectType;
                }
            }
        }

        private void UpdateLineRenderers()
        {
            UpdateLineRenderers(_lineRendererSourcePrefab, _sourceSkeletonData, 0);
            UpdateLineRenderers(_lineRendererTargetPrefab, _targetSkeletonData, 1);
            UpdateLineRenderers(_lineRendererSourcePrefab, _sourceSkeletonTPoseData, 2);
            UpdateLineRenderers(_lineRendererTargetPrefab, _targetSkeletonTPoseData, 3, true);
        }

        private void UpdateBoneRenderers()
        {
            UpdateJointRenderers(_jointRendererSourcePrefab, _sourceSkeletonData, 0);
            UpdateJointRenderers(_jointRendererTargetPrefab, _targetSkeletonData, 1);
            UpdateJointRenderers(_jointRendererSourcePrefab, _sourceSkeletonTPoseData, 2);
            UpdateJointRenderers(_jointRendererTargetPrefab, _targetSkeletonData, 3);
        }

        private void UpdateAxesRenderers()
        {
            UpdateAxesRenderers(_axisRendererSourcePrefab, _sourceSkeletonData, 0);
            UpdateAxesRenderers(_axisRendererTargetPrefab, _targetSkeletonData, 1);
            UpdateAxesRenderers(_axisRendererSourcePrefab, _sourceSkeletonTPoseData, 2);
            UpdateAxesRenderers(_axisRendererTargetPrefab, _targetSkeletonTPoseData, 3);
        }

        private void UpdateLineRenderers(
            GameObject prefabToUse,
            SkeletonMetadata metadata,
            int lineIndex,
            bool print = false)
        {
            if (metadata == null)
            {
                return;
            }

            var boneDataMap = metadata.BodyToBoneData;
            foreach (var key in boneDataMap.Keys)
            {
                var renderObject = _humanBoneToDebugRenderObjects[key];
                var boneData = boneDataMap[key];

                if (renderObject.AnnotatedLineRenderers[lineIndex] == null)
                {
                    renderObject.AnnotatedLineRenderers[lineIndex] = new AnnotatedLineRenderer();
                    var newObject = GameObject.Instantiate(prefabToUse);
                    newObject.name += $".{key}";
                    renderObject.AnnotatedLineRenderers[lineIndex].LineRend =
                        newObject.GetComponent<LineRenderer>();
                    renderObject.AnnotatedLineRenderers[lineIndex].TextMesh =
                        renderObject.AnnotatedLineRenderers[lineIndex].LineRend.GetComponentInChildren<TextMeshPro>();

                    var bodySection = BoneToBodySection[key];
                    // don't enable text for hands, creates noisy text
                    if (IsBodySectionInArray(bodySection, _bodySectionsToRenderDebugText))
                    {
                        renderObject.AnnotatedLineRenderers[lineIndex].TextMesh.text = key.ToString();
                    }
                }

                renderObject.AnnotatedLineRenderers[lineIndex].TextMesh.transform.position = boneData.FromPosition;
                renderObject.AnnotatedLineRenderers[lineIndex].LineRend.SetPosition(0, boneData.FromPosition);
                renderObject.AnnotatedLineRenderers[lineIndex].LineRend.SetPosition(1, boneData.ToPosition);
            }
        }

        private void UpdateJointRenderers(
            GameObject prefabToUse,
            SkeletonMetadata metadata,
            int jointIndex)
        {
            if (metadata == null)
            {
                return;
            }

            var boneDataMap = metadata.BodyToBoneData;
            foreach (var key in boneDataMap.Keys)
            {
                var renderObject = _humanBoneToDebugRenderObjects[key];
                var boneData = boneDataMap[key];

                if (renderObject.JointTransforms[jointIndex] == null)
                {
                    renderObject.JointTransforms[jointIndex] =
                        GameObject.Instantiate(prefabToUse).transform;
                }
                renderObject.JointTransforms[jointIndex].position = boneData.OriginalJoint.position;
                renderObject.JointTransforms[jointIndex].rotation = boneData.OriginalJoint.rotation;
            }
        }

        private void UpdateAxesRenderers(
            GameObject prefabToUse,
            SkeletonMetadata metadata,
            int axesIndex)
        {
            if (metadata == null)
            {
                return;
            }

            var boneDataMap = metadata.BodyToBoneData;
            foreach (var key in boneDataMap.Keys)
            {
                var renderObject = _humanBoneToDebugRenderObjects[key];
                var boneData = boneDataMap[key];

                if (renderObject.Axes[axesIndex] == null)
                {
                    renderObject.Axes[axesIndex] =
                        GameObject.Instantiate(prefabToUse).transform;
                }
                renderObject.Axes[axesIndex].position = boneData.OriginalJoint.position;
                renderObject.Axes[axesIndex].rotation = boneData.JointPairOrientation;
            }
        }

        private void UpdateActiveStates()
        {
            foreach (var value in _humanBoneToDebugRenderObjects.Values)
            {
                foreach (var lineRend in value.AnnotatedLineRenderers)
                {
                    if (lineRend != null &&
                        lineRend.GetActiveMode() != _showSkeletalDebugViews)
                    {
                        lineRend.SetActiveMode(_showSkeletalDebugViews);
                    }
                }

                foreach (var jointTransform in value.JointTransforms)
                {
                    if (jointTransform != null &&
                        jointTransform.gameObject.activeSelf != _showSkeletalDebugViews)
                    {
                        jointTransform.gameObject.SetActive(_showSkeletalDebugViews);
                    }
                }

                foreach (var axesTransform in value.Axes)
                {
                    if (axesTransform != null &&
                        axesTransform.gameObject.activeSelf != _showDebugAxes)
                    {
                        axesTransform.gameObject.SetActive(_showDebugAxes);
                    }
                }
            }

            _targetRenderer.enabled = !_showDebugAxes && !_showSkeletalDebugViews;
        }

        private void AlignTargetWithSource()
        {
            if (!_ovrSkeleton.IsInitialized ||
                _ovrSkeleton.Bones == null || _ovrSkeleton.Bones.Count == 0)
            {
                return;
            }

            var bones = _ovrSkeleton.Bones;
            var numBones = bones.Count;
            var targetBoneDataMap = _targetSkeletonData.BodyToBoneData;

            for (int i = 0; i < numBones; i++)
            {
                var currBone = bones[i];
                var skelBoneId = currBone.Id;
                if (!_customBoneIdToHumanBodyBone.ContainsKey(skelBoneId))
                {
                    continue;
                }

                var humanBodyBone = _customBoneIdToHumanBodyBone[skelBoneId];
                if (!targetBoneDataMap.ContainsKey(humanBodyBone))
                {
                    continue;
                }
                var targetData = targetBoneDataMap[humanBodyBone];
                // Skip if we can map the joint at all.
                if (!targetData.CorrectionQuaternion.HasValue)
                {
                    continue;
                }

                var targetJoint = targetData.OriginalJoint;
                var correctionQuaternion = targetData.CorrectionQuaternion.Value;
                var adjustment = FindAdjustment(humanBodyBone);

                var bodySectionOfJoint = BoneToBodySection[humanBodyBone];
                bool shouldUpdatePosition = IsBodySectionInArray(
                    bodySectionOfJoint, _bodySectionToPosition);

                if (adjustment == null)
                {
                    targetJoint.rotation = currBone.Transform.rotation * correctionQuaternion;
                    if (_updatePositions && shouldUpdatePosition)
                    {
                        targetJoint.position = currBone.Transform.position;
                    }
                }
                else
                {
                    if (!adjustment.DisableRotationTransform)
                    {
                        targetJoint.rotation = currBone.Transform.rotation * correctionQuaternion;
                    }
                    targetJoint.rotation *= adjustment.RotationChange;
                    if (!adjustment.DisablePositionTransform && _updatePositions
                        && shouldUpdatePosition)
                    {
                        targetJoint.position = currBone.Transform.position;
                    }
                }
            }

            // Apply positional tweaks after the fact.
            if (_updatePositions)
            {
                ApplyPositionAdjustments();
            }
        }

        private bool IsBodySectionInArray(
            BodySection bodySectionToCheck,
            BodySection[] sectionArrayToCheck)
        {
            foreach (var bodySection in sectionArrayToCheck)
            {
                if (bodySection == bodySectionToCheck)
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyPositionAdjustments()
        {
            var bones = _ovrSkeleton.Bones;
            var numBones = bones.Count;
            var targetBoneDataMap = _targetSkeletonData.BodyToBoneData;
            // tweak positions once all positions are set
            for (int i = 0; i < numBones; i++)
            {
                var currBone = bones[i];
                var skelBoneId = currBone.Id;
                if (!_customBoneIdToHumanBodyBone.ContainsKey(skelBoneId))
                {
                    continue;
                }

                var humanBodyBone = _customBoneIdToHumanBodyBone[skelBoneId];
                if (!targetBoneDataMap.ContainsKey(humanBodyBone))
                {
                    continue;
                }
                var targetData = targetBoneDataMap[humanBodyBone];
                // Skip if we can map the joint at all.
                if (!targetData.CorrectionQuaternion.HasValue)
                {
                    continue;
                }

                var targetJoint = targetData.OriginalJoint;
                var adjustment = FindAdjustment(humanBodyBone);
                if (adjustment != null && Mathf.Abs(adjustment.JointDisplacement) > Mathf.Epsilon)
                {
                    var adjustmentAxis = targetData.JointPairOrientation * Vector3.forward;
                    var boneLength = adjustmentAxis.magnitude;
                    if (boneLength > Mathf.Epsilon)
                    {
                        targetJoint.position +=
                            adjustmentAxis * adjustment.JointDisplacement;
                    }
                }
            }
        }

        private JointAdjustment FindAdjustment(HumanBodyBones boneId)
        {
            for (int i = 0; i < _adjustments.Length; i++)
            {
                if (_adjustments[i].Joint == boneId)
                {
                    return _adjustments[i];
                }
            }

            return null;
        }
    }
}
