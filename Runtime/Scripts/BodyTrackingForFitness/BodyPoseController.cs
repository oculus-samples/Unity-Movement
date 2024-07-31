// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Movement.Utils;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Allows fine control of body poses by creating a single auditable and editable
    /// <see cref="Pose"/> list.
    /// </summary>
    public class BodyPoseController : MonoBehaviour, IBodyPose, IBody
    {
        private static class BodyPoseControllerTooltips
        {
            public const string SourceDataObject =
                "A body pose source. Can be a " + nameof(ScriptableObject) + " that inherits " +
                nameof(IBodyPose) + ". If you want to use active body data, reference " +
                nameof(OVRBodyPose) + ", or " + nameof(PoseFromBody) + ".";

            public const string BodyBoneTransforms =
                "Organizes the list of transforms to be mapped to the main list of bone poses";

            public const string BodyPoses =
                "The managed list of bone poses that collectively creates a body pose";

            public const string DrawSkeletonGizmo =
                "Flag to enable/prevent skeleton Gizmo drawing";
        }

        /// <inheritdoc cref="IBody.WhenBodyUpdated"/>
        public event Action WhenBodyUpdated = delegate { };

        /// <inheritdoc cref="IBodyPose.WhenBodyPoseUpdated"/>
        public event Action WhenBodyPoseUpdated = delegate { };

        /// <summary>
        /// "A body pose source. Can be a <see cref="ScriptableObject"/> that inherits
        /// <see cref="IBodyPose"/>. If you want to use active body data, reference
        /// <see cref="OVRBodyPose"/>, or <see cref="PoseFromBody"/>
        /// </summary>
        [Tooltip(BodyPoseControllerTooltips.SourceDataObject)]
        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _sourceDataObject;
        private IBodyPose _iPose;

        /// <summary>
        /// Organizes the list of transforms to be mapped to the list of bone poses
        /// </summary>
        [Tooltip(BodyPoseControllerTooltips.BodyBoneTransforms)]
        [SerializeField]
        protected BodyPoseBoneTransforms _bodyBoneTransforms;

        /// <summary>
        /// The managed list of bone poses that collectively creates a body pose
        /// </summary>
        [Tooltip(BodyPoseControllerTooltips.BodyPoses)]
        [ContextMenuItem(nameof(RefreshFromSourceData), nameof(RefreshFromSourceData))]
        [ContextMenuItem(nameof(RefreshTPose), nameof(RefreshTPose))]
        [EnumNamedArray(typeof(BodyBoneName))]
        [SerializeField]
        protected Pose[] _bonePoses = Array.Empty<Pose>();

        /// <summary>
        /// How many times has the body pose changed?
        /// </summary>
        private int _currentDataVersion;

        /// <inheritdoc cref="_bonePoses"/>
        public Pose[] BonePoses => _bonePoses;

        /// <inheritdoc cref="IBodyPose.SkeletonMapping"/>
        public ISkeletonMapping SkeletonMapping => _skeletonMapping != null ?
            _skeletonMapping : _skeletonMapping = new FullBodySkeletonTPose();

        /// <inheritdoc cref="IBody.IsConnected"/>
        public bool IsConnected => BodyPose != null;

        /// <inheritdoc cref="IBody.IsHighConfidence"/>
        public bool IsHighConfidence => true;

        /// <inheritdoc cref="IBody.IsTrackedDataValid"/>
        public bool IsTrackedDataValid => true;

        /// <inheritdoc cref="IBody.Scale"/>
        public float Scale => 1;

        /// <inheritdoc cref="IBody.CurrentDataVersion"/>
        public int CurrentDataVersion => _currentDataVersion;

        private static ISkeletonMapping _skeletonMapping;

        /// <inheritdoc cref="_bodyBoneTransforms"/>
        public BodyPoseBoneTransforms SkeletonTransforms
        {
            get => _bodyBoneTransforms;
            set => _bodyBoneTransforms = value;
        }

        /// <inheritdoc cref="_sourceDataObject"/>
        public IBodyPose BodyPose
        {
            get
            {
                if (_iPose != null)
                {
                    return _iPose;
                }

                _iPose = _sourceDataObject as IBodyPose;
                if (_iPose == this as IBodyPose || _iPose == _bodyBoneTransforms as IBodyPose)
                {
                    _iPose = null;
                    _sourceDataObject = null;
                }

                return _iPose;
            }
        }

        private void Reset()
        {
            FindLocalBodyPose();
            if (_sourceDataObject == null)
            {
                RefreshTPose();
            }
        }

        /// Used to assist component assignment at editor time
        internal void FindLocalBodyPose()
        {
            IBodyPose[] bodyPoses = GetComponents<IBodyPose>();
            for (int i = 0; i < bodyPoses.Length; ++i)
            {
                _sourceDataObject = bodyPoses[i] as UnityEngine.Object;
                if (_sourceDataObject == this as UnityEngine.Object)
                {
                    _sourceDataObject = null;
                }
                if (_sourceDataObject != null)
                {
                    break;
                }
            }
        }

        private void OnEnable()
        {
            if (BodyPose != null)
            {
                BodyPose.WhenBodyPoseUpdated += RefreshBonePosesFromDataSource;
            }
        }

        private void OnDisable()
        {
            if (BodyPose != null)
            {
                BodyPose.WhenBodyPoseUpdated -= RefreshBonePosesFromDataSource;
            }
        }

        /// <summary>
        /// Public imperative method that refreshes bone pose data with input from the data source.
        /// </summary>
        public void RefreshFromSourceData()
        {
            _iPose = _sourceDataObject as IBodyPose;
            if (BodyPose == null)
            {
                Debug.LogError($"invalid source data");
                return;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying && (BodyPose is PoseFromBody || BodyPose is OVRBodyPose))
            {
                Debug.LogWarning("Cannot read body data at editor time");
                return;
            }
#endif
            RefreshBonePosesFromDataSource();
        }

        private void RefreshBonePosesFromDataSource()
        {
            ReadFrom(BodyPose);
        }

        /// <summary>
        /// Forces the bone pose data into a T-pose, which is a guaranteed safe pose.
        /// </summary>
        public void RefreshTPose()
        {
            ReadFrom(new FullBodySkeletonTPose());
        }

        /// <summary>
        /// Fills the managed bone pose array with body data from the given body pose
        /// </summary>
        public void ReadFrom(IBodyPose bodyPose)
        {
            Array.Resize(ref _bonePoses, FullBodySkeletonTPose.TPose.ExpectedBoneCount);
            for (int i = 0; i < _bonePoses.Length; ++i)
            {
                if (bodyPose.GetJointPoseFromRoot((BodyJointId)i, out Pose pose))
                {
                    _bonePoses[i] = pose;
                }
            }
            NotifyChange();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Called whenever data changes to inform all listeners of the data change
        /// </summary>
        public void NotifyChange()
        {
            ++_currentDataVersion;
            WhenBodyPoseUpdated.Invoke();
            WhenBodyUpdated.Invoke();
        }

        /// <inheritdoc cref="IBody.GetRootPose"/>
        public bool GetRootPose(out Pose pose)
        {
            return GetJointPose(BodyJointId.Body_Root, out pose);
        }

        /// <inheritdoc cref="IBody.GetJointPose"/>
        public bool GetJointPose(BodyJointId bodyJointId, out Pose pose)
        {
            if (_bonePoses.Length == 0)
            {
                pose = default;
                return false;
            }
            pose = _bonePoses[(int)bodyJointId];
            return true;
        }

        /// <inheritdoc cref="IBodyPose.GetJointPoseLocal"/>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
        {
            return FullBodySkeletonTPose.GetJointPoseLocalIfFromRootIsKnown(
                this, bodyJointId, out pose);
        }

        /// <inheritdoc cref="IBodyPose.GetJointPoseFromRoot"/>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
        {
            int id = (int)bodyJointId;
            if (id >= 0 && id < _bonePoses.Length)
            {
                pose = _bonePoses[id];
                return true;
            }
            pose = default;
            return false;
        }

        public void ApplyTransformsToBonePoses()
        {
            IList<Transform> bones = _bodyBoneTransforms.BoneTransforms;
            Transform boneRoot = _bodyBoneTransforms.BoneContainer;
            Quaternion boneRootRotationOffset = Quaternion.Inverse(boneRoot.rotation);
            for (int i = 0; i < _bonePoses.Length; ++i)
            {
                Pose newChildData = new Pose(
                    boneRoot.InverseTransformPoint(bones[i].position),
                    boneRootRotationOffset * bones[i].rotation);
                _bonePoses[i] = newChildData;
            }
            NotifyChange();
        }
    }
}
