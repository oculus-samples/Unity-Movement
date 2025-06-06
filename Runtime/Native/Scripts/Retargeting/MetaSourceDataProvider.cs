// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Source data provider for the Meta XR Movement SDK data.
    /// </summary>
    public class MetaSourceDataProvider : OVRBody, ISourceDataProvider
    {
        /// <summary>
        /// The name of the half body manifestation for the Meta XR Movement SDK skeleton.
        /// </summary>
        public const string HalfBodyManifestation = "halfbody";

        /// <summary>
        /// The delay in seconds before body tracking data is considered valid for retargeting.
        /// </summary>
        [SerializeField]
        protected float _validBodyTrackingDelay = 0.25f;

        protected OVRPlugin.BodyJointSet _currentSkeletonType;
        protected int _skeletalChangedCount = -1;
        protected int _currentSkeletalChangeCount = -1;
        protected float _currentValidBodyTrackingTime;
        protected bool _isValid;

        private void Start()
        {
            _currentSkeletonType = ProvidedSkeletonType;
        }

        /// <inheritdoc />
        public virtual NativeArray<NativeTransform> GetSkeletonPose()
        {
            var sourcePose = SkeletonUtilities.GetPosesFromTheTracker(
                this,
                Pose.identity,
                out _currentSkeletalChangeCount,
                out _isValid);

            // Wait some time for the body tracking data to be accurate before retargeting.
            if (_currentValidBodyTrackingTime < _validBodyTrackingDelay)
            {
                _currentValidBodyTrackingTime += Time.deltaTime;
                _isValid = false;
            }

            return sourcePose;
        }

        /// <inheritdoc />
        public virtual NativeArray<NativeTransform> GetSkeletonTPose()
        {
            var sourcePose = SkeletonUtilities.GetBindPoses(this);
            _skeletalChangedCount = _currentSkeletalChangeCount;
            return sourcePose;
        }

        public virtual string GetManifestation()
        {
            return _isValid && ProvidedSkeletonType == OVRPlugin.BodyJointSet.UpperBody ? HalfBodyManifestation : null;
        }

        /// <inheritdoc />
        public virtual bool IsPoseValid()
        {
            return _isValid;
        }

        /// <inheritdoc />
        public virtual bool IsNewTPoseAvailable()
        {
            if (_currentSkeletonType == ProvidedSkeletonType)
            {
                return _currentSkeletalChangeCount != _skeletalChangedCount;
            }

            _currentSkeletonType = ProvidedSkeletonType;
            return true;
        }
    }
}
