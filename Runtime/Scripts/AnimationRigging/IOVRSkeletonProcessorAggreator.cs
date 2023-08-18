// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// For a class that reads from an <see cref="OVRSkeleton"/>. This
    /// interface aggregates <see cref="IOVRSkeletonProcessor"/>s that will
    /// modify the skeleton (eg: force hand poses, control body without input).
    /// </summary>
    public interface IOVRSkeletonProcessorAggregator
    {
        /// <summary>
        /// Adds given processor to this <see cref="IOVRSkeletonProcessorAggregator"/>
        /// </summary>
        /// <param name="processor"></param>
        public void AddProcessor(IOVRSkeletonProcessor processor);

        /// <summary>
        /// Removes given processor from <see cref="IOVRSkeletonProcessorAggregator"/>
        /// </summary>
        /// <param name="processor"></param>
        public void RemoveProcessor(IOVRSkeletonProcessor processor);
    }
}
