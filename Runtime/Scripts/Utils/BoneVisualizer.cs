// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows list creation/destruction without referencing the type of list
    /// </summary>
    public interface IListGenerating
    {
        /// <summary>
        /// Generates a list
        /// </summary>
        public void GenerateList();
        /// <summary>
        /// Clears the generated list
        /// </summary>
        public void ClearList();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Anchors custom UI for BoneVisualizer. Should always be in
    /// <see cref="BoneVisualizer{BoneType}.CustomBoneVisualData"/>
    /// </summary>
    [Serializable] public class EditorSkeletonControls { }
#endif

    /// <summary>
    /// This class is a base class for <see cref="BoneVisualizer{BoneType}"/>,
    /// keeping type information that does not rely on a BoneType generic, and
    /// allowing all <see cref="BoneVisualizer{BoneType}"/>s to be accessed
    /// with a common type.
    /// </summary>
    public abstract class BoneVisualizer : MonoBehaviour, IListGenerating
        , IOVRSkeletonProcessor
    {
        /// <summary>
        /// Visualization guide type. Indicates if user
        /// wants to use the mask, the standard bone visual data,
        /// or their own custom data which allows custom bone
        /// pairings.
        /// </summary>
        public enum VisualizationGuideType
        {
            AvatarMask = 0,
            BoneVisualData,
        }

        /// <summary>
        /// When in the game loop to update the skeleton from it's source.
        /// Useful for debugging the state of the skeleton source at different
        /// stages of the game loop.
        /// </summary>
        public enum WhenToRender
        {
            Automatic,
            Update,
            LateUpdate,
            VisualizeCalledExplicitly
        }

        /// <summary>
        /// Visual types (lines, axes, etc).
        /// </summary>
        [Flags]
        public enum VisualType
        {
            None = 0,
            Lines = 1,
            Axes = 2
        }

        /// <summary>
        /// Callback for newly generated <see cref="VisualType.Axes"/>
        /// </summary>
        /// <param name="gameObject"></param>
        public delegate void InstantiateAxisEvent(
            int bone, GameObject gameObject);

        /// <summary>
        /// Callback for newly generated <see cref="VisualType.Lines"/>
        /// </summary>
        /// <param name="gameObject"></param>
        public delegate void InstantiateLineEvent(
            int firstBone, int secondBone, GameObject gameObject);

        protected const string _LINE_VISUAL_NAME_SUFFIX_TOKEN = "-LineVisual.";
        protected const string _CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN = "-CustomLineVisual.";
        protected const string _AXIS_VISUAL_NAME_SUFFIX_TOKEN = "-AxisVisual.";

        /// <summary>
        /// When to render this skeleton during the Unity gameloop.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.WhenToRender)]
        protected WhenToRender _whenToRender = WhenToRender.LateUpdate;

        protected Dictionary<object, LineRenderer> _boneTupleToLineRenderer
            = new Dictionary<object, LineRenderer>();
        protected Dictionary<int, Transform> _boneIdToAxisObject
            = new Dictionary<int, Transform>();

        /// <summary>
        /// Callback for newly generated <see cref="VisualType.Axes"/>
        /// </summary>
        public InstantiateAxisEvent OnNewAxis { get; set; } = null;

        /// <summary>
        /// Callback for newly generated <see cref="VisualType.Lines"/>
        /// </summary>
        public InstantiateLineEvent OnNewLine { get; set; } = null;

        /// <summary>
        /// The dictionary of bone visuals.
        /// Note these are visuals of bones, not bones themselves.
        /// </summary>
        public Dictionary<object, LineRenderer> BoneVisualRenderers
            => _boneTupleToLineRenderer;

        /// <summary>
        /// The dictionary of bone joint visuals.
        /// Note these are visuals of bones, not bones themselves.
        /// </summary>
        public Dictionary<int, Transform> JointVisualTransforms
            => _boneIdToAxisObject;

        /// <inheritdoc/>
        public bool EnableSkeletonProcessing { get => enabled; set => enabled = value; }

        /// <inheritdoc/>
        public virtual string SkeletonProcessorLabel => name;

        /// <inheritdoc/>
        public abstract void GenerateList();

        /// <inheritdoc/>
        public abstract void ClearList();

        /// <summary>
        /// Applies bone positions & rotations to axes & lines visuals
        /// </summary>
        public abstract void Visualize();
        /// <summary>
        /// Can be used by UI to change the source of the skeleton visual data
        /// </summary>
        /// <param name="body"></param>
        public abstract void SetBody(GameObject body);

        /// <inheritdoc/>
        public virtual void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (_whenToRender == WhenToRender.Automatic)
            {
                _whenToRender = WhenToRender.VisualizeCalledExplicitly;
            }
            Visualize();
        }
    }

    /// <summary>
    /// Allows visualizing bones found in an OVRSkeleton or Animator component.
    /// <see cref="BoneType"/> should be an enum that maps to bone IDs.
    /// </summary>
    public abstract class BoneVisualizer<BoneType> : BoneVisualizer
    {
        /// <summary>
        /// Bone tuple class: pair of bone joint IDs that define a bone
        /// </summary>
        [Serializable]
        public class BoneTuple
        {
            /// <summary>
            /// First bone in tuple.
            /// </summary>
            public BoneType FirstBone;
            /// <summary>
            /// Second bone in tuple.
            /// </summary>
            public BoneType SecondBone;
            /// <summary>
            /// Whether or not to hide the bone (show by default)
            /// </summary>
            public bool Hide;

            /// <summary>
            /// <see cref="FirstBone"/> as an Integer, which simplifies code
            /// when the bone index is ambiguous
            /// </summary>
            public int FirstBoneId
            {
                get => (int)(object)FirstBone;
                set => FirstBone = (BoneType)(object)value;
            }

            /// <summary>
            /// <see cref="SecondBone"/> as an Integer, which simplifies code
            /// when the bone index is ambiguous
            /// </summary>
            public int SecondBoneId
            {
                get => (int)(object)SecondBone;
                set => SecondBone = (BoneType)(object)value;
            }

            /// <summary>
            /// Standard BoneTuple constructor.
            /// </summary>
            public BoneTuple(int firstBone, int secondBone)
            {
                FirstBoneId = firstBone;
                SecondBoneId = secondBone;
            }

            /// <summary>
            /// Copy Constructor
            /// </summary>
            /// <param name="original"></param>
            public BoneTuple(BoneTuple original)
            {
                FirstBone = original.FirstBone;
                SecondBone = original.SecondBone;
            }

            /// <summary>
            /// General equality check, without the overhead of implementing operator==
            /// </summary>
            /// <param name="firstBone"></param>
            /// <param name="secondBone"></param>
            /// <returns></returns>
            public bool Is(int firstBone, int secondBone)
            {
                return FirstBoneId == firstBone && SecondBoneId == secondBone;
            }
        }

        /// <summary>
        /// Manages data defining which bones should be visualized
        /// </summary>
        [Serializable]
        public class CustomBoneVisualData
        {
#if UNITY_EDITOR
            /// <summary>
            /// used to anchor custom UI in the inspector
            /// </summary>
            public EditorSkeletonControls _editorSkeletonControls;
#endif
            /// <summary>
            /// Bone tuples to visualize.
            /// </summary>
            public List<BoneTuple> BoneTuples;
            /// <summary>
            /// Joints used
            /// </summary>
            private BitArray JointsUsed;
            /// <summary>
            /// Keep track of expected bone count, to look for changes
            /// </summary>
            protected int _lastKnownTupleCount;

            /// <summary>
            /// Determine if data has changed and needs to be re-rendered
            /// </summary>
            public bool IsChanged => _lastKnownTupleCount != BoneTuples.Count;

            /// <summary>
            /// Default constructor
            /// </summary>
            public CustomBoneVisualData() { }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="original"></param>
            public CustomBoneVisualData(CustomBoneVisualData original) {
                BoneTuples = new List<BoneTuple>(original.BoneTuples.Count);
                for(int i = 0; i < original.BoneTuples.Count; ++i)
                {
                    BoneTuples.Add(new BoneTuple(original.BoneTuples[i]));
                }
                Initialize(original.JointsUsed.Count);
            }

            /// <summary>
            /// Indicates if a joint should be drawn
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public bool IsJointVisible(int index)
            {
                if (JointsUsed == null || JointsUsed.Count == 0)
                {
                    Initialize(-1);
                }
                return JointsUsed.Get(index);
            }

            /// <summary>
            /// Indicates if tuple exists
            /// </summary>
            /// <param name="firstBone">First bone in question.</param>
            /// <param name="secondBone">Second bone in question.</param>
            /// <returns>True if so, false if not.</returns>
            public bool DoesTupleExist(int firstBone, int secondBone)
            {
                if (BoneTuples == null || BoneTuples.Count == 0)
                {
                    return false;
                }
                foreach (var currentBoneTuple in BoneTuples)
                {
                    if (currentBoneTuple.Is(firstBone, secondBone))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Indicates if tuple exists
            /// </summary>
            public bool DoesTupleExist(BoneTuple tuple)
            {
                return DoesTupleExist(tuple.FirstBoneId, tuple.SecondBoneId);
            }

            /// <summary>
            /// Sets the bone visual array to all bones possible.
            /// Call this before <see cref="Initialize(int)"/>
            /// </summary>
            public void FillArrayWithAllBones(int count,
                Func<int, BoneTuple> BoneIdToTuple)
            {
                BoneTuples = new List<BoneTuple>();
                for (int currentBone = 0; currentBone < count; currentBone++)
                {
                    BoneTuple boneTuple = BoneIdToTuple(currentBone);
                    if (DoesTupleExist(boneTuple))
                    {
                        continue;
                    }
                    BoneTuples.Add(boneTuple);
                }
            }

            /// <summary>
            /// Initializes a bit flag array to determine if bones are being used
            /// </summary>
            /// <param name="jointCount">If <= 0, read <see cref="BoneTuples"/></param>
            public void Initialize(int jointCount)
            {
                if (jointCount <= 0)
                {
                    BoneTuples.ForEach(bt => jointCount = Math.Max(jointCount,
                        Math.Max(bt.FirstBoneId, bt.SecondBoneId)));
                    ++jointCount;
                }
                if (JointsUsed == null || JointsUsed.Count < jointCount)
                {
                    JointsUsed = new BitArray(jointCount);
                }
                else
                {
                    JointsUsed.SetAll(false);
                }
                for (int i = 0; i < BoneTuples.Count; ++i)
                {
                    BoneTuple boneTuple = BoneTuples[i];
                    if (boneTuple.Hide)
                    {
                        continue;
                    }
                    if (boneTuple.FirstBoneId >= 0
                    && boneTuple.FirstBoneId < JointsUsed.Count)
                    {
                        JointsUsed.Set(boneTuple.FirstBoneId, true);
                    }
                    if (boneTuple.SecondBoneId >= 0
                    && boneTuple.SecondBoneId < JointsUsed.Count)
                    {
                        JointsUsed.Set(boneTuple.SecondBoneId, true);
                    }
                }
                _lastKnownTupleCount = BoneTuples.Count;
            }
        }

        /// <summary>
        /// The type of guide used to visualize bones.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.VisualizationGuideType)]
        protected VisualizationGuideType _visualizationGuideType =
            VisualizationGuideType.BoneVisualData;

        /// <summary>
        /// Mask to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.MaskToVisualize)]
        [Interaction.ConditionalHide("_visualizationGuideType", 0)]
        protected AvatarMask _maskToVisualize = null;

        /// <summary>
        /// Custom bone visual data, which allows custom pairing of bones for line rendering.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.BoneVisualData)]
        [Interaction.ConditionalHide("_visualizationGuideType", 1)]
        [ContextMenuItem(nameof(UseStandardBones),nameof(UseStandardBones))]
        protected CustomBoneVisualData _customBoneVisualData;

        /// <summary>
        /// Without real <see cref="CustomBoneVisualData"/>, use this common data
        /// </summary>
        private static CustomBoneVisualData _defaultBoneVisualData;

        /// <summary>
        /// Line renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.LineRendererPrefab)]
        protected GameObject _lineRendererPrefab;

        /// <summary>
        /// Axis renderer to use for visualization.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.AxisRendererPrefab)]
        protected GameObject _axisRendererPrefab;

        /// <summary>
        /// Indicates what kind of visual is desired.
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerTooltips.VisualType)]
        protected VisualType _visualType = VisualType.None;

        /// <summary>
        /// Does <see cref="_visualType"/> include <see cref="VisualType.Lines"/>
        /// </summary>
        public bool IsShowingLines
        {
            get => (_visualType & VisualType.Lines) != 0;
            set
            {
                if (value)
                {
                    _visualType |= VisualType.Lines;
                }
                else
                {
                    _visualType &= ~VisualType.Lines;
                }
            }
        }

        /// <summary>
        /// Does <see cref="_visualType"/> include <see cref="VisualType.Axes"/>
        /// </summary>
        public bool IsShowingAxes
        {
            get => (_visualType & VisualType.Axes) != 0;
            set
            {
                if (value)
                {
                    _visualType |= VisualType.Axes;
                }
                else
                {
                    _visualType &= ~VisualType.Axes;
                }
            }
        }

        /// <inheritdoc/>
        protected virtual void Awake()
        {
            Assert.IsNotNull(_lineRendererPrefab);
            Assert.IsNotNull(_axisRendererPrefab);
            ResetBoneVisuals();
        }

        protected virtual void OnDisable()
        {
            ResetBoneVisuals();
        }

        /// <summary>
        /// Clears existing bones. Prompts generation in <see cref="Update"/>.
        /// </summary>
        protected void ResetBoneVisuals()
        {
            _customBoneVisualData.Initialize(GetBoneCount());
            ClearBoneTupleVisualObjects();
            ClearBoneAxisVisualObjects();
        }

        /// <inheritdoc/>
        protected virtual void Start()
        {
            if (_customBoneVisualData.BoneTuples == null ||
            _customBoneVisualData.BoneTuples.Count == 0)
            {
                UseCommonDefaultBones();
            }
        }

        /// <inheritdoc/>
        protected virtual void Update()
        {
            if (_customBoneVisualData.IsChanged)
            {
                bool modifiedDefaults = _customBoneVisualData == _defaultBoneVisualData;
                ResetBoneVisuals();
                if (modifiedDefaults)
                {
                    _customBoneVisualData = new CustomBoneVisualData(_customBoneVisualData);
                    _defaultBoneVisualData.FillArrayWithAllBones(GetBoneCount(), GetBoneTuple);
                    _defaultBoneVisualData.Initialize(GetBoneCount());
                }
            }
            if (_whenToRender == WhenToRender.Update)
            {
                Visualize();
            }
        }

        /// <summary>
        /// MonoBehaviour.LateUpdate
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (_whenToRender != WhenToRender.Automatic
            && _whenToRender != WhenToRender.LateUpdate)
            {
                return;
            }
            Visualize();
        }

        /// <summary>
        /// Applies bone positions & rotations to axes & lines visuals
        /// </summary>
        public override void Visualize()
        {
            VisualizeJoints();
            VisualizeBoneLines();
        }

        /// <summary>
        /// Selects all bones for visualization.
        /// </summary>
        public override void GenerateList()
        {
            switch (_visualizationGuideType)
            {
                case VisualizationGuideType.AvatarMask:
                    _maskToVisualize = CreateAllBonesMask();
                    break;
                case VisualizationGuideType.BoneVisualData:
                    UseStandardBones();
                    break;
            }
        }

        protected void UseStandardBones()
        {
            _customBoneVisualData = new CustomBoneVisualData();
            _customBoneVisualData.FillArrayWithAllBones(GetBoneCount(), GetBoneTuple);
        }

        /// <summary>
        /// Allocates bone visual data only once for all default skeletons
        /// </summary>
        private void UseCommonDefaultBones()
        {
            if (_defaultBoneVisualData == null)
            {
                _defaultBoneVisualData = new CustomBoneVisualData();
                _defaultBoneVisualData.FillArrayWithAllBones(GetBoneCount(), GetBoneTuple);
                _defaultBoneVisualData.Initialize(GetBoneCount());
            }
            _customBoneVisualData = _defaultBoneVisualData;
        }

        /// <summary>
        /// Resets all visual data.
        /// </summary>
        public override void ClearList()
        {
            _maskToVisualize = null;
            _customBoneVisualData = new CustomBoneVisualData();
            _customBoneVisualData.BoneTuples.Clear();
            ClearBoneTupleVisualObjects();
            ClearBoneAxisVisualObjects();
        }

        protected void ClearBoneTupleVisualObjects()
        {
            foreach (var value in _boneTupleToLineRenderer)
            {
                Destroy(value.Value.gameObject);
            }
            _boneTupleToLineRenderer.Clear();
        }

        protected void ClearBoneAxisVisualObjects()
        {
            foreach (var value in _boneIdToAxisObject)
            {
                Destroy(value.Value.gameObject);
            }
            _boneIdToAxisObject.Clear();
        }

        protected AvatarMask CreateAllBonesMask()
        {
            var allBonesMask = new AvatarMask();
            allBonesMask.InitializeDefaultValues(true);
            return allBonesMask;
        }

        private void VisualizeJoints()
        {
            int count = GetBoneCount();
            if (!IsShowingAxes)
            {
                for (var currentBone = 0; currentBone < count; currentBone++)
                {
                    EnforceAxisRendererEnableState(currentBone, false);
                }
                return;
            }
            for (var currentBone = 0; currentBone < count; currentBone++)
            {
                if (!ShouldVisualizeJoint(currentBone))
                {
                    EnforceAxisRendererEnableState(currentBone, false);
                    continue;
                }
                SetUpAxisRenderer(currentBone);
                EnforceAxisRendererEnableState(currentBone, true);
            }
        }

        protected bool ShouldVisualizeJoint(int bone)
        {
            switch (_visualizationGuideType)
            {
                case VisualizationGuideType.AvatarMask:
                    var bodyPart = GetAvatarBodyPart(bone);
                    return _maskToVisualize == null ||
                        _maskToVisualize.GetHumanoidBodyPartActive(bodyPart);
                case VisualizationGuideType.BoneVisualData:
                    return _customBoneVisualData.IsJointVisible(bone);
            }
            return false;
        }

        private void VisualizeBoneLines()
        {
            if(!IsShowingLines)
            {
                foreach (var tupleItem in _customBoneVisualData.BoneTuples)
                {
                    EnforceCustomLineRendererEnableState(tupleItem, false);
                }
                return;
            }
            foreach (var tupleItem in _customBoneVisualData.BoneTuples)
            {
                if (tupleItem.Hide)
                {
                    EnforceCustomLineRendererEnableState(tupleItem, false);
                    continue;
                }
                SetupBoneTupleLineRenderer(tupleItem);
                EnforceCustomLineRendererEnableState(tupleItem, true);
            }
        }

        protected void SetupBoneTupleLineRenderer(BoneTuple tupleItem)
        {
            if (!_boneTupleToLineRenderer.ContainsKey(tupleItem))
            {
                var newObject = Instantiate(_lineRendererPrefab);
                newObject.name +=
                    $"{_CUSTOM_LINE_VISUAL_NAME_SUFFIX_TOKEN}" +
                    $"{tupleItem.FirstBone}-{tupleItem.SecondBone}";
                newObject.transform.SetParent(transform);
                var lineRenderer = newObject.GetComponent<LineRenderer>();
                _boneTupleToLineRenderer[tupleItem] = lineRenderer;
                OnNewLine?.Invoke(tupleItem.FirstBoneId, tupleItem.SecondBoneId, newObject);
            }
            if (GetBoneTransforms(tupleItem,
                out Transform firstJoint, out Transform secondJoint))
            {
                var lineRendererComp = _boneTupleToLineRenderer[tupleItem];
                lineRendererComp.SetPosition(0, firstJoint.position);
                lineRendererComp.SetPosition(1, secondJoint.position);
            }
        }

        protected bool GetBoneTransforms(BoneTuple tupleItem,
            out Transform firstJoint, out Transform secondJoint)
        {
            if (!TryGetBoneTransforms(tupleItem, out firstJoint, out secondJoint))
            {
                return false;
            }
            if (firstJoint == null || secondJoint == null)
            {
                string edge = $"{tupleItem.FirstBone}-{tupleItem.SecondBone}";
                int i = _customBoneVisualData.BoneTuples.IndexOf(tupleItem);
                Debug.LogWarning($"Cannot find transform for tuple {edge}\n" +
                    $"<color=#888800>Hiding {this}." +
                    $"{nameof(_customBoneVisualData)}." +
                    $"{nameof(_customBoneVisualData.BoneTuples)}[{i}]:" +
                    $" {edge}</color>");
                tupleItem.Hide = true;
                return false;
            }
            return true;
        }

        protected void SetUpAxisRenderer(int currentBone)
        {
            var boneTransform = GetBoneTransform(currentBone);
            if (boneTransform == null)
            {
                return;
            }

            if (!_boneIdToAxisObject.ContainsKey(currentBone))
            {
                GameObject newObject = Instantiate(_axisRendererPrefab);
                BoneType bone = (BoneType)(object)currentBone;
                newObject.name += $"{_AXIS_VISUAL_NAME_SUFFIX_TOKEN}{bone}";
                newObject.transform.SetParent(transform);
                _boneIdToAxisObject[currentBone] = newObject.transform;
                OnNewAxis?.Invoke(currentBone, newObject);
            }

            var axisComp = _boneIdToAxisObject[currentBone];
            axisComp.position = boneTransform.position;
            axisComp.rotation = boneTransform.rotation;
        }

        protected void EnforceCustomLineRendererEnableState(BoneTuple tuple, bool enableValue)
        {
            if (!_boneTupleToLineRenderer.ContainsKey(tuple))
            {
                return;
            }
            _boneTupleToLineRenderer[tuple].enabled = enableValue;
        }

        protected void EnforceAxisRendererEnableState(int bodyBone, bool enableValue)
        {
            if (!_boneIdToAxisObject.ContainsKey(bodyBone))
            {
                return;
            }
            _boneIdToAxisObject[bodyBone].gameObject.SetActive(enableValue);
        }

        /// <summary>
        /// Should be a constant time function returning the count of possible bone/joints
        /// </summary>
        /// <returns>number of bones/joints in this skeleton</returns>
        protected abstract int GetBoneCount();

        /// <summary>
        /// Gets standard begin and end joints of a bone
        /// </summary>
        /// <returns>the default <see cref="BoneTuple"/> for this bone</returns>
        protected abstract BoneTuple GetBoneTuple(int currentBone);

        /// <summary>
        /// Used to resolve a joint when rendering Axis
        /// </summary>
        /// <returns>the <see cref="Transform"/> associated with the given bone ID</returns>
        protected abstract Transform GetBoneTransform(int currentBone);

        /// <summary>
        /// Gets both bone transforms of the given <see cref="BoneTuple"/>
        /// </summary>
        /// <returns>true if the bones could be retrieved</returns>
        protected abstract bool TryGetBoneTransforms(BoneTuple tupleItem,
            out Transform firstJoint, out Transform secondJoint);

        /// <summary>
        /// Used with Unity's <see cref="AvatarMask"/>, which selects specific parts of an avatar
        /// </summary>
        /// <returns>the <see cref="AvatarMaskBodyPart"/> containing the given bone</returns>
        protected abstract AvatarMaskBodyPart GetAvatarBodyPart(int currentBone);
    }
}
