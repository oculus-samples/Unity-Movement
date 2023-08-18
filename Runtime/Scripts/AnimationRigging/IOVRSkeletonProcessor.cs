// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// For a class that modifies or filters an <see cref="OVRSkeleton"/>
    /// </summary>
    public interface IOVRSkeletonProcessor
    {
        /// <summary>
        /// Processes an <see cref="OVRSkeleton"/>s for some purpose (eg:
        /// forcing a hand pose, moving body parts without the user)
        /// </summary>
        /// <param name="bones"></param>
        public void ProcessSkeleton(OVRSkeleton skeleton);

        /// <summary>
        /// Allows this processor to be turned on and off by an
        /// <see cref="IOVRSkeletonProcessorAggregator"/>
        /// </summary>
        public bool EnableSkeletonProcessing { get; set; }

        /// <summary>
        /// Gives an <see cref="IOVRSkeletonProcessorAggregator"/> a name
        /// </summary>
        public string SkeletonProcessorLabel { get; }
    }
}
