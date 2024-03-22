// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Interaction.Body.Input;
using Oculus.Movement.Utils;
using UnityEngine;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Generates visible bones for each bone <see cref="Pose"/>
    /// </summary>
    public class BodyPoseBoneVisuals : MonoBehaviour
    {
        private static class BodyPoseBoneVisualsTooltips
        {
            public const string SkeletonTransforms =
                "Component that creates and manages bone transforms for the body being tracked";

            public const string BoneVisualPrefab =
                "Object to duplicate and scale for each bone";

            public const string BoneThickness =
                "Bone Thickness to Bone Length ratio; smaller value makes thinner bones";

            public const string ChildVisuals =
                "Bone visuals objects that are children of " + nameof(BodyPoseBoneVisuals.Skeleton);
        }

        /// <summary>
        /// Component that creates and manages bone transforms for the body being tracked
        /// </summary>
        [Tooltip(BodyPoseBoneVisualsTooltips.SkeletonTransforms)]
        [SerializeField]
        protected BodyPoseBoneTransforms _skeletonTransforms;

        /// <summary>
        /// Object to duplicate and scale for each bone
        /// </summary>
        [Tooltip(BodyPoseBoneVisualsTooltips.BoneVisualPrefab)]
        [ContextMenuItem(nameof(CreatePrimitiveCubeVisualPrefab),nameof(CreatePrimitiveCubeVisualPrefab))]
        [SerializeField]
        protected GameObject _boneVisualPrefab;
        
        /// <summary>
        /// Bone Thickness to Bone Length ratio; smaller value makes thinner bones
        /// </summary>
        [Tooltip(BodyPoseBoneVisualsTooltips.BoneThickness)]
        [SerializeField]
        protected float _boneThickness = 1f / 4;

        /// <summary>
        /// Bone visuals objects that are children of <see cref="BodyPoseBoneVisuals.Skeleton"/>
        /// </summary>
        [Tooltip(BodyPoseBoneVisualsTooltips.ChildVisuals)]
        [ContextMenuItem(nameof(RefreshVisualsInEditor),nameof(RefreshVisualsInEditor))]
        [ContextMenuItem(nameof(ClearBoneVisuals),nameof(ClearBoneVisuals))]
        [ContextMenuItem(nameof(SelectBoneVisuals),nameof(SelectBoneVisuals))]
        [EnumNamedArray (typeof(BodyJointId))]
        [SerializeField]
        protected private List<Transform> _childVisuals = new List<Transform>();
        
        private const string BoneTransformPrefix = "B.";

        /// <inheritdoc cref="_skeletonTransforms"/>
        public BodyPoseBoneTransforms Skeleton
        {
            get => _skeletonTransforms ;
            set => _skeletonTransforms = value;
        } 

        /// <inheritdoc cref="_boneVisualPrefab"/>
        public GameObject BoneVisualPrefab
        {
            get => _boneVisualPrefab;
            set => _boneVisualPrefab = value;
        }

        /// <inheritdoc cref="_childVisuals"/>
        public List<Transform> BoneVisuals => _childVisuals;

        private void Start()
        {
            if (_boneVisualPrefab == null)
            {
                CreatePrimitiveCubeVisualPrefab();
            }
        }
        
        private void OnEnable()
        {
            Skeleton.AddListener(RefreshVisuals);
        }

        private void OnDisable()
        {
            Skeleton.RemoveListener(RefreshVisuals);
        }

        private void Reset()
        {
            Skeleton = GetComponent<BodyPoseBoneTransforms>();
        }

        /// <summary>
        /// Removes all bone visuals
        /// </summary>
        public void ClearBoneVisuals()
        {
            for (int i = 0; i < _childVisuals.Count; ++i)
            {
                Transform child = _childVisuals[i];
                if (child == null){
                    continue;
                }
                DestroyImmediate(child.gameObject);
            }
            _childVisuals.RemoveAll(t => true);
        }

        /// <summary>
        /// Recreates or re-applies bone visuals to the partner <see cref="Skeleton"/>
        /// </summary>
        public void RefreshVisuals()
        {
            if (_boneVisualPrefab == null)
            {
                CreatePrimitiveCubeVisualPrefab();
            }
            int count = (int)BodyJointId.Body_End;
            for (int boneId = 0; boneId < count; ++boneId)
            {
                Transform childObject = null;
                if (_childVisuals.Count == boneId)
                {
                    _childVisuals.Add(null);
                }
                if ((childObject = _childVisuals[boneId]) == null)
                {
                    childObject = FindOrCreateChildTransform(boneId);
                }
                if (childObject != null)
                {
                    childObject.localPosition = Vector3.zero;
                    childObject.localRotation =
                        FullBodySkeletonTPose.TPose.GetForwardRotation(boneId);
                    ApplyLength(boneId, childObject);
                }
            }
        }

        private Transform FindOrCreateChildTransform(int boneId)
        {
            if (!Skeleton.GetJointPoseFromRoot((BodyJointId)boneId, out Pose pose))
            {
                if (!Skeleton.GetJointPoseFromRootUsingBoneTransform((BodyJointId)boneId, out pose))
                {
                    return null;
                }
            }
            Type boneEnum = typeof(BodyBoneName);
            string childName = BoneTransformPrefix + boneId.ToString("000") +
                               Enum.GetName(boneEnum, boneId);
            Transform boneParent = Skeleton.BoneTransforms[boneId];
            if (boneParent == null)
            {
                boneParent = Skeleton.BoneContainer;
                if (boneParent == null)
                {
                    return null;
                }
            }
            Transform childObject = boneParent.Find(childName);
            if (childObject == null)
            {
#if UNITY_EDITOR
                bool isPrefabAsset = _boneVisualPrefab.scene.name == null;
                if (isPrefabAsset)
                {
                    childObject = ((GameObject)UnityEditor.PrefabUtility
                        .InstantiatePrefab(_boneVisualPrefab)).transform;
                }
                else
                {
                    childObject = Instantiate(_boneVisualPrefab).transform;
                }
#else
                childObject = Instantiate(_boneVisualPrefab).transform;
#endif
                childObject.name = childName;
                childObject.gameObject.SetActive(true);
                childObject.SetParent(boneParent, false);
            }
            else
            {
                int indexInList = _childVisuals.IndexOf(childObject);
                if (indexInList >= 0 && indexInList != boneId)
                {
                    string incorrectlyReplacedChild = Enum.GetName(boneEnum, indexInList);
                    Debug.LogWarning(
                        $"{childName} already at {indexInList} ({incorrectlyReplacedChild})");
                }
            }
            if (_childVisuals.Count == boneId)
            {
                _childVisuals.Add(childObject);
            }
            else
            {
                _childVisuals[boneId] = childObject;
            }
            return childObject;
        }

        private void ApplyLength(int boneId, Transform visualObject)
        {
            GetBoneLength(boneId, out float boneLength, out bool haveValidEndPoint);
            float boneVisualScale = _boneThickness * boneLength;
            if (haveValidEndPoint)
            {
                visualObject.localScale =
                    new Vector3(boneVisualScale, boneVisualScale, boneLength);
            }
            else
            {
                visualObject.localScale = Vector3.one * boneVisualScale;
            }
        }

        private void GetBoneLength(int boneId, out float boneLength, out bool hasBoneTarget)
        {
            boneLength = 0;
            int targetId = FullBodySkeletonTPose.TPose.GetNext(boneId);
            hasBoneTarget = targetId >= 0;
            bool isBoneValid = Skeleton.GetJointPoseFromRoot((BodyJointId)boneId, out Pose pose);
            if (targetId >= 0 && isBoneValid)
            {
                isBoneValid = Skeleton.GetJointPoseFromRoot((BodyJointId)targetId, out Pose target);
                boneLength = isBoneValid ? Vector3.Distance(pose.position, target.position) : 0;
                hasBoneTarget = true;
            } else if (targetId < 0 && isBoneValid)
            {
                int pId = FullBodySkeletonTPose.TPose.GetParent(boneId);
                isBoneValid = Skeleton.GetJointPoseFromRoot((BodyJointId)pId, out Pose parent);
                boneLength = isBoneValid ? Vector3.Distance(pose.position, parent.position) : 0;
            }
            if (boneLength == 0)
            {
                boneLength = FullBodySkeletonTPose.TPose.GetBoneLength(boneId);
            }
        }

        /// <summary>
        /// Creates a minimally simple bone visualization prefab that scales correctly with bones
        /// </summary>
        public void CreatePrimitiveCubeVisualPrefab()
        {
            GameObject prefab = CreateCubePrefabThatScalesInOneDirection(Vector3.forward, transform);
            prefab.SetActive(false);
#if UNITY_EDITOR
            prefab.name = UnityEditor.ObjectNames.NicifyVariableName(nameof(_boneVisualPrefab));
#else
            prefab.name = nameof(_boneVisualPrefab);
#endif
            _boneVisualPrefab = prefab;
        }

        private static GameObject CreateCubePrefabThatScalesInOneDirection(Vector3 boneDirection, Transform parent)
        {
            GameObject visualPrefab = new GameObject();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Transform cubeTransform = cube.transform;
            cubeTransform.SetParent(visualPrefab.transform, false);
            cubeTransform.localPosition = boneDirection * 0.5f;
            visualPrefab.transform.SetParent(parent);
            if (Application.isPlaying)
            {
                Destroy(cubeTransform.GetComponent<BoxCollider>());
            }
            else
            {
                DestroyImmediate(cubeTransform.GetComponent<BoxCollider>());
            }
            return visualPrefab;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor time call to refresh bone visuals
        /// </summary>
        public void RefreshVisualsInEditor()
        {
            UnityEditor.EditorApplication.delayCall += RefreshVisuals;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor time call to select only bone visuals
        /// </summary>
        public void SelectBoneVisuals()
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            for(int i = 0; i < BoneVisuals.Count; ++i)
            {
                Transform boneVisual = BoneVisuals[i];
                objects.Add(boneVisual.gameObject);
            }
            UnityEditor.Selection.objects = objects.ToArray();
        }
#else
        public void RefreshVisualsInEditor() {}
        public void SelectBoneVisuals() {}
#endif
    }
}
