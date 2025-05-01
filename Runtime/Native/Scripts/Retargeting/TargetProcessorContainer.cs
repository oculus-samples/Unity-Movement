// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Contains all possible target processors, including the current one being used.
    /// </summary>
    [Serializable]
    public class TargetProcessorContainer
    {
        /// <summary>
        /// The current processor type.
        /// </summary>
        public TargetProcessor.ProcessorType CurrentProcessorType => _currentProcessorType;

        /// <summary>
        /// The currently selected processor type to be used for target skeleton processing.
        /// </summary>
        [SerializeField]
        protected TargetProcessor.ProcessorType _currentProcessorType;

        /// <summary>
        /// Reference to the twist skeletal processor implementation that handles joint twist calculations.
        /// </summary>
        [SerializeField]
        protected TwistSkeletalProcessor _twistProcessor;

        /// <summary>
        /// Reference to the animation skeletal processor implementation that handles animation blending.
        /// </summary>
        [SerializeField]
        protected AnimationSkeletalProcessor _animationProcessor;

        /// <summary>
        /// Reference to the hip pinning skeletal processor implementation that handles hip position constraints.
        /// </summary>
        [SerializeField]
        protected HipPinningSkeletalProcessor _hipPinningProcessor;

        /// <summary>
        /// Reference to the locomotion skeletal processor implementation that handles movement and foot placement.
        /// </summary>
        [SerializeField]
        protected LocomotionSkeletalProcessor _locomotionProcessor;

        /// <summary>
        /// Reference to the CCD (Cyclic Coordinate Descent) skeletal processor implementation that handles inverse kinematics.
        /// </summary>
        [SerializeField]
        protected CCDSkeletalProcessor _ccdProcessor;

        /// <summary>
        /// Returns the current <see cref="TargetProcessor"/> based on the current type
        /// saved.
        /// </summary>
        /// <returns>The derived <see cref="TargetProcessor"/> type.</returns>
        public TargetProcessor GetCurrentProcessor()
        {
            switch (_currentProcessorType)
            {
                case TargetProcessor.ProcessorType.Twist:
                    return _twistProcessor;
                case TargetProcessor.ProcessorType.Animation:
                    return _animationProcessor;
                case TargetProcessor.ProcessorType.HipPinning:
                    return _hipPinningProcessor;
                case TargetProcessor.ProcessorType.Locomotion:
                    return _locomotionProcessor;
                case TargetProcessor.ProcessorType.CCDIK:
                    return _ccdProcessor;
                default:
                    return null;
            }
        }
    }
}
