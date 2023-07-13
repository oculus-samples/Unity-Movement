// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows visualizing bones found in an OVRSkeleton component.
    /// </summary>
    [DefaultExecutionOrder(230)]
    public class OVRSkeletonBoneVisualizer : MonoBehaviour
    {
        [Serializable]
        protected class BoneVisualData
        {
            /// <summary>
            /// Indicates if bone should be visualized or not.
            /// </summary>
            /// <param name="bone">Bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool BoneShouldBeVisualized(OVRSkeleton.BoneId bone)
            {
                if (BonesToVisualize == null || BonesToVisualize.Length == 0)
                {
                    return false;
                }
                foreach (var currentBone in BonesToVisualize)
                {
                    if ((OVRSkeleton.BoneId)currentBone == bone)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Sets the bone visual array to all bones possible.
            /// </summary>
            public void FillArrayWithAllBones()
            {
                BonesToVisualize = new CustomMappings.BodyTrackingBoneId[(int)CustomMappings.BodyTrackingBoneId.Body_End];
                for (var currentBone = CustomMappings.BodyTrackingBoneId.Body_Hips;
                    currentBone < CustomMappings.BodyTrackingBoneId.Body_End;
                    currentBone++)
                {
                    BonesToVisualize[(int)currentBone] = currentBone;
                }
            }

            /// <summary>
            /// Bones to visualize.
            /// </summary>
            public CustomMappings.BodyTrackingBoneId[] BonesToVisualize;
        }

        /// <summary>
        /// Bone tuple class.
        /// </summary>
        [Serializable]
        public class BoneTuple
        {
            /// <summary>
            /// Bone tuple constructor.
            /// </summary>
            /// <param name="firstBone">First bone.</param>
            /// <param name="secondBone">Second bone.</param>
            public BoneTuple(
                CustomMappings.BodyTrackingBoneId firstBone,
                CustomMappings.BodyTrackingBoneId secondBone)
            {
                FirstBone = firstBone;
                SecondBone = secondBone;
            }

            /// <summary>
            /// First bone in tuple.
            /// </summary>
            public CustomMappings.BodyTrackingBoneId FirstBone;
            /// <summary>
            /// Second bone in tuple.
            /// </summary>
            public CustomMappings.BodyTrackingBoneId SecondBone;
        }

        /// <summary>
        /// Allows rendering custom bone tuples.
        /// </summary>
        [Serializable]
        public class CustomBoneVisualData
        {
            /// <summary>
            /// Indicates if bone should be visualized or not.
            /// </summary>
            /// <param name="bone">Bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool BoneShouldBeVisualized(OVRSkeleton.BoneId bone)
            {
                if (BoneTuples == null || BoneTuples.Length == 0)
                {
                    return false;
                }
                foreach (var currentBoneTuple in BoneTuples)
                {
                    if ((OVRSkeleton.BoneId)currentBoneTuple.FirstBone == bone)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Sets the bone visual array to all bones possible.
            /// </summary>
            public void FillArrayWithAllBones()
            {
                BoneTuples = new BoneTuple[(int)OVRSkeleton.BoneId.Body_End];
                for (var currentBone = OVRSkeleton.BoneId.Body_Hips;
                    currentBone < OVRSkeleton.BoneId.Body_End;
                    currentBone++)
                {
                    var boneTuple = CustomMappings.OVRSkeletonBoneIdToJointPair[currentBone];
                    var firstJoint = boneTuple.Item1;
                    var secondJoint = boneTuple.Item2;

                    BoneTuples[(int)currentBone] = new BoneTuple((CustomMappings.BodyTrackingBoneId)firstJoint,
                        (CustomMappings.BodyTrackingBoneId)secondJoint);
                }
            }

            /// <summary>
            /// Find bone tuple corresponding to start bone.
            /// </summary>
            /// <param name="startingBone">Start bone.</param>
            /// <returns>Bone tuple found, if any.</returns>
            public BoneTuple GetBoneTupleForStartingBone(OVRSkeleton.BoneId startingBone)
            {
                foreach (var boneTuple in BoneTuples)
                {
                    if (startingBone == (OVRSkeleton.BoneId)boneTuple.FirstBone)
                    {
                        return boneTuple;
                    }
                }

                return null;
            }

            /// <summary>
            /// Bone tuples to visualize.
            /// </summary>
            public BoneTuple[] BoneTuples;
        }

        /// <summary>
        /// Visualization guide type. Indicates if user
        /// wants to use the mask, the standard bone visual data,
        /// or their own custom data which allows custom bone
        /// pairings.
        /// </summary>
        public enum VisualizationGuideType
        {
            Mask = 0,
            BoneVisualData,
            CustomBoneVisualData
        }

        /// <summary>
        /// Visual types (lines, axes, etc).
        /// </summary>
        public enum VisualType
        {
            None = 0,
            Lines,
            Axes,
            LinesAxes
        }

        /// <summary>
        /// OVRSkeleton component to visualize bones for.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.OVRSkeletonComp)]
        protected OVRSkeleton _ovrSkeletonComp;

        /// <summary>
        /// Whether to visualize bind pose or not.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.VisualizeBindPose)]
        protected bool _visualizeBindPose = false;

        /// <summary>
        /// The type of guide used to visualize bones.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.VisualizationGuideType)]
        protected VisualizationGuideType _visualizationGuideType = VisualizationGuideType.Mask;

        /// <summary>
        /// Mask to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.MaskToVisualize)]
        protected AvatarMask _maskToVisualize = null;

        /// <summary>
        /// Bone collection to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.BoneVisualData)]
        protected BoneVisualData _boneVisualData;

        /// <summary>
        /// Custom bone visual data, which allows custom pairing of bones for line rendering.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.BoneVisualData)]
        protected CustomBoneVisualData _customBoneVisualData;

        /// <summary>
        /// Line renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.LineRendererPrefab)]
        protected GameObject _lineRendererPrefab;

        /// <summary>
        /// Axis renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.AxisRendererPrefab)]
        protected GameObject _axisRendererPrefab;

        /// <summary>
        /// Indicates what kind of visual is desired.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.VisualType)]
        protected VisualType _visualType = VisualType.None;

        private const string _LINE_VISUAL_NAME_SUFFIX_TOKEN = "-LineVisual.";
        private const string _AXIS_VISUAL_NAME_SUFFIX_TOKEN = "-AxisVisual.";

        private Dictionary<OVRSkeleton.BoneId, LineRenderer> _humanBoneToLineRenderer
            = new Dictionary<OVRSkeleton.BoneId, LineRenderer>();
        private Dictionary<OVRSkeleton.BoneId, Transform> _humanBoneToAxisObject
            = new Dictionary<OVRSkeleton.BoneId, Transform>();

        private void Awake()
        {
            Assert.IsNotNull(_ovrSkeletonComp);
            Assert.IsNotNull(_lineRendererPrefab);
            Assert.IsNotNull(_axisRendererPrefab);
        }

        /// <summary>
        /// Selects all bones for visualization.
        /// </summary>
        public void SelectAllBones()
        {
            if (_visualizationGuideType == VisualizationGuideType.Mask)
            {
                _maskToVisualize = CreateAllBonesMask();
            }
            else if (_visualizationGuideType == VisualizationGuideType.BoneVisualData)
            {
                _boneVisualData.FillArrayWithAllBones();
            }
            else
            {
                _customBoneVisualData.FillArrayWithAllBones();
            }
        }

        /// <summary>
        /// Resets all visual data.
        /// </summary>
        public void ClearData()
        {
            _maskToVisualize = null;
            _boneVisualData = new BoneVisualData();
            _customBoneVisualData = new CustomBoneVisualData();
            foreach (var value in _humanBoneToLineRenderer.Values)
            {
                Destroy(value.gameObject);
            }
            foreach (var value in _humanBoneToAxisObject.Values)
            {
                Destroy(value.gameObject);
            }
            _humanBoneToAxisObject.Clear();
            _humanBoneToLineRenderer.Clear();
        }

        private AvatarMask CreateAllBonesMask()
        {
            var allBonesMask = new AvatarMask();
            allBonesMask.InitializeDefaultValues(true);
            return allBonesMask;
        }

        private void LateUpdate()
        {
            VisualizeBones();
        }

        private void VisualizeBones()
        {
            for (var currentBone = OVRSkeleton.BoneId.Body_Hips; currentBone < OVRSkeleton.BoneId.Body_End;
                    currentBone++)
            {
                if (!ShouldVisualizeBone(currentBone))
                {
                    EnforceLineRendererEnableState(currentBone, false);
                    EnforceAxisRendererEnableState(currentBone, false);
                    continue;
                }

                switch (_visualType)
                {
                    case VisualType.Axes:
                        SetUpAxisRenderer(currentBone);
                        EnforceLineRendererEnableState(currentBone, false);
                        EnforceAxisRendererEnableState(currentBone, true);
                        break;
                    case VisualType.Lines:
                        SetUpLineRenderer(currentBone);
                        EnforceLineRendererEnableState(currentBone, true);
                        EnforceAxisRendererEnableState(currentBone, false);
                        break;
                    case VisualType.LinesAxes:
                        SetUpLineRenderer(currentBone);
                        SetUpAxisRenderer(currentBone);
                        EnforceLineRendererEnableState(currentBone, true);
                        EnforceAxisRendererEnableState(currentBone, true);
                        break;
                    case VisualType.None:
                        EnforceLineRendererEnableState(currentBone, false);
                        EnforceAxisRendererEnableState(currentBone, false);
                        break;
                }
            }
        }

        private bool ShouldVisualizeBone(OVRSkeleton.BoneId bone)
        {
            var bodyPart = CustomMappings.OVRSkeletonBoneIdToAvatarBodyPart[bone];
            if (_visualizationGuideType == VisualizationGuideType.Mask)
            {
                return _maskToVisualize != null &&
                    _maskToVisualize.GetHumanoidBodyPartActive(bodyPart);
            }
            else if (_visualizationGuideType == VisualizationGuideType.BoneVisualData)
            {
                return _boneVisualData != null &&
                    _boneVisualData.BoneShouldBeVisualized(bone);
            }
            else
            {
                return _customBoneVisualData != null &&
                    _customBoneVisualData.BoneShouldBeVisualized(bone);
            }
        }

        private void SetUpLineRenderer(OVRSkeleton.BoneId currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;

            (firstJoint, secondJoint) = GetVisualTransformPair(currentBone);

            if (firstJoint == null || secondJoint == null)
            {
                return;
            }

            if (!_humanBoneToLineRenderer.ContainsKey(currentBone))
            {
                var newObject = GameObject.Instantiate(_lineRendererPrefab);
                newObject.name += $"{_LINE_VISUAL_NAME_SUFFIX_TOKEN}{(CustomMappings.BodyTrackingBoneId)currentBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _humanBoneToLineRenderer[currentBone] = lineRenderer;
            }

            var lineRendererComp = _humanBoneToLineRenderer[currentBone];
            lineRendererComp.SetPosition(0, firstJoint.position);
            lineRendererComp.SetPosition(1, secondJoint.position);
        }

        private (Transform, Transform) GetVisualTransformPair(OVRSkeleton.BoneId currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;
            // If using mask or standard bone visual data, then default to hardcoded
            // mapped pairs
            if (_visualizationGuideType == VisualizationGuideType.Mask ||
                _visualizationGuideType == VisualizationGuideType.BoneVisualData)
            {
                var boneTuple = CustomMappings.OVRSkeletonBoneIdToJointPair[currentBone];
                firstJoint =
                    RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp, boneTuple.Item1,
                        _visualizeBindPose);

                if (boneTuple.Item2 == OVRSkeleton.BoneId.Body_End)
                {
                    secondJoint = firstJoint.GetChild(0);
                }
                else
                {
                    secondJoint = RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                        boneTuple.Item2, _visualizeBindPose);
                }
            }
            else if (_visualizationGuideType == VisualizationGuideType.CustomBoneVisualData)
            {
                var boneTuple = _customBoneVisualData.GetBoneTupleForStartingBone(currentBone);
                if (boneTuple != null)
                {
                    firstJoint = (OVRSkeleton.BoneId)boneTuple.FirstBone != OVRSkeleton.BoneId.Body_End ?
                        RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                            (OVRSkeleton.BoneId)boneTuple.FirstBone, _visualizeBindPose) :
                        null;
                    secondJoint = (OVRSkeleton.BoneId)boneTuple.SecondBone != OVRSkeleton.BoneId.Body_End ?
                        RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                            (OVRSkeleton.BoneId)boneTuple.SecondBone, _visualizeBindPose) :
                        null;
                }
            }
            return (firstJoint, secondJoint);
        }

        private void SetUpAxisRenderer(OVRSkeleton.BoneId currentBone)
        {
            var boneTransform = RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                currentBone, _visualizeBindPose);
            if (boneTransform == null)
            {
                return;
            }

            if (!_humanBoneToAxisObject.ContainsKey(currentBone))
            {
                var newObject = GameObject.Instantiate(_axisRendererPrefab);
                newObject.name += $"{_AXIS_VISUAL_NAME_SUFFIX_TOKEN}{(CustomMappings.BodyTrackingBoneId)currentBone}";
                newObject.transform.SetParent(transform);
                _humanBoneToAxisObject[currentBone] = newObject.transform;
            }

            var axisComp = _humanBoneToAxisObject[currentBone];
            axisComp.position = boneTransform.position;
            axisComp.rotation = boneTransform.rotation;
        }

        private void EnforceLineRendererEnableState(OVRSkeleton.BoneId bone, bool enableValue)
        {
            if (!_humanBoneToLineRenderer.ContainsKey(bone))
            {
                return;
            }
            _humanBoneToLineRenderer[bone].enabled = enableValue;
        }

        private void EnforceAxisRendererEnableState(OVRSkeleton.BoneId bone, bool enableValue)
        {
            if (!_humanBoneToAxisObject.ContainsKey(bone))
            {
                return;
            }
            _humanBoneToAxisObject[bone].gameObject.SetActive(enableValue);
        }
    }
}
