// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The source data provider interface, which provides source data.
    /// </summary>
    public interface ISourceDataProvider
    {
        /// <summary>
        /// The source current skeleton pose data.
        /// </summary>
        /// <returns>The array of transforms that represents the current skeleton pose data.</returns>
        public NativeArray<NativeTransform> GetSkeletonPose();

        /// <summary>
        /// The source T-Pose skeleton pose data.
        /// </summary>
        /// <returns>The array of transforms that represents the skeleton T-Pose data.</returns>
        public NativeArray<NativeTransform> GetSkeletonTPose();

        /// <summary>
        /// The current manifestation that should be used for retargeting.
        /// </summary>
        /// <returns>The current manifestation name.</returns>
        public string GetManifestation();

        /// <summary>
        /// Returns true if the current pose data is valid.
        /// </summary>
        /// <returns>True if the current pose data is valid.</returns>
        public bool IsPoseValid();

        /// <summary>
        /// Returns true if there is a new updated T-Pose available.
        /// </summary>
        /// <returns>True if there is a new T-Pose available.</returns>
        public bool IsNewTPoseAvailable();
    }
}
