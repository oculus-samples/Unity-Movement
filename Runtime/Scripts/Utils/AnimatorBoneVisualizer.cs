// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows visualizing bones found in an Animator component.
    /// </summary>
    public class AnimatorBoneVisualizer : MonoBehaviour
    {
        [Serializable]
        protected class BoneVisualData
        {
            /// <summary>
            /// Validates fields on class using Assert calls.
            /// </summary>
            public void Validate()
            {
                Assert.IsTrue(BonesToVisualize != null &&
                    BonesToVisualize.Length > 0);
            }

            /// <summary>
            /// Indicates if bone should be visualized or not.
            /// </summary>
            /// <param name="bone">Bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool BoneShouldBeVisualized(HumanBodyBones bone)
            {
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

            public HumanBodyBones[] BonesToVisualize;
        }

        public enum VisualizationGuideType
        {
            Mask = 0,
            BoneVisualData
        }
        public enum VisualType
        {
            None = 0,
            Lines,
            Axes,
            Both
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
        [SerializeField, ConditionalHide("_visualizationGuideType", VisualizationGuideType.Mask)]
        [Tooltip(AnimatorBoneVisualizerTooltips.AnimatorComp)]
        protected AvatarMask _maskToVisualize = null;

        /// <summary>
        /// Bone collection to use for visualization.
        /// </summary>
        [SerializeField, ConditionalHide("_visualizationGuideType", VisualizationGuideType.BoneVisualData)]
        [Tooltip(AnimatorBoneVisualizerTooltips.AnimatorComp)]
        protected BoneVisualData _boneVisualData;

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
            if (_visualizationGuideType == VisualizationGuideType.Mask)
            {
                Assert.IsNotNull(_maskToVisualize);
            }
            else
            {
                _boneVisualData.Validate();
            }
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
            else
            {
                _boneVisualData.FillArrayWithAllBones();
            }
        }

        private AvatarMask CreateAllBonesMask()
        {
            var allBonesMask = new AvatarMask();
            allBonesMask.InitializeDefaultValues(true);
            return allBonesMask;
        }

        private void Update()
        {
            VisualizeBones();
        }

        private void VisualizeBones()
        {
            for (var currentBone = HumanBodyBones.Hips; currentBone < HumanBodyBones.LastBone; currentBone++)
            {
                if (!ShouldVisualizeBone(currentBone))
                {
                    continue;
                }

                switch (_visualType)
                {
                    case VisualType.Axes:
                        SetUpAxisRenderer(currentBone);
                        EnforceLineRendererEnableState(false);
                        EnforceAxisRendererEnableState(true);
                        break;
                    case VisualType.Lines:
                        SetUpLineRenderer(currentBone);
                        EnforceLineRendererEnableState(true);
                        EnforceAxisRendererEnableState(false);
                        break;
                    case VisualType.Both:
                        SetUpLineRenderer(currentBone);
                        SetUpAxisRenderer(currentBone);
                        EnforceLineRendererEnableState(true);
                        EnforceAxisRendererEnableState(true);
                        break;
                    case VisualType.None:
                        EnforceLineRendererEnableState(false);
                        EnforceAxisRendererEnableState(false);
                        break;

                }
            }
        }

        private bool ShouldVisualizeBone(HumanBodyBones bone)
        {
            var bodyPart = CustomMappings.HumanBoneToAvatarBodyPart[bone];
            if (_visualizationGuideType == VisualizationGuideType.Mask)
            {
                return _maskToVisualize.GetHumanoidBodyPartActive(bodyPart);
            }
            else
            {
                return _boneVisualData.BoneShouldBeVisualized(bone);
            }
        }

        private void SetUpLineRenderer(HumanBodyBones currentBone)
        {
            // each joint has a bone tupe that we can use to visualize the bones.
            var boneTuple = CustomMappings.BoneToJointPair[currentBone];
            var firstJoint = _animatorComp.GetBoneTransform(boneTuple.Item1);

            if (firstJoint == null)
            {
                return;
            }

            Transform secondJoint = null;
            if (boneTuple.Item2 == HumanBodyBones.LastBone)
            {
                secondJoint = firstJoint.GetChild(0);
            }
            else
            {
                secondJoint = _animatorComp.GetBoneTransform(boneTuple.Item2);
            }

            if (secondJoint == null)
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

        private void EnforceLineRendererEnableState(bool enableValue)
        {
            foreach (var value in _humanBoneToLineRenderer.Values)
            {
                value.enabled = enableValue;
            }
        }

        private void EnforceAxisRendererEnableState(bool enableValue)
        {
            foreach (var value in _humanBoneToAxisObject.Values)
            {
                value.gameObject.SetActive(enableValue);
            }
        }
    }
}
