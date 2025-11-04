// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
#endif
using Oculus.Movement.Utils;
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// This class has bone transform generation functionality like OVRSkeleton.
    /// These transforms can be generated in a T-pose at edit time, and that set of body
    /// transforms will persist at runtime.
    /// </summary>
    public class BodyPoseBoneTransforms : MonoBehaviour
#if ISDK_DEFINED
        , IBodyPose
#endif
    {
#if ISDK_DEFINED
        private static class BodyPoseBoneTransformsTooltips
        {
            public const string BodyPoseSource =
                "The body data being used to populate and arrange the bone transforms";

            public const string BonesRoot =
                "Where bone transforms will parent themselves to";

            public const string TransformHierarchy =
                "Determines how the bone transform hierarchy is set up";

            public const string BoneTransforms =
                "The bone transforms of each bone";
        }

        /// <summary>
        /// What kind of hierarchy architecture the bone transforms adopt
        /// </summary>
        public enum TransformHierarchy
        {
            /// <summary>
            /// How the <see cref="OVRPlugin"/> arranges bones, better for tracking
            /// </summary>
            FlatHierarchy,
            /// <summary>
            /// How artists arrange bones, better for posing
            /// </summary>
            TreeHierarchy
        }

        /// <summary>
        /// The body data being used to populate and arrange the bone transforms
        /// </summary>
        [Tooltip(BodyPoseBoneTransformsTooltips.BodyPoseSource)]
        [Interface(typeof(IBodyPose))]
        [SerializeField]
        protected UnityEngine.Object _bodyPoseSource;
        private IBodyPose _bodyPose;

        /// <summary>
        /// Where bone transforms will parent themselves to
        /// </summary>
        [Tooltip(BodyPoseBoneTransformsTooltips.BonesRoot)]
        [SerializeField]
        protected Transform bonesContainer;

        /// <summary>
        /// Determines how the bone transform hierarchy is set up
        /// </summary>
        [Tooltip(BodyPoseBoneTransformsTooltips.TransformHierarchy)]
        [SerializeField]
        protected TransformHierarchy _transformHierarchy = TransformHierarchy.TreeHierarchy;

        /// <summary>
        /// The bone transforms of each bone
        /// </summary>
        [Tooltip(BodyPoseBoneTransformsTooltips.BoneTransforms)]
        [ContextMenuItem(nameof(RefreshHierarchy), nameof(RefreshHierarchy))]
        [EnumNamedArray(typeof(BodyBoneName))]
        [SerializeField]
        protected Transform[] _boneTransforms = new Transform[0];

        /// <summary>
        /// Fast access to bone ownership, used by editor GUI
        /// </summary>
        private HashSet<Transform> _boneHashSet;

        /// <summary>
        /// <inheritdoc cref="_boneTransforms"/>.
        /// Must be read only so that HashSet stays consistent
        /// </summary>
        public ReadOnlyCollection<Transform> BoneTransforms =>
            new ReadOnlyCollection<Transform>(_boneTransforms);

        private string BoneContainerDefaultName => "Bones";

        private string BoneTransformNamePrefix => ".";

        /// <inheritdoc cref="_bodyPoseSource"/>
        public IBodyPose BodyPose
        {
            get
            {
                if (_bodyPose != null)
                {
                    return _bodyPose;
                }
                switch (_bodyPoseSource)
                {
                    case GameObject go:
                        _bodyPose = GetNonSelfLocalBodyPose(go);
                        break;
                    case Component component:
                        _bodyPose = component as IBodyPose;
                        if (_bodyPose == null)
                        {
                            _bodyPose = GetNonSelfLocalBodyPose(component.gameObject);
                        }
                        break;
                }
                return _bodyPose;
            }
            set
            {
                RemoveListener(UpdateTransformsData);
                _bodyPose = value;
                AddListener(UpdateTransformsData);
                _bodyPoseSource = value as UnityEngine.Object;
            }
        }

        /// <summary>
        /// Which Unity Transform is used as the container of all bones.
        /// </summary>
        public Transform BoneContainer
        {
            get => bonesContainer;
            set => bonesContainer = value;
        }

        private GameObject CreateNewBone() => new GameObject();

        /// <inheritdoc cref="_boneHashSet"/>
        public bool OwnsBone(Transform bone) => _boneHashSet != null
            ? _boneHashSet.Contains(bone) : false;

        /// <inheritdoc/>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
        {
            bool haveData = false;
            if (_bodyPose != null)
            {
                haveData = _bodyPose.GetJointPoseLocal(bodyJointId, out pose);
            }
            else
            {
                pose = default;
            }
            if (!haveData && IsValidBone(bodyJointId))
            {
                return FullBodySkeletonTPose.GetJointPoseLocalIfFromRootIsKnown(
                    this, bodyJointId, out pose);
            }
            return haveData;
        }

        /// <inheritdoc/>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
        {
            if (_bodyPose == null)
            {
                _bodyPose = _bodyPoseSource as IBodyPose;
            }
            bool haveData = false;
            if (_bodyPose != null)
            {
                haveData = _bodyPose.GetJointPoseFromRoot(bodyJointId, out pose);
            }
            else
            {
                pose = default;
            }
            if (!haveData && IsValidBone(bodyJointId))
            {
                haveData = GetJointPoseFromRootUsingBoneTransform(bodyJointId, out pose);
            }
            return haveData;
        }

        /// <summary>
        /// As <see cref="GetJointPoseFromRoot"/>, except that it sources data from
        /// bone transforms, not from <see cref="BodyPose"/>
        /// </summary>
        public bool GetJointPoseFromRootUsingBoneTransform(BodyJointId bodyJointId, out Pose pose)
        {
            int id = (int)bodyJointId;
            Transform bone = id >= 0 && id < _boneTransforms.Length ? _boneTransforms[id] : null;
            if (bone != null)
            {
                pose = new Pose(BoneContainer.InverseTransformPoint(bone.position),
                    Quaternion.Inverse(BoneContainer.rotation) * bone.rotation);
                return true;
            }
            pose = default;
            return false;
        }

        private bool IsValidBone(BodyJointId id) =>
            (int)id >= 0 && (int)id < _boneTransforms.Length;

        /// <inheritdoc cref="IBodyPose.SkeletonMapping"/>
        public ISkeletonMapping SkeletonMapping => BodyPose.SkeletonMapping;

        /// <inheritdoc cref="IBodyPose.WhenBodyPoseUpdated"/>
        public event Action WhenBodyPoseUpdated = delegate { };

        /// <inheritdoc cref="_transformHierarchy"/>
        public TransformHierarchy ParentHierarchy
        {
            get => _transformHierarchy;
            set
            {
                if (value == _transformHierarchy)
                {
                    return;
                }
                _transformHierarchy = value;
                RefreshHierarchy();
            }
        }

        /// <summary>
        /// Should be called when <see cref="ParentHierarchy"/> changes
        /// </summary>
        public void RefreshHierarchy()
        {
            RefreshBoneTransformsFromPoses();
            switch (ParentHierarchy)
            {
                case TransformHierarchy.FlatHierarchy:
                    for (int i = 0; i < _boneTransforms.Length; ++i)
                    {
                        Transform bone = _boneTransforms[i];
                        if (bone == null)
                        {
                            continue;
                        }
                        bone.SetParent(BoneContainer);
                    }
                    break;
                case TransformHierarchy.TreeHierarchy:
                    for (int i = 0; i < _boneTransforms.Length; ++i)
                    {
                        Transform bone = _boneTransforms[i];
                        if (bone == null)
                        {
                            continue;
                        }
                        int parentId = FullBodySkeletonTPose.TPose.GetParent(i);
                        Transform parent = parentId >= 0
                            ? _boneTransforms[parentId] : BoneContainer;
                        if (parent == null)
                        {
                            parent = BoneContainer;
                        }
#if UNITY_EDITOR
                        if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(bone))
#endif
                        {
                            bone.SetParent(parent);
                        }
                    }
                    break;
            }
        }

        public void RefreshTPose()
        {
            IBodyPose prevBodyPose = _bodyPose;
            RefreshHierarchy();
            _bodyPose = prevBodyPose;
        }

        /// <summary>
        /// Applies body pose data to the bone transform hierarchy
        /// </summary>
        public void RefreshBoneTransformsFromPoses()
        {
            if (BoneContainer == null)
            {
                CreateBoneContainerIfNeeded();
            }
            Array.Resize(ref _boneTransforms, (int)BodyJointId.Body_End);
            for (int i = 0; i < (int)BodyJointId.Body_End; ++i)
            {
                Transform childObject = _boneTransforms[i];
                bool isValid = GetJointPoseFromRoot((BodyJointId)i, out Pose pose);
                if (isValid && childObject == null)
                {
                    childObject = FindOrCreateBoneTransform(i);
                }
                if (childObject != null)
                {
                    if (isValid)
                    {
                        ApplyPoseToBone(childObject, pose);
                    }
                }
            }
            _boneHashSet = new HashSet<Transform>(_boneTransforms);
        }

        private void CreateBoneContainerIfNeeded()
        {
            // stop executing if called on destroyed object, like from an errant callback
            if (this == null)
            {
                return;
            }
            Transform self = transform;
            if (BoneContainer == null)
            {
                BoneContainer = self.Find(BoneContainerDefaultName);
            }
            if (BoneContainer == null)
            {
                BoneContainer = new GameObject(BoneContainerDefaultName).transform;
                BoneContainer.SetParent(self, false);
            }
        }

        private void ApplyPoseToBone(Transform bone, Pose pose)
        {
            Vector3 position = pose.position;
            position = BoneContainer.TransformPoint(position);
            bone.position = position;
            bone.rotation = BoneContainer.rotation * pose.rotation;
        }

        private Transform FindOrCreateBoneTransform(int boneId)
        {
            Type boneEnum = typeof(BodyBoneName);
            string childName = BoneTransformNamePrefix + boneId.ToString("000") +
                               Enum.GetName(boneEnum, boneId);
            Transform expectedParent = GetParentTransformFromHierarchy(boneId);
            Transform childObject = null;
            if (expectedParent == null)
            {
                Debug.Log($"missing parent for {(BodyJointId)boneId}, body: {name}");
            }
            else
            {
                childObject = expectedParent.Find(childName);
            }
            if (childObject == null)
            {
                childObject = CreateNewBone().transform;
                childObject.name = childName;
                childObject.SetParent(expectedParent, false);
            }
            else
            {
                int indexInList = GetBoneTransformIndex(childObject);
                if (indexInList >= 0)
                {
                    string incorrectlyReplacedChild = Enum.GetName(boneEnum, indexInList);
                    Debug.LogWarning($"{childName} already at {indexInList}" +
                                     $" ({incorrectlyReplacedChild})");
                }
            }
            _boneTransforms[boneId] = childObject;
            return childObject;
        }

        private int GetBoneTransformIndex(Transform boneTransform)
        {
            // manual for loop, because BoneTransforms.IndexOf crashes with null values
            for (int i = 0; i < _boneTransforms.Length; ++i)
            {
                if (_boneTransforms[i] == boneTransform)
                {
                    return i;
                }
            }
            return -1;
        }

        private Transform GetParentTransformFromHierarchy(int boneId)
        {
            switch (ParentHierarchy)
            {
                case TransformHierarchy.TreeHierarchy:
                    int parentId = FullBodySkeletonTPose.TPose.GetParent(boneId);
                    if (parentId >= 0)
                    {
                        return _boneTransforms[parentId];
                    }
                    break;
            }
            return BoneContainer;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _bodyPoseSource = GetNonSelfLocalBodyPose(gameObject) as UnityEngine.Object;
            BodyPoseController controller = GetComponent<BodyPoseController>();
            if (controller != null && controller.SkeletonTransforms == null)
            {
                controller.SkeletonTransforms = this;
                controller.FindLocalBodyPose();
            }
            if (_bodyPose != null)
            {
                RefreshBoneTransformsFromPoses();
            }
        }

        private void OnValidate()
        {
            // force reset of IBodyPoseProvider interface at editor time
            _bodyPose = null;
            if (bonesContainer == null)
            {
                return;
            }
            if (_bodyPoseSource == this || _bodyPose == this as IBodyPose)
            {
                _bodyPose = null;
                _bodyPoseSource = null;
                Debug.LogWarning("Should not read from self");
            }
            BodyPoseUpdateListener(true);
            UnityEditor.EditorApplication.delayCall += RefreshHierarchyDuringEditor;
        }

        public void RefreshHierarchyDuringEditor()
        {
            if (this == null)
            {
                return;
            }

            // Only execute when in the scene view (not project view) and actually modified
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                return;
            }

            RefreshHierarchy();
        }
#endif

        private IBodyPose GetNonSelfLocalBodyPose(GameObject go)
        {
            IBodyPose[] poses = go.GetComponents<IBodyPose>();
            for (int i = 0; i < poses.Length; ++i)
            {
                if (poses[i] == this as IBodyPose)
                {
                    continue;
                }
                return poses[i];
            }
            return null;
        }

        private void OnEnable() => BodyPoseUpdateListener(true);

        private void OnDisable() => BodyPoseUpdateListener(false);

        private void OnDestroy() => BodyPoseUpdateListener(false);

        private void BodyPoseUpdateListener(bool enabled)
        {
            if (enabled)
            {
                AddListener(UpdateTransformsData);
            }
            else
            {
                RemoveListener(UpdateTransformsData);
            }
        }

        /// <summary>
        /// Should be called when body pose source data changes.
        /// </summary>
        private void UpdateTransformsData()
        {
            if (this == null)
            {
                return;
            }
            if (BodyPose == null)
            {
                throw new Exception("Was not removed from");
            }
            RefreshBoneTransformsFromPoses();
            NotifyBodyPoseUpdate();
        }

        /// <summary>
        /// Notifies listeners that body pose data has been modified.
        /// </summary>
        public void NotifyBodyPoseUpdate()
        {
            WhenBodyPoseUpdated.Invoke();
        }

        /// <summary>
        /// Adds callback to listener that keeps track of source data changes
        /// </summary>
        public void AddListener(Action onPoseChange)
        {
            if (BodyPose == null)
            {
                return;
            }
            BodyPose.WhenBodyPoseUpdated -= onPoseChange;
            BodyPose.WhenBodyPoseUpdated += onPoseChange;
        }

        /// <summary>
        /// Removes callback from listener that keeps track of source data changes
        /// </summary>
        public void RemoveListener(Action onPoseChange)
        {
            if (_bodyPose == null)
            {
                return;
            }
            BodyPose.WhenBodyPoseUpdated -= onPoseChange;
        }
#endif
    }
}
