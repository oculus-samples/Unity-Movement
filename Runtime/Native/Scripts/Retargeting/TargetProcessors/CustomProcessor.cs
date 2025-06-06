// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// A custom processor that a user can define.
    /// </summary>
    [Serializable]
    public class CustomProcessor : TargetProcessor
    {
        public CustomProcessorBehavior CustomBehaviour
        {
            get => _customBehavior;
            set => _customBehavior = value;
        }

        /// <summary>
        /// Component that is associated with custom processor implementation.
        /// </summary>
        [SerializeField]
        protected CustomProcessorBehavior _customBehavior;

        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter retargeter)
        {
            _customBehavior.Initialize(retargeter);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            _customBehavior.Destroy();
        }

        /// <inheritdoc />
        public override void UpdatePose(ref NativeArray<NativeTransform> pose)
        {
            _customBehavior.UpdatePose(ref pose, _weight);
        }

        /// <inheritdoc />
        public override void LateUpdatePose(
            ref NativeArray<NativeTransform> currentPose,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            _customBehavior.LateUpdatePose(ref currentPose, ref targetPoseLocal, _weight);
        }
    }
}
