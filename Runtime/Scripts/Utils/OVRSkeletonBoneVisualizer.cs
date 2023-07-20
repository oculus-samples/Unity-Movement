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
        public class BoneTuple : ISerializationCallbackReceiver
        {
            public BoneTuple()
            {
                FirstBone = CustomMappings.BodyTrackingBoneId.Body_Hips;
                SecondBone = CustomMappings.BodyTrackingBoneId.Body_Hips;

                BonePair =
                    new Tuple<CustomMappings.BodyTrackingBoneId,
                     CustomMappings.BodyTrackingBoneId>(FirstBone, SecondBone);
            }

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

                BonePair =
                    new Tuple<CustomMappings.BodyTrackingBoneId,
                     CustomMappings.BodyTrackingBoneId>(FirstBone, SecondBone);
            }

            public void OnBeforeSerialize()
            {
                // Nothing to see here.
            }

            public void OnAfterDeserialize()
            {
                // always keep this up-to-date
                BonePair =
                    new Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId>(FirstBone, SecondBone);
            }

            /// <summary>
            /// First bone in tuple.
            /// </summary>
            public CustomMappings.BodyTrackingBoneId FirstBone;
            /// <summary>
            /// Second bone in tuple.
            /// </summary>
            public CustomMappings.BodyTrackingBoneId SecondBone;

            /// <summary>
            /// Use this as a key for dictionary look-ups. That way multiple pairs
            /// with the same first and second bones will map to the same key.
            /// </summary>
            [HideInInspector]
            public Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId> BonePair;
        }

        /// <summary>
        /// Allows rendering custom bone tuples.
        /// </summary>
        [Serializable]
        public class CustomBoneVisualData
        {
            /// <summary>
            /// Indicates if tuple exists
            /// </summary>
            /// <param name="firstBone">First bone in question.</param>
            /// <param name="secondBone">Second bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool DoesTupleExist(
                OVRSkeleton.BoneId firstBone,
                OVRSkeleton.BoneId secondBone)
            {
                if (BoneTuples == null || BoneTuples.Length == 0)
                {
                    return false;
                }
                foreach (var currentBoneTuple in BoneTuples)
                {
                    if ((OVRSkeleton.BoneId)currentBoneTuple.FirstBone == firstBone &&
                        (OVRSkeleton.BoneId)currentBoneTuple.SecondBone == secondBone)
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
        private const string _CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN = "-CustomLineVisual.";
        private const string _AXIS_VISUAL_NAME_SUFFIX_TOKEN = "-AxisVisual.";

        private Dictionary<OVRSkeleton.BoneId, LineRenderer> _humanBoneToLineRenderer
            = new Dictionary<OVRSkeleton.BoneId, LineRenderer>();
        private Dictionary<Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId>, LineRenderer> _boneTupeToLineRenderer
            = new Dictionary<Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId>, LineRenderer>();
        private Dictionary<OVRSkeleton.BoneId, Transform> _humanBoneToAxisObject
            = new Dictionary<OVRSkeleton.BoneId, Transform>();

        private List<Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId>> _itemsToDelete =
            new List<Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId>>();

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
            _customBoneVisualData.BoneTuples = new BoneTuple[0];
            foreach (var value in _humanBoneToLineRenderer.Values)
            {
                Destroy(value.gameObject);
            }
            ClearBoneTupleVisualObjects();
            foreach (var value in _humanBoneToAxisObject.Values)
            {
                Destroy(value.gameObject);
            }
            _humanBoneToAxisObject.Clear();
            _humanBoneToLineRenderer.Clear();
        }

        private void ClearBoneTupleVisualObjects()
        {
            foreach (var value in _boneTupeToLineRenderer)
            {
                Destroy(value.Value.gameObject);
            }

            _boneTupeToLineRenderer.Clear();
        }

        private AvatarMask CreateAllBonesMask()
        {
            var allBonesMask = new AvatarMask();
            allBonesMask.InitializeDefaultValues(true);
            return allBonesMask;
        }

        private void LateUpdate()
        {
            VisualizeNonCustomData();
            VisualizeCustomData();
        }

        private void VisualizeNonCustomData()
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
                        SetUpFixedTransformPairLineRenderer(currentBone);
                        EnforceLineRendererEnableState(currentBone, true);
                        EnforceAxisRendererEnableState(currentBone, false);
                        break;
                    case VisualType.LinesAxes:
                        SetUpFixedTransformPairLineRenderer(currentBone);
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

        private void VisualizeCustomData()
        {
            if (_visualizationGuideType != VisualizationGuideType.CustomBoneVisualData)
            {
                ClearBoneTupleVisualObjects();
                return;
            }

            foreach (var tupleItem in _customBoneVisualData.BoneTuples)
            {
                switch (_visualType)
                {
                    case VisualType.Axes:
                        SetUpAxisRenderer((OVRSkeleton.BoneId)tupleItem.FirstBone);
                        EnforceCustomLineRendererEnableState(tupleItem.BonePair, false);
                        EnforceAxisRendererEnableState((OVRSkeleton.BoneId)tupleItem.FirstBone, true);
                        break;
                    case VisualType.Lines:
                        SetupBoneTupleLineRenderer(tupleItem);
                        EnforceCustomLineRendererEnableState(tupleItem.BonePair, true);
                        EnforceAxisRendererEnableState((OVRSkeleton.BoneId)tupleItem.FirstBone, false);
                        break;
                    case VisualType.LinesAxes:
                        SetupBoneTupleLineRenderer(tupleItem);
                        SetUpAxisRenderer((OVRSkeleton.BoneId)tupleItem.FirstBone);
                        EnforceCustomLineRendererEnableState(tupleItem.BonePair, true);
                        EnforceAxisRendererEnableState((OVRSkeleton.BoneId)tupleItem.FirstBone, true);
                        break;
                    case VisualType.None:
                        EnforceCustomLineRendererEnableState(tupleItem.BonePair, false);
                        EnforceAxisRendererEnableState((OVRSkeleton.BoneId)tupleItem.FirstBone, false);
                        break;
                }
            }

            _itemsToDelete.Clear();
            // remove previously-rendered tuples, if any
            foreach (var key in _boneTupeToLineRenderer.Keys)
            {
                if (!_customBoneVisualData.DoesTupleExist(
                        (OVRSkeleton.BoneId)key.Item1,
                        (OVRSkeleton.BoneId)key.Item2))
                {
                    _itemsToDelete.Add(key);
                }
            }
            foreach (var item in _itemsToDelete)
            {
                var lineRend = _boneTupeToLineRenderer[item];
                Destroy(lineRend.gameObject);
                _boneTupeToLineRenderer.Remove(item);
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
                return false;
            }
        }

        private void SetUpFixedTransformPairLineRenderer(OVRSkeleton.BoneId currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;

            (firstJoint, secondJoint) = GetFixedTransformPairForBone(currentBone);

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

        private (Transform, Transform) GetFixedTransformPairForBone(OVRSkeleton.BoneId currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;

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

        private void SetupBoneTupleLineRenderer(BoneTuple tupleItem)
        {
            if (!_boneTupeToLineRenderer.ContainsKey(tupleItem.BonePair))
            {
                var newObject = GameObject.Instantiate(_lineRendererPrefab);
                newObject.name +=
                    $"{_CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN}{tupleItem.FirstBone}-{tupleItem.SecondBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _boneTupeToLineRenderer[tupleItem.BonePair] = lineRenderer;
            }
         
            var firstJoint = RiggingUtilities.FindBoneTransformFromSkeleton(
                _ovrSkeletonComp,
                (OVRSkeleton.BoneId)tupleItem.FirstBone,
                _visualizeBindPose);

            Transform secondJoint = null;
            if (tupleItem.SecondBone >= CustomMappings.BodyTrackingBoneId.Body_End)
            {
                secondJoint = firstJoint.GetChild(0);
            }
            else
            {
                secondJoint = RiggingUtilities.FindBoneTransformFromSkeleton(
                    _ovrSkeletonComp,
                    (OVRSkeleton.BoneId)tupleItem.SecondBone,
                    _visualizeBindPose);
            }

            if (firstJoint == null || secondJoint == null)
            {
                Debug.LogWarning($"Cannot find transform for tuple " +
                    $"{tupleItem.FirstBone}-{tupleItem.SecondBone}");
                return;
            }
            var lineRendererComp = _boneTupeToLineRenderer[tupleItem.BonePair];
            lineRendererComp.SetPosition(0, firstJoint.position);
            lineRendererComp.SetPosition(1, secondJoint.position);
        }

        private void EnforceLineRendererEnableState(OVRSkeleton.BoneId bone, bool enableValue)
        {
            if (!_humanBoneToLineRenderer.ContainsKey(bone))
            {
                return;
            }
            _humanBoneToLineRenderer[bone].enabled = enableValue;
        }

        private void EnforceCustomLineRendererEnableState(
            Tuple<CustomMappings.BodyTrackingBoneId, CustomMappings.BodyTrackingBoneId> tuple, bool enableValue)
        {
            if (!_boneTupeToLineRenderer.ContainsKey(tuple))
            {
                return;
            }
            _boneTupeToLineRenderer[tuple].enabled = enableValue;
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
