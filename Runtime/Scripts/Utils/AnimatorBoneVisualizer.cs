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
            /// Indicates if bone should be visualized or not.
            /// </summary>
            /// <param name="bone">Bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool BoneShouldBeVisualized(HumanBodyBones bone)
            {
                if (BoneTuples == null || BoneTuples.Length == 0)
                {
                    return false;
                }
                foreach (var currentBoneTuple in BoneTuples)
                {
                    if (currentBoneTuple.FirstBone == bone)
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
            /// Find bone tuple corresponding to start bone.
            /// </summary>
            /// <param name="startingBone">Start bone.</param>
            /// <returns>Bone tuple found, if any.</returns>
            public BoneTuple GetBoneTupleForStartingBone(HumanBodyBones startingBone)
            {
                foreach (var boneTuple in BoneTuples)
                {
                    if (startingBone == boneTuple.FirstBone)
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
        private const string _AXIS_VISUAL_NAME_SUFFIX_TOKEN = "-AxisVisual.";

        private Dictionary<HumanBodyBones, LineRenderer> _humanBoneToLineRenderer
            = new Dictionary<HumanBodyBones, LineRenderer>();
        private Dictionary<HumanBodyBones, Transform> _humanBoneToAxisObject
            = new Dictionary<HumanBodyBones, Transform>();

        private void Awake()
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
                return _customBoneVisualData != null &&
                    _customBoneVisualData.BoneShouldBeVisualized(bone);
            }
        }

        private void SetUpLineRenderer(HumanBodyBones currentBone)
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
                newObject.name += $"{_LINE_VISUAL_NAME_SUFFIX_TOKEN}{currentBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _humanBoneToLineRenderer[currentBone] = lineRenderer;
            }

            var lineRendererComp = _humanBoneToLineRenderer[currentBone];
            lineRendererComp.SetPosition(0, firstJoint.position);
            lineRendererComp.SetPosition(1, secondJoint.position);
        }

        private (Transform, Transform) GetVisualTransformPair(HumanBodyBones currentBone)
        {
            Transform firstJoint = null;
            Transform secondJoint = null;
            // If using mask or standard bone visual data, then default to hardcoded
            // mapped pairs
            if (_visualizationGuideType == VisualizationGuideType.Mask ||
                _visualizationGuideType == VisualizationGuideType.BoneVisualData)
            {
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
            }
            else if (_visualizationGuideType == VisualizationGuideType.CustomBoneVisualData)
            {
                var boneTuple = _customBoneVisualData.GetBoneTupleForStartingBone(currentBone);
                if (boneTuple != null)
                {
                    firstJoint = boneTuple.FirstBone != HumanBodyBones.LastBone ?
                        _animatorComp.GetBoneTransform(boneTuple.FirstBone) :
                        null;
                    secondJoint = boneTuple.SecondBone != HumanBodyBones.LastBone ?
                        _animatorComp.GetBoneTransform(boneTuple.SecondBone) :
                        null;
                }
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

        private void EnforceLineRendererEnableState(HumanBodyBones bodyBone, bool enableValue)
        {
            if (!_humanBoneToLineRenderer.ContainsKey(bodyBone))
            {
                return;
            }
            _humanBoneToLineRenderer[bodyBone].enabled = enableValue;
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
