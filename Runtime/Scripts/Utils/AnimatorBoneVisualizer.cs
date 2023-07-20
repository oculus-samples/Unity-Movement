// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows visualizing bones found in an Animator component.
    /// </summary>
    [DefaultExecutionOrder(230)]
    public class AnimatorBoneVisualizer : MonoBehaviour
    {
        [Serializable]
        protected class BoneVisualData
        {
            /// <summary>
            /// Indicates if bone should be visualized or not.
            /// </summary>
            /// <param name="bone">Bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool BoneShouldBeVisualized(HumanBodyBones bone)
            {
                if (BonesToVisualize == null || BonesToVisualize.Length == 0)
                {
                    return false;
                }
                foreach (var currentBone in BonesToVisualize)
                {
                    if (currentBone == bone)
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
                BonesToVisualize = new HumanBodyBones[(int)HumanBodyBones.LastBone];
                for (var currentBone = HumanBodyBones.Hips; currentBone < HumanBodyBones.LastBone;
                    currentBone++)
                {
                    BonesToVisualize[(int)currentBone] = currentBone;
                }
            }

            /// <summary>
            /// Bones to visualize.
            /// </summary>
            public HumanBodyBones[] BonesToVisualize;
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
                HumanBodyBones firstBone,
                HumanBodyBones secondBone)
            {
                FirstBone = firstBone;
                SecondBone = secondBone;
            }

            /// <summary>
            /// First bone in tuple.
            /// </summary>
            public HumanBodyBones FirstBone;
            /// <summary>
            /// Second bone in tuple.
            /// </summary>
            public HumanBodyBones SecondBone;
        }

        /// <summary>
        /// Allows rendering custom bone tuples.
        /// </summary>
        [Serializable]
        public class CustomBoneVisualData
        {
            /// <summary>
            /// Indicates if tuple with first bone exists or not.
            /// </summary>
            /// <param name="firstBone">First bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool TupleWithFirstBoneExists(
                HumanBodyBones firstBone)
            {
                if (BoneTuples == null || BoneTuples.Length == 0)
                {
                    return false;
                }
                foreach (var currentBoneTuple in BoneTuples)
                {
                    if (currentBoneTuple.FirstBone == firstBone)
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
                BoneTuples = new BoneTuple[(int)HumanBodyBones.LastBone];
                for (var currentBone = HumanBodyBones.Hips; currentBone < HumanBodyBones.LastBone;
                    currentBone++)
                {
                    var boneTuple = CustomMappings.BoneToJointPair[currentBone];
                    var firstJoint = boneTuple.Item1;
                    var secondJoint = boneTuple.Item2;

                    BoneTuples[(int)currentBone] = new BoneTuple(firstJoint, secondJoint);
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
        /// Animator component to visualize bones for.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.AnimatorComp)]
        protected Animator _animatorComp;

        /// <summary>
        /// The type of guide used to visualize bones.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.VisualizationGuideType)]
        protected VisualizationGuideType _visualizationGuideType = VisualizationGuideType.Mask;

        /// <summary>
        /// Mask to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.MaskToVisualize)]
        protected AvatarMask _maskToVisualize = null;

        /// <summary>
        /// Bone collection to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.BoneVisualData)]
        protected BoneVisualData _boneVisualData;

        /// <summary>
        /// Custom bone visual data, which allows custom pairing of bones for line rendering.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.BoneVisualData)]
        protected CustomBoneVisualData _customBoneVisualData;

        /// <summary>
        /// Line renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.LineRendererPrefab)]
        protected GameObject _lineRendererPrefab;

        /// <summary>
        /// Axis renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.AxisRendererPrefab)]
        protected GameObject _axisRendererPrefab;

        /// <summary>
        /// Indicates what kind of visual is desired.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.VisualType)]
        protected VisualType _visualType = VisualType.None;

        private const string _LINE_VISUAL_NAME_SUFFIX_TOKEN = "-LineVisual.";
        private const string _CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN = "-CustomLineVisual.";
        private const string _AXIS_VISUAL_NAME_SUFFIX_TOKEN = "-AxisVisual.";

        private Dictionary<HumanBodyBones, LineRenderer> _humanBoneToLineRenderer
            = new Dictionary<HumanBodyBones, LineRenderer>();
        private Dictionary<BoneTuple, LineRenderer> _boneTupeToLineRenderer
            = new Dictionary<BoneTuple, LineRenderer>();
        private Dictionary<HumanBodyBones, Transform> _humanBoneToAxisObject
            = new Dictionary<HumanBodyBones, Transform>();

        /// <summary>
        /// MonoBehaviour.Awake
        /// </summary>
        protected virtual void Awake()
        {
            Assert.IsNotNull(_animatorComp);
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
            foreach (var value in _boneTupeToLineRenderer)
            {
                Destroy(value.Value.gameObject);
            }
            foreach (var value in _humanBoneToAxisObject.Values)
            {
                Destroy(value.gameObject);
            }
            _humanBoneToAxisObject.Clear();
            _boneTupeToLineRenderer.Clear();
            _humanBoneToLineRenderer.Clear();
        }

        private AvatarMask CreateAllBonesMask()
        {
            var allBonesMask = new AvatarMask();
            allBonesMask.InitializeDefaultValues(true);
            return allBonesMask;
        }

        /// <summary>
        /// MonoBehaviour.LateUpdate
        /// </summary>
        protected virtual void LateUpdate()
        {
            VisualizeNonCustomData();
            VisualizeCustomData();
        }

        private void VisualizeNonCustomData()
        {
            for (var currentBone = HumanBodyBones.Hips; currentBone < HumanBodyBones.LastBone; currentBone++)
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
                foreach (var key in _boneTupeToLineRenderer.Keys)
                {
                    EnforceCustomLineRendererEnableState(key, false);
                }
                return;
            }

            foreach (var tupleItem in _customBoneVisualData.BoneTuples)
            {
                switch (_visualType)
                {
                    case VisualType.Axes:
                        SetUpAxisRenderer(tupleItem.FirstBone);
                        EnforceCustomLineRendererEnableState(tupleItem, false);
                        EnforceAxisRendererEnableState(tupleItem.FirstBone, true);
                        break;
                    case VisualType.Lines:
                        SetupBoneTupleLineRenderer(tupleItem);
                        EnforceCustomLineRendererEnableState(tupleItem, true);
                        EnforceAxisRendererEnableState(tupleItem.FirstBone, false);
                        break;
                    case VisualType.LinesAxes:
                        SetupBoneTupleLineRenderer(tupleItem);
                        SetUpAxisRenderer(tupleItem.FirstBone);
                        EnforceCustomLineRendererEnableState(tupleItem, true);
                        EnforceAxisRendererEnableState(tupleItem.FirstBone, true);
                        break;
                    case VisualType.None:
                        EnforceCustomLineRendererEnableState(tupleItem, false);
                        EnforceAxisRendererEnableState(tupleItem.FirstBone, false);
                        break;
                }
            }
            // disable previously-rendered tuples, if any
            foreach (var key in _boneTupeToLineRenderer.Keys)
            {
                if (!_customBoneVisualData.TupleWithFirstBoneExists(key.FirstBone))
                {
                    EnforceCustomLineRendererEnableState(key, false);
                }
            }
        }

        private bool ShouldVisualizeBone(HumanBodyBones bone)
        {
            var bodyPart = CustomMappings.HumanBoneToAvatarBodyPart[bone];
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

        private void SetUpFixedTransformPairLineRenderer(HumanBodyBones currentBone)
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
                newObject.name += $"{_LINE_VISUAL_NAME_SUFFIX_TOKEN}{currentBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _humanBoneToLineRenderer[currentBone] = lineRenderer;
            }

            var lineRendererComp = _humanBoneToLineRenderer[currentBone];
            lineRendererComp.SetPosition(0, firstJoint.position);
            lineRendererComp.SetPosition(1, secondJoint.position);
        }

        private (Transform, Transform) GetFixedTransformPairForBone(HumanBodyBones currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;

            var boneTuple = CustomMappings.BoneToJointPair[currentBone];
            firstJoint = _animatorComp.GetBoneTransform(boneTuple.Item1);

            if (boneTuple.Item2 == HumanBodyBones.LastBone)
            {
                secondJoint = firstJoint.GetChild(0);
            }
            else
            {
                secondJoint = _animatorComp.GetBoneTransform(boneTuple.Item2);
            }

            return (firstJoint, secondJoint);
        }

        private void SetUpAxisRenderer(HumanBodyBones currentBone)
        {
            var boneTransform = _animatorComp.GetBoneTransform(currentBone);
            if (boneTransform == null)
            {
                return;
            }

            if (!_humanBoneToAxisObject.ContainsKey(currentBone))
            {
                var newObject = GameObject.Instantiate(_axisRendererPrefab);
                newObject.name += $"{_AXIS_VISUAL_NAME_SUFFIX_TOKEN}{currentBone}";
                newObject.transform.SetParent(transform);
                _humanBoneToAxisObject[currentBone] = newObject.transform;
            }

            var axisComp = _humanBoneToAxisObject[currentBone];
            axisComp.position = boneTransform.position;
            axisComp.rotation = boneTransform.rotation;
        }

        private void SetupBoneTupleLineRenderer(BoneTuple tupleItem)
        {
            if (!_boneTupeToLineRenderer.ContainsKey(tupleItem))
            {
                var newObject = GameObject.Instantiate(_lineRendererPrefab);
                newObject.name +=
                    $"{_CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN}{tupleItem.FirstBone}-{tupleItem.SecondBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _boneTupeToLineRenderer[tupleItem] = lineRenderer;
            }

            var firstJoint = _animatorComp.GetBoneTransform(tupleItem.FirstBone);
            var secondJoint = _animatorComp.GetBoneTransform(tupleItem.SecondBone);

            if (firstJoint == null || secondJoint == null)
            {
                Debug.LogWarning($"Cannot find transform for tuple " +
                    $"{tupleItem.FirstBone}-{tupleItem.SecondBone}");
                return;
            }
            var lineRendererComp = _boneTupeToLineRenderer[tupleItem];
            lineRendererComp.SetPosition(0, firstJoint.position);
            lineRendererComp.SetPosition(1, secondJoint.position);
        }

        private void EnforceLineRendererEnableState(HumanBodyBones bodyBone, bool enableValue)
        {
            if (!_humanBoneToLineRenderer.ContainsKey(bodyBone))
            {
                return;
            }
            _humanBoneToLineRenderer[bodyBone].enabled = enableValue;
        }

        private void EnforceCustomLineRendererEnableState(BoneTuple boneTuple, bool enableValue)
        {
            if (!_boneTupeToLineRenderer.ContainsKey(boneTuple))
            {
                return;
            }
            _boneTupeToLineRenderer[boneTuple].enabled = enableValue;
        }

        private void EnforceAxisRendererEnableState(HumanBodyBones bodyBone, bool enableValue)
        {
            if (!_humanBoneToAxisObject.ContainsKey(bodyBone))
            {
                return;
            }
            _humanBoneToAxisObject[bodyBone].gameObject.SetActive(enableValue);
        }
    }
}
