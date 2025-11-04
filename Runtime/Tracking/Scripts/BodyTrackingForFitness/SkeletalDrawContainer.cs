// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using Meta.XR.Movement.BodyTrackingForFitness;
using Meta.XR.Movement.Retargeting;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Fitness
{
    /// <summary>
    /// Allows using skeletal draw class as a container.
    /// </summary>
    public class SkeletalDrawContainer : MonoBehaviour
    {
        /// <summary>
        /// Indicates how many bones are being visualized.
        /// </summary>
        public int NumBonesToDraw => _nativePose.Length;

        /// <summary>
        /// Controls skeletal thickness.
        /// </summary>
        [SerializeField]
        protected float _thickness = 0.005f;

        /// <summary>
        /// Transforms game object.
        /// </summary>
        [SerializeField]
        protected BodyPoseBoneTransforms _bodyPoseTransforms;

        /// <summary>
        /// Retargeting body data asset.
        /// </summary>
        [SerializeField]
        protected TextAsset _data;

        /// <summary>
        /// The indexes to ignore when doing skeleton draw.
        /// </summary>
        [SerializeField]
        protected List<int> _indexesToIgnoreDraw;

        /// <summary>
        /// Use this to change the color.
        /// </summary>
        [SerializeField]
        protected Color _defaultColor = Color.white;

        private readonly SkeletonDraw _skeletalDraw = new();
        private NativeArray<MSDKUtility.NativeTransform> _nativePose;
        private int[] _parentIndices;

        private void Awake()
        {
            Assert.IsNotNull(_bodyPoseTransforms);
            _skeletalDraw.InitDraw(_defaultColor, _thickness);
            _skeletalDraw.IndexesToIgnore = _indexesToIgnoreDraw;
        }

        private void Start()
        {
            InitializeParentIndices();
        }

        private void InitializeParentIndices()
        {
            MSDKUtility.CreateOrUpdateHandle(_data.text, out var handle);
            MSDKUtility.GetParentJointIndexes(handle, MSDKUtility.SkeletonType.SourceSkeleton, out var indices);
            _parentIndices = indices.ToArray();
        }

        private void OnDestroy()
        {
            if (_nativePose.IsCreated)
            {
                _nativePose.Dispose();
            }
        }

        private void UpdateCollections()
        {
#if ISDK_DEFINED
            var boneTransforms = _bodyPoseTransforms.BoneTransforms;
            if (!_nativePose.IsCreated || _nativePose.Length != boneTransforms.Count)
            {
                _nativePose = new NativeArray<MSDKUtility.NativeTransform>(
                    boneTransforms.Count, Allocator.Persistent);
            }

            if (_parentIndices.Length != boneTransforms.Count)
            {
                InitializeParentIndices();
            }

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                _nativePose[i] = new MSDKUtility.NativeTransform(boneTransforms[i]);
            }
#endif
        }

        private void Update()
        {
            UpdateCollections();
            _skeletalDraw.LoadDraw(_parentIndices.Length, _parentIndices, _nativePose);
            _skeletalDraw.Draw();
        }

        /// <summary>
        /// Colors a part of the skeleton by a certain color.
        /// </summary>
        /// <param name="index">Index of bone.</param>
        /// <param name="newColor"></param>
        public void ColorSkeletalBoneByIndex(int index, Color newColor)
        {
            _skeletalDraw.SetBoneColor(index, newColor);
        }
    }
}
