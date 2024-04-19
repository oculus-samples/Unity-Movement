// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Takes 2 <see cref="IBodyPose"/> objects, and compares them.
    /// Pushes colors onto <see cref="BodyPoseBoneVisuals.BoneVisuals"/> bone
    /// corresponding to angle difference. Details of how each bone is compare
    /// are in the <see cref="_alignmentWiggleRoom"/> array.
    /// </summary>
    public class BodyPoseAlignmentDetector : MonoBehaviour
    {
        private static class BodyPoseAlignmentDetectorTooltips
        {
            public const string PoseA = "The first body pose to compare.";

            public const string PoseB = "The second body pose to compare.";

            public const string DetectionStyleTxt =
                "Determines bone alignment detection style: " +
                nameof(BodyPoseAlignmentDetector.DetectionStyle.JointRotation) + " or " +
                nameof(BodyPoseAlignmentDetector.DetectionStyle.BoneDirection) +
                " which is basically with or without roll.";

            public const string BoneDirectionDetectionRoot =
                "Compare bone direction with a given bone used as a reference point. This allows" +
                "direction agnostic body poses to be detected, eg: squats being detected from the " +
                "hips instead of the root (floor) so a squat in any direction is recognized.";

            public const string BoneDeltaGizmo =
                "Whether or not to draw a pose comparison visualization at editor time";

            public const string BoneVisualErrorColor = "Colors used for alignment visualization";

            public const string BoneVisualsToColor =
                "Bone visual collections to color based on the alignment of bones";

            public const string AlignmentWiggleRoom =
                nameof(BodyPoseAlignmentDetector.DetectorConfig.MaxAngleDelta) +
                ": maximum angle delta counting as alignment.\n" +
                nameof(BodyPoseAlignmentDetector.DetectorConfig.Width) +
                ": difference between what counts as in and out\n";

            public const string AlignmentState =
                "Result data structure that identifies how closely bones are matching.";

            public const string MinTimeInState =
                "Pose alignment must be maintained this many seconds to change the Active property.";

            public const string PoseEvents = "Callback container structure";

            public const string OnCompliance = "Called when all configured bones align";

            public const string OnDeficiency =
                "Called as soon as not all configured bones are aligned";

            public const string OnCompliantBoneCount =
                "Called when How many bones are aligned changes, passing the new count";

            public const string DetectorConfigId =
                "The bone to compare from each Body Pose";

            public const string DetectorConfigMaxAngleDelta =
                "Maximum angle between two bones that can be considered aligned";

            public const string DetectorConfigWidth =
                "Threshold when transitioning states. Half added to " +
                nameof(DetectorConfig.MaxAngleDelta) + " when leaving Active state, and also " +
                "subtracted when entering";
        }

        /// <summary>
        /// Callback notifies how many bones match each other
        /// </summary>
        [Serializable]
        public class UnityEvent_int : UnityEvent<int> { }

        /// <summary>
        /// Describes 2 methods of tracking bone alignment
        /// </summary>
        public enum DetectionStyle
        {
            /// <summary>
            /// Compares the quaternion, including roll of joints
            /// </summary>
            JointRotation,
            /// <summary>
            /// Only compares bone direction, ignoring roll of bone
            /// </summary>
            BoneDirection,
        }

        /// <summary>
        /// Data structure defining how strictly bones should match each other, and how much
        /// deviation is required before the bones are no longer considered matching.
        /// </summary>
        [Serializable]
        public class DetectorConfig
        {
#if UNITY_EDITOR
            // name will label the element in the Editor UI, and not exist at runtime
            [HideInInspector, SerializeField]
            private string name;

            /// <summary>
            /// Editor only. updates label in Inspector list
            /// </summary>
            public void UpdateEditorLabel()
            {
                name = ((BodyBoneName)Id).ToString();
            }
#else
            public void UpdateEditorLabel() { }
#endif
            /// <summary>
            /// The bone to compare from each Body Pose.
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.DetectorConfigId)]
            public BodyJointId Id = BodyJointId.Body_Head;

            /// <summary>
            /// Maximum angle between two bones that can be considered aligned.
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.DetectorConfigMaxAngleDelta)]
            [Min(0)]
            public float MaxAngleDelta = 30f;

            /// <summary>
            /// Threshold when transitioning states. Half added to <see cref="MaxAngleDelta"/>
            /// when leaving Active state, and also subtracted when entering.
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.DetectorConfigWidth)]
            [Min(0)]
            public float Width = 4f;
        }

        /// <summary>
        /// Result data structure that identifies how closely bones are matching.
        /// </summary>
        [Serializable]
        public struct AlignmentState
        {
#if UNITY_EDITOR
            [HideInInspector, SerializeField]
            private string name;

            /// <summary>
            /// Editor only. updates label in Inspector list
            /// </summary>
            public void UpdateEditorLabel()
            {
                name = ((BodyBoneName)Id).ToString();
            }
#else
            public void UpdateEditorLabel() { }
#endif
            /// <summary>
            /// The bone being compared
            /// </summary>
            [HideInInspector]
            public BodyJointId Id;

            /// <summary>
            /// The angle between two bones
            /// </summary>
            public float AngleDelta;

            public AlignmentState(BodyJointId id, float delta)
            {
                Id = id;
                AngleDelta = delta;
#if UNITY_EDITOR
                name = ((BodyBoneName)Id).ToString();
#endif
            }
        }

        /// <summary>
        /// Callback container structure
        /// </summary>
        [Serializable]
        public class Events
        {
            /// <summary>
            /// Called when all configured bones align
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.OnCompliance)]
            public UnityEvent OnCompliance = new UnityEvent();

            /// <summary>
            /// Called as soon as not all configured bones are aligned
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.OnDeficiency)]
            public UnityEvent OnDeficiency = new UnityEvent();

            /// <summary>
            /// Called when How many bones are aligned changes, passing the new count
            /// </summary>
            [Tooltip(BodyPoseAlignmentDetectorTooltips.OnCompliantBoneCount)]
            public UnityEvent_int OnCompliantBoneCount = new UnityEvent_int();
        }

        /// <summary>
        /// The first body pose to compare.
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.PoseA)]
        [SerializeField, Interface(typeof(IBodyPose))]
        [ContextMenuItem(nameof(SwapPoseOrder),nameof(SwapPoseOrder))]
        private UnityEngine.Object _poseA;
        private IBodyPose _iPoseA;

        /// <summary>
        /// The second body pose to compare.
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.PoseB)]
        [SerializeField, Interface(typeof(IBodyPose))]
        [ContextMenuItem(nameof(SwapPoseOrder),nameof(SwapPoseOrder))]
        private UnityEngine.Object _poseB;
        private IBodyPose _iPoseB;

        /// <summary>
        /// Determines bone alignment detection style: <see cref="DetectionStyle.JointRotation"/>
        /// or <see cref="DetectionStyle.BoneDirection"/>, which is basically with or without roll.
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.DetectionStyleTxt)]
        [SerializeField]
        protected DetectionStyle _detectionStyle = DetectionStyle.BoneDirection;

        /// <summary>
        /// Compare bone direction with a given bone used as a reference point. This allows
        /// direction agnostic body poses to be detected, eg: squats being detected from the hips
        /// instead of the root (floor) so a squat in any direction is recognized.
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.BoneDirectionDetectionRoot)]
        [ConditionalHide(nameof(_detectionStyle), DetectionStyle.BoneDirection)]
        [SerializeField]
        protected BodyJointId _boneDirectionDetectionRoot = BodyJointId.Invalid;

#if UNITY_EDITOR
        /// <summary>
        /// Whether or not to draw a pose comparison visualization at editor time
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.BoneDeltaGizmo)]
        [ContextMenuItem(nameof(SwapPoseOrder),nameof(SwapPoseOrder))]
        [ConditionalHide(nameof(_detectionStyle), DetectionStyle.BoneDirection)]
        [SerializeField]
        protected bool _boneDeltaGizmo;
#endif

        /// <summary>
        /// Colors used for alignment visualization
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.BoneVisualErrorColor)]
        [ContextMenuItem(nameof(ForceCompareLogic),nameof(ForceCompareLogic))]
        [SerializeField]
        protected Gradient BoneVisualErrorColor = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.green, 0),
                new GradientColorKey(Color.yellow, .125f),
                new GradientColorKey(Color.red, .25f),
                new GradientColorKey(Color.magenta, .75f),
                new GradientColorKey(Color.black, 1),
            }
        };

        /// <summary>
        /// Bone visual collections to color based on the alignment of bones
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.BoneVisualsToColor)]
        [ContextMenuItem(nameof(ForceCompareLogic),nameof(ForceCompareLogic))]
        [SerializeField] protected List<BodyPoseBoneVisuals> _boneVisualsToColor =
            new List<BodyPoseBoneVisuals>();

        /// <summary>
        /// <see cref="DetectorConfig.MaxAngleDelta"/>: maximum angle delta counting as alignment
        /// <see cref="DetectorConfig.Width"/>: difference between what counts as in and out
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.AlignmentWiggleRoom)]
        [ContextMenuItem(nameof(LabelConfigs),nameof(LabelConfigs))]
        [ContextMenuItem(nameof(CheckMostBones),nameof(CheckMostBones))]
        [ContextMenuItem(nameof(SelectTrackedBoneVisuals),nameof(SelectTrackedBoneVisuals))]
        [ContextMenuItem(nameof(SelectUntrackedBoneVisuals),nameof(SelectUntrackedBoneVisuals))]
        [SerializeField]
        private List<DetectorConfig> _alignmentWiggleRoom = new List<DetectorConfig>();

        /// <inheritdoc cref="AlignmentState"/>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.AlignmentState)]
        [SerializeField]
        protected List<AlignmentState> _alignmentStates = new List<AlignmentState>();

        /// <summary>
        /// Pose alignment must be maintained this many seconds to change the Active property.
        /// </summary>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.MinTimeInState)]
        [SerializeField]
        private float _minTimeInState = 0.05f;

        /// <inheritdoc cref="Events"/>
        [Tooltip(BodyPoseAlignmentDetectorTooltips.PoseEvents)]
        [SerializeField]
        protected Events _poseEvents = new Events();

        /// <summary>
        /// Normalizes comparisons to be relative to a specific bone
        /// </summary>
        private Pose _boneDirectionOffsetA, _boneDirectionOffsetB;
        private bool _isActive;
        private bool _isActiveLastFrame;
        private bool _internalActive;
        private float _lastStateChangeTime;
        private int _compliantBonesKnown;

        /// <inheritdoc cref="_poseA"/>
        public IBodyPose PoseA
        {
            get => _iPoseA != null ? _iPoseA : _iPoseA = _poseA as IBodyPose;
            set => _poseA = (_iPoseA = value) as UnityEngine.Object;
        }

        /// <inheritdoc cref="_poseB"/>
        public IBodyPose PoseB
        {
            get => _iPoseB != null ? _iPoseB : _iPoseB = _poseB as IBodyPose;
            set => _poseB = (_iPoseB = value) as UnityEngine.Object;
        }

        /// <summary>
        /// True if <see cref="PoseA"/> is aligned with <see cref="PoseB"/>
        /// </summary>
        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }
                return UpdateActiveState();
            }
        }

        private void RefreshPoseReferences()
        {
            _iPoseA = _poseA as IBodyPose;
            _iPoseB = _poseB as IBodyPose;
        }

        private void OnEnable()
        {
            RefreshPoseReferences();
            AddEvents();
        }

        private void OnDisable()
        {
            RemoveEvents();
        }

        private void Reset()
        {
            CheckMostBones();
        }

        /// <summary>
        /// Assign this detector to compare every bone
        /// </summary>
        private void CheckMostBones()
        {
            List<DetectorConfig> configs = new List<DetectorConfig>();
            HashSet<BodyJointId> bonesToTrack = new HashSet<BodyJointId>();
            Array.ForEach(BoneGroup.CommonBody, b => bonesToTrack.Add(b));
            Array.ForEach(BoneGroup.LeftHand, b => bonesToTrack.Add(b));
            Array.ForEach(BoneGroup.RightHand, b => bonesToTrack.Add(b));
            Array.ForEach(BoneGroup.CommonIgnored, b => bonesToTrack.Remove(b));
            foreach (BodyJointId id in bonesToTrack)
            {
                DetectorConfig config = new DetectorConfig
                {
                    Id = id, MaxAngleDelta = 30, Width = 4
                };
                configs.Add(config);
            }
            SetJointCompareConfigs(configs);
            ForceCompareLogic();
        }

        /// <summary>
        /// Allows the alignment sensitivity to be set with a script
        /// </summary>
        public void SetJointCompareConfigs(IEnumerable<DetectorConfig> configs)
        {
            _alignmentWiggleRoom = new List<DetectorConfig>(configs);
#if UNITY_EDITOR
            LabelConfigs();
            EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            HandlePoseChangesAtEditorTime();
            LabelConfigs();
            ForceCompareLogic();
        }

        private void HandlePoseChangesAtEditorTime()
        {
            bool poseAChanged = (_poseA != null && _iPoseA != _poseA as IBodyPose);
            bool poseBChanged = (_poseB != null && _iPoseB != _poseB as IBodyPose);
            if (poseAChanged)
            {
                _iPoseA = _poseA as IBodyPose;
            }
            if (poseBChanged)
            {
                _iPoseB = _poseB as IBodyPose;
            }
            if (poseAChanged || poseBChanged)
            {
                AddEvents();
            }
        }

        private void LabelConfigs()
        {
            _alignmentWiggleRoom.ForEach(c => c.UpdateEditorLabel());
        }

        private void SwapPoseOrder()
        {
            (_poseA, _poseB) = (_poseB, _poseA);
            (_iPoseA, _iPoseB) = (_iPoseB, _iPoseA);
        }
#else
        private void SwapPoseOrder(){}
        private void LabelConfigs(){}
#endif

        private void AddEvents()
        {
            if (PoseA != null)
            {
                AddEvent(PoseA, CompareLogic);
            }
            if (PoseB != null)
            {
                AddEvent(PoseB, CompareLogic);
            }
        }

        private void RemoveEvents()
        {
            if (PoseA != null)
            {
                RemoveEvent(PoseA, CompareLogic);
            }
            if (PoseB != null)
            {
                RemoveEvent(PoseB, CompareLogic);
            }
        }

        private void AddEvent(IBodyPose bodyPose, Action action)
        {
            bodyPose.WhenBodyPoseUpdated -= action;
            bodyPose.WhenBodyPoseUpdated += action;
        }

        private void RemoveEvent(IBodyPose bodyPose, Action action)
        {
            bodyPose.WhenBodyPoseUpdated -= action;
        }

        /// <summary>
        /// Force compare logic to happen, updating <see cref="_alignmentStates"/>
        /// </summary>
        public void ForceCompareLogic()
        {
            RefreshPoseReferences();
            CompareLogic();
        }

        private void CompareLogic()
        {
            bool compliant = false;
            if (_iPoseA != null && _iPoseB != null)
            {
                compliant = Active;
                if (compliant != _isActiveLastFrame)
                {
                    if (compliant)
                    {
                        _poseEvents.OnCompliance.Invoke();
                    }
                    else
                    {
                        _poseEvents.OnDeficiency.Invoke();
                    }
                }
            }
            _isActiveLastFrame = compliant;
            _alignmentStates.ForEach(
                state => _boneVisualsToColor.ForEach(visuals => ColorBoneByState(visuals, state)));
        }

        private bool UpdateActiveState()
        {
            bool wasActive = _internalActive;
            _internalActive = true;
            UpdateBoneDirectionOffset();
            int compliantBones = 0;
            _alignmentStates.Clear();
            foreach (var config in _alignmentWiggleRoom)
            {
                float maxDelta = wasActive ?
                    config.MaxAngleDelta + config.Width / 2f :
                    config.MaxAngleDelta - config.Width / 2f;
                bool withinDelta = GetBoneAlignmentDelta(config.Id, out float delta) &&
                                   Mathf.Abs(delta) <= maxDelta;
                _alignmentStates.Add(new AlignmentState(config.Id, delta));
                if (withinDelta)
                {
                    ++compliantBones;
                }
                _internalActive &= withinDelta;
            }

            if (compliantBones != _compliantBonesKnown)
            {
                _poseEvents.OnCompliantBoneCount.Invoke(compliantBones);
                _compliantBonesKnown = compliantBones;
            }
            float time = Time.time;
            if (wasActive != _internalActive)
            {
                _lastStateChangeTime = time;
            }
            if (time - _lastStateChangeTime >= _minTimeInState)
            {
                _isActive = _internalActive;
            }
            return _isActive;
        }

        private void UpdateBoneDirectionOffset()
        {
            if (_boneDirectionDetectionRoot == BodyJointId.Invalid)
            {
                _boneDirectionOffsetA = _boneDirectionOffsetB =
                    new Pose(Vector3.zero, Quaternion.identity);
                return;
            }
            if (_iPoseA != null)
            {
                _iPoseA.GetJointPoseFromRoot(_boneDirectionDetectionRoot, out Pose bonePoseA);
                PoseUtils.Inverse(bonePoseA, ref _boneDirectionOffsetA);
            }
            if (_iPoseB != null)
            {
                _iPoseB.GetJointPoseFromRoot(_boneDirectionDetectionRoot, out Pose bonePoseB);
                PoseUtils.Inverse(bonePoseB, ref _boneDirectionOffsetB);
            }
        }

        private bool GetBoneAlignmentDelta(BodyJointId joint, out float delta)
        {
            if (_iPoseA == null || _iPoseB == null)
            {
                Debug.LogWarning($"{this} cannot detect delta without two input poses");
                delta = 0;
                return false;
            }
            switch (_detectionStyle)
            {
                case DetectionStyle.JointRotation: return GetJointDelta(joint, out delta);
                case DetectionStyle.BoneDirection: return GetBoneDirectionDelta(joint, out delta);
            }
            delta = 0;
            return false;
        }

        private bool GetJointDelta(BodyJointId joint, out float delta)
        {
            if (!_iPoseA.GetJointPoseLocal(joint, out Pose localA) ||
                !_iPoseB.GetJointPoseLocal(joint, out Pose localB))
            {
                delta = 0;
                return false;
            }
            delta = Quaternion.Angle(localA.rotation, localB.rotation);
            return true;
        }

        private bool GetBoneDirectionDelta(BodyJointId joint, out float delta)
        {
            if (!GetBoneDirection(_iPoseA, joint, _boneDirectionOffsetA, out Vector3 dirA) ||
                !GetBoneDirection(_iPoseB, joint, _boneDirectionOffsetB, out Vector3 dirB))
            {
                delta = 0;
                return false;
            }
            delta = Vector3.Angle(dirA, dirB);
            return true;
        }

        private static bool GetBoneDirection(IBodyPose bodyPose, BodyJointId id, Pose offset, out Vector3 direction)
        {
            if (!bodyPose.GetJointPoseFromRoot(id, out Pose joint))
            {
                direction = Vector3.zero;
                return false;
            }
            Quaternion forwardRot = FullBodySkeletonTPose.TPose.GetForwardRotation((int)id);
            Quaternion boneRotation = offset.rotation * joint.rotation * forwardRot;
            direction = boneRotation * Vector3.forward;
            return true;
        }

        private void ColorBoneByState(BodyPoseBoneVisuals boneVisuals, AlignmentState state)
        {
            int id = (int)state.Id;
            if (boneVisuals == null || boneVisuals.BoneVisuals.Count == 0 ||
                id < 0 || id >= boneVisuals.BoneVisuals.Count)
            {
                return;
            }
            Transform boneTransform = boneVisuals.BoneVisuals[id];
            if (boneTransform == null)
            {
                return;
            }
            Renderer r = boneTransform.GetComponentInChildren<Renderer>();
            const float MaximumAngleDifference = 180;
            float progress = state.AngleDelta / MaximumAngleDifference;
            if (r != null)
            {
                Color color = progress >= 0 ? BoneVisualErrorColor.Evaluate(progress) : Color.clear;
                if (!Application.isPlaying)
                {
                    Material m = new Material(r.sharedMaterial);
                    m.color = color;
                    r.sharedMaterial = m;
                }
                else
                {
                    r.material.color = color;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_boneDeltaGizmo)
            {
                return;
            }
            RefreshPoseReferences();
            DrawSkeletonsBoneDirectionsGizmo(
                _iPoseB, _iPoseA, transform, _boneDirectionDetectionRoot);
        }

        private static void DrawSkeletonsBoneDirectionsGizmo(
            IBodyPose fullBodyA, IBodyPose fullBodyB, Transform root, BodyJointId referenceBone)
        {
            if (fullBodyA == null || fullBodyB == null)
            {
                return;
            }
            fullBodyA.GetJointPoseFromRoot(referenceBone, out Pose rootPoseA);
            fullBodyB.GetJointPoseFromRoot(referenceBone, out Pose rootPoseB);
            Pose offsetA = default, offsetB = default;
            if (referenceBone != BodyJointId.Invalid)
            {
                PoseUtils.Inverse(rootPoseA, ref offsetA);
                PoseUtils.Inverse(rootPoseB, ref offsetB);
            }
            else
            {
                offsetA = offsetB = new Pose(Vector3.zero, Quaternion.identity);
            }
            for (int i = 0; i < FullBodySkeletonTPose.TPose.ExpectedBoneCount; ++i)
            {
                bool isBone =
                    GetBoneDirection(fullBodyA, (BodyJointId)i, offsetA, out Vector3 dirA);
                if (isBone)
                {
                    float len = FullBodySkeletonTPose.TPose.GetBoneLength(i);
                    fullBodyA.GetJointPoseFromRoot((BodyJointId)i, out Pose poseA);
                    Vector3 startA = poseA.position;
                    dirA = rootPoseA.rotation * dirA;
                    Vector3 endA = startA + dirA * len;
                    GetBoneDirection(fullBodyB, (BodyJointId)i, offsetB, out Vector3 dirB);
                    dirB = rootPoseA.rotation * dirB;
                    Vector3 startB = startA;
                    Vector3 endB = startB + dirB * len;
                    startA = root.TransformPoint(startA);
                    endA = root.TransformPoint(endA);
                    startB = root.TransformPoint(startB);
                    endB = root.TransformPoint(endB);
                    Handles.DrawBezier(startA,endA,startA,endA,
                        Color.cyan,null,5);
                    Handles.DrawBezier(startB,endB,startB,endB,
                        Color.yellow,null,5);
                }
            }
        }

        private void SelectTrackedBoneVisuals()
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            foreach (BodyPoseBoneVisuals boneVisuals in _boneVisualsToColor)
            {
                _alignmentWiggleRoom.ForEach(c =>
                {
                    int id = (int)c.Id;
                    Transform boneVisual = boneVisuals.BoneVisuals[id];
                    objects.Add(boneVisual.gameObject);
                });
            }
            Selection.objects = objects.ToArray();
        }

        private void SelectUntrackedBoneVisuals()
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            HashSet<UnityEngine.Object> objectsNotToSelect = new HashSet<UnityEngine.Object>();
            foreach (BodyPoseBoneVisuals boneVisuals in _boneVisualsToColor)
            {
                _alignmentWiggleRoom.ForEach(c =>
                {
                    int id = (int)c.Id;
                    Transform boneVisual = boneVisuals.BoneVisuals[id];
                    objectsNotToSelect.Add(boneVisual.gameObject);
                });
                for(int i = 0; i < boneVisuals.BoneVisuals.Count; ++i)
                {
                    if (boneVisuals.BoneVisuals[i] == null)
                    {
                        continue;
                    }
                    GameObject boneVisual = boneVisuals.BoneVisuals[i].gameObject;
                    if (!objectsNotToSelect.Contains(boneVisual))
                    {
                        objects.Add(boneVisual);
                    }
                }
            }
            Selection.objects = objects.ToArray();
        }
#else
        private void SelectTrackedBoneVisuals(){}
        private void SelectUntrackedBoneVisuals(){}
#endif
    }
}
