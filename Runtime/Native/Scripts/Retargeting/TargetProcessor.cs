// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Modifies source joints. Use this influence the source joints before they are
    /// retargeted to a target character.
    /// </summary>
    [Serializable]
    public abstract class TargetProcessor
    {
        /// <summary>
        /// Defines the source processor type. This can be
        /// extended in the future to support more custom versions.
        /// </summary>
        public enum ProcessorType
        {
            None = 0,
            Twist = 1,
            Animation = 2,
            Locomotion = 3,
            CCDIK = 4,
            HandIK = 5,
            HipPinning = 6,
            Custom
        }

        /// <summary>
        /// The weight of this processor.
        /// </summary>
        public float Weight
        {
            get => _weight;
            set => _weight = value;
        }

        /// <summary>
        /// The weight of this processor, controlling how much influence it has on the target skeleton.
        /// </summary>
        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float _weight = 1.0f;

        /// <summary>
        /// Editor-only property that determines whether the processor is expanded in the inspector.
        /// </summary>
        [SerializeField]
        protected bool _isFoldoutExpanded = true;

        /// <summary>
        /// Initializes the processor.
        /// </summary>
        /// <param name="retargeter">The native retargeter.</param>
        public abstract void Initialize(CharacterRetargeter retargeter);

        /// <summary>
        /// Destroys the processor.
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Processes the retargeted pose values in Update.
        /// </summary>
        /// <param name="pose">The pose that the constraint needs to affect.</param>
        public abstract void UpdatePose(ref NativeArray<NativeTransform> pose);

        /// <summary>
        /// Processes the target pose values in LateUpdate.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be updated by the constraint.</param>
        public abstract void LateUpdatePose(ref NativeArray<NativeTransform> currentPose,
            ref NativeArray<NativeTransform> targetPose);
    }
}
