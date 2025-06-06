// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows toggling debug draw of skeletons.
    /// </summary>
    public class MovementDebugDrawSkeletonMenu : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _debugDrawTransforms;

        [SerializeField]
        private CharacterRetargeter[] _retargeters;

        [SerializeField, InspectorButton("ToggleSourceSkeletonDraw")]
        private bool _toggleSourceButton;

        [SerializeField, InspectorButton("ToggleTargetSkeletonDraw")]
        private bool _toggleTargetButton;

        private void Awake()
        {
            Assert.IsTrue(_retargeters is { Length: > 0 });
            foreach (var retargeter in _retargeters)
            {
                Assert.IsNotNull(retargeter);
            }
        }

        private void Start()
        {
            for (var i = 0; i < _retargeters.Length; i++)
            {
                _retargeters[i].DebugDrawTransform = _debugDrawTransforms[i];
            }
        }

        public void ToggleSourceSkeletonDraw()
        {
            foreach (var retargeter in _retargeters)
            {
                retargeter.DebugDrawSourceSkeleton = !retargeter.DebugDrawSourceSkeleton;
            }
        }

        public void ToggleTargetSkeletonDraw()
        {
            foreach (var retargeter in _retargeters)
            {
                retargeter.DebugDrawTargetSkeleton = !retargeter.DebugDrawTargetSkeleton;
            }
        }

        public void ClearSkeletonDraw()
        {
            foreach (var retargeter in _retargeters)
            {
                retargeter.DebugDrawSourceSkeleton = false;
                retargeter.DebugDrawTargetSkeleton = false;
            }
        }
    }
}
