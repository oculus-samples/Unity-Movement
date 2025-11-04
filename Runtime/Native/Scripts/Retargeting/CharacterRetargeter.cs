// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The CharacterRetargeter takes body tracking data from OVRBody and returns a retargeted output
    /// that can be applied to a target skeleton.
    /// </summary>
    public class CharacterRetargeter : CharacterRetargeterConfig
    {
        /// <summary>
        /// If the data is valid.
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            set => _isValid = value;
        }

        /// <summary>
        /// Retargeting handle.
        /// </summary>
        public ulong RetargetingHandle => _skeletonRetargeter?.NativeHandle ?? 0;

        /// <summary>
        /// The source data provider.
        /// </summary>
        public ISourceDataProvider DataProvider => _dataProvider;

        /// <summary>
        /// Source processor containers.
        /// </summary>
        public SourceProcessorContainer[] SourceProcessorContainers => _sourceProcessorContainers;

        /// <summary>
        /// Source processor containers.
        /// </summary>
        public TargetProcessorContainer[] TargetProcessorContainers => _targetProcessorContainers;

        /// <summary>
        /// The skeleton retargeter.
        /// </summary>
        public SkeletonRetargeter SkeletonRetargeter
        {
            get => _skeletonRetargeter;
            set => _skeletonRetargeter = value;
        }

        /// <summary>
        /// The offset transform.
        /// </summary>
        public Transform DebugDrawTransform
        {
            get => _debugDrawTransform;
            set => _debugDrawTransform = value;
        }

        /// <summary>
        /// True if debug draw should be enabled.
        /// </summary>
        public bool DebugDrawSourceSkeleton
        {
            get => _debugDrawSourceSkeleton;
            set => _debugDrawSourceSkeleton = value;
        }

        /// <summary>
        /// True if debug draw should be enabled.
        /// </summary>
        public bool DebugDrawTargetSkeleton
        {
            get => _debugDrawTargetSkeleton;
            set => _debugDrawTargetSkeleton = value;
        }

        /// <summary>
        /// Set to true if the retargeter is valid or not.
        /// </summary>
        public bool RetargeterValid => _isValid && _skeletonRetargeter.IsInitialized;

        /// <summary>
        /// Whether to draw debug visualization for the source skeleton.
        /// </summary>
        [SerializeField]
        protected bool _debugDrawSourceSkeleton;

        /// <summary>
        /// The color to use when drawing the source skeleton debug visualization.
        /// </summary>
        [SerializeField]
        protected Color _debugDrawSourceSkeletonColor = Color.white;

        /// <summary>
        /// Whether to draw debug visualization for the target skeleton.
        /// </summary>
        [SerializeField]
        protected bool _debugDrawTargetSkeleton;

        /// <summary>
        /// The color to use when drawing the target skeleton debug visualization.
        /// </summary>
        [SerializeField]
        protected Color _debugDrawTargetSkeletonColor = Color.green;

        /// <summary>
        /// The color to use when drawing an invalid target skeleton debug visualization.
        /// </summary>
        [SerializeField]
        protected Color _debugDrawInvalidTargetSkeletonColor = Color.red;

        /// <summary>
        /// The color to use when drawing an invalid target skeleton debug visualization.
        /// </summary>
        [SerializeField]
        protected Color _debugDrawInvalidSourceSkeletonColor = Color.magenta;

        /// <summary>
        /// The skeleton retargeter instance used for retargeting operations.
        /// </summary>
        [SerializeField]
        protected SkeletonRetargeter _skeletonRetargeter = new();

        /// <summary>
        /// Array of source processor containers that modify the source skeleton data.
        /// </summary>
        [SerializeField]
        protected SourceProcessorContainer[] _sourceProcessorContainers;

        /// <summary>
        /// Array of target processor containers that modify the target skeleton data.
        /// </summary>
        [SerializeField]
        protected TargetProcessorContainer[] _targetProcessorContainers;

        // Skeleton data provider.
        private ISourceDataProvider _dataProvider;
        private string _currentManifestation;
        private bool _isValid;
        private bool _isCalibrated;

        // Jobs.
        protected Transform _debugDrawTransform;
        private JobHandle _convertPoseJobHandle;

        /// <summary>
        /// Initializes the character retargeter by finding required components and validating configuration.
        /// </summary>
        public virtual void Awake()
        {
            _dataProvider = gameObject.GetComponent<ISourceDataProvider>();
            _debugDrawTransform = transform;
            Assert.IsNotNull(_config, "Must have a reference to a config; none are defined.");
            Assert.IsNotNull(_dataProvider, "Must have a skeleton data provider; none are on this gameObject.");
        }

        /// <summary>
        /// Initializes the character retargeter and sets up retargeting with the provided configuration.
        /// </summary>
        public override void Start()
        {
            base.Start();
            Setup(Config);
        }

        /// <summary>
        /// Updates the character retargeter each frame, processing source poses and applying retargeting.
        /// </summary>
        public virtual void Update()
        {
            var sourcePose = _dataProvider.GetSkeletonPose();
            _currentManifestation = _dataProvider.GetManifestation();
            _isValid = _dataProvider.IsPoseValid();
            if (!_skeletonRetargeter.IsInitialized)
            {
                return;
            }

            if (!_isValid)
            {
                if (_debugDrawSourceSkeleton)
                {
                    _skeletonRetargeter.DrawInvalidSourcePose(_debugDrawInvalidSourceSkeletonColor);
                }

                return;
            }

            // Perform retargeting.
            UpdateSkeletalTPose();
            CalculatePose(sourcePose);
        }

        /// <summary>
        /// Performs late update processing for the character retargeter, applying the final pose to the character.
        /// </summary>
        public virtual void LateUpdate()
        {
            if (!_skeletonRetargeter.IsInitialized)
            {
                return;
            }

            if (!_skeletonRetargeter.AppliedPose)
            {
                if (_debugDrawTargetSkeleton)
                {
                    _skeletonRetargeter.DrawInvalidTargetPose(_debugDrawInvalidTargetSkeletonColor);
                }

                return;
            }

            UpdatePose();
        }

        /// <summary>
        /// Cleans up resources when the character retargeter is destroyed.
        /// </summary>
        public virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Sets up the character retargeter with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration string containing retargeting data.</param>
        public void Setup(string config)
        {
            _skeletonRetargeter.Dispose();
            _skeletonRetargeter.Setup(config);
            _skeletonRetargeter.HipsScale =
                Vector3.Scale(_jointPairs[_skeletonRetargeter.HipsJointIndex].Joint.lossyScale,
                    transform.lossyScale.Reciprocal());
            foreach (var sourceProcessorContainer in _sourceProcessorContainers)
            {
                sourceProcessorContainer?.GetCurrentProcessor()?.Initialize(this);
            }

            foreach (var targetProcessorContainer in _targetProcessorContainers)
            {
                targetProcessorContainer?.GetCurrentProcessor()?.Initialize(this);
            }

            if (_skeletonRetargeter.ApplyRootScale)
            {
                transform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Disposes of resources used by the character retargeter.
        /// </summary>
        public void Dispose()
        {
            _skeletonRetargeter?.Dispose();
            _skeletonRetargeter = null;
            if (_joints.isCreated)
            {
                _joints.Dispose();
            }
        }

        /// <summary>
        /// Calibrate the retargeter to be fixed to the last source T-Pose.
        /// </summary>
        public void Calibrate()
        {
            if (!_isValid)
            {
                return;
            }

            UpdateSkeletalTPose(true);
            _isCalibrated = true;
        }

        /// <summary>
        /// Calculates the retargeted pose from the source pose.
        /// </summary>
        /// <param name="sourcePose">The source pose to retarget.</param>
        public void CalculatePose(NativeArray<NativeTransform> sourcePose)
        {
            // If calibrated, match the pose.
            if (_isCalibrated)
            {
                _skeletonRetargeter.Align(sourcePose);
            }

            // Run processors with source pose and retargeting target pose data.
            foreach (var processor in _sourceProcessorContainers)
            {
                processor.GetCurrentProcessor()?.ProcessSkeleton(sourcePose);
            }

            if (!_skeletonRetargeter.Update(sourcePose, _currentManifestation))
            {
                return;
            }

            foreach (var targetProcessorContainer in _targetProcessorContainers)
            {
                targetProcessorContainer?.GetCurrentProcessor()?.UpdatePose(ref _skeletonRetargeter.RetargetedPose);
            }

            // If root scale isn't being applied, we should still retarget in world space correctly.
            if (!_skeletonRetargeter.ApplyRootScale)
            {
                var pose = _skeletonRetargeter.RetargetedPose[_skeletonRetargeter.RootJointIndex];
                pose.Scale = transform.localScale;
                _skeletonRetargeter.RetargetedPose[_skeletonRetargeter.RootJointIndex] = pose;
            }

            // Create job to convert world pose to local pose.
            var job = new SkeletonJobs.ConvertWorldToLocalPoseJob
            {
                RootJointIndex = _skeletonRetargeter.RootJointIndex,
                HipsJointIndex = _skeletonRetargeter.HipsJointIndex,
                HipsScale = _skeletonRetargeter.HipsScale,
                RetargetingBehavior = _skeletonRetargeter.RetargetingBehavior,
                ParentIndices = _skeletonRetargeter.TargetParentIndices,
                WorldPose = _skeletonRetargeter.RetargetedPose,
                LocalPose = _skeletonRetargeter.RetargetedPoseLocal,
                LocalTPose = _skeletonRetargeter.TargetReferencePoseLocal,
            };
            _convertPoseJobHandle = job.Schedule();

            if (!_skeletonRetargeter.IsInitialized)
            {
                return;
            }

            if (_debugDrawSourceSkeleton)
            {
                _skeletonRetargeter.DrawDebugSourcePose(_debugDrawTransform, _debugDrawSourceSkeletonColor);
            }
        }

        /// <summary>
        /// Updates the character's pose with the retargeted data.
        /// </summary>
        public void UpdatePose()
        {
            _convertPoseJobHandle.Complete();

            // Run processors with current pose and retargeted target pose data if the character is non-zero.
            var currentPose = GetCurrentBodyPose(JointType.NoWorldSpace);
            foreach (var targetProcessorContainer in _targetProcessorContainers)
            {
                targetProcessorContainer?.GetCurrentProcessor()?.LateUpdatePose(
                    ref currentPose,
                    ref _skeletonRetargeter.RetargetedPoseLocal);
            }

            currentPose.Dispose();

            ApplyPose();
            if (_debugDrawTargetSkeleton)
            {
                if (_isValid)
                {
                    _skeletonRetargeter.DrawDebugTargetPose(_debugDrawTransform, _debugDrawTargetSkeletonColor);
                }
                else
                {
                    _skeletonRetargeter.DrawInvalidTargetPose(_debugDrawInvalidTargetSkeletonColor);
                }
            }
        }

        /// <summary>
        /// Updates the T-pose reference used for retargeting.
        /// </summary>
        /// <param name="sourcePose">The source pose to use as reference for the T-pose.</param>
        public void UpdateTPose(NativeArray<NativeTransform> sourcePose)
        {
            UpdateSourceReferenceTPose(_skeletonRetargeter.NativeHandle, sourcePose, _currentManifestation);
            _skeletonRetargeter.UpdateSourceReferencePose(sourcePose, _currentManifestation);
        }

        /// <summary>
        /// Gets a processor of the specified type from the source processor containers.
        /// </summary>
        /// <typeparam name="T">The type of processor to get.</typeparam>
        /// <returns>The processor of the specified type if found, otherwise null.</returns>
        public T GetSourceProcessor<T>() where T : class
        {
            var containers = SourceProcessorContainers;
            foreach (var container in containers)
            {
                if (container?.GetCurrentProcessor() is T processor)
                {
                    return processor;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a processor of the specified type from the target processor containers.
        /// </summary>
        /// <typeparam name="T">The type of processor to get.</typeparam>
        /// <returns>The processor of the specified type if found, otherwise null.</returns>
        public T GetTargetProcessor<T>() where T : class
        {
            var containers = TargetProcessorContainers;
            foreach (var container in containers)
            {
                if (container?.GetCurrentProcessor() is T processor)
                {
                    return processor;
                }
            }

            return null;
        }

        private void UpdateSkeletalTPose(bool forceUpdate = false)
        {
            if (!forceUpdate && (_isCalibrated || !_dataProvider.IsNewTPoseAvailable()))
            {
                return;
            }

            var sourcePose = _dataProvider.GetSkeletonTPose();
            UpdateTPose(sourcePose);
        }

        private void ApplyPose()
        {
            var rootScale = _skeletonRetargeter.RootScale;
            var headScale = _skeletonRetargeter.HeadScale;

            // Apply scale first.
            if (_skeletonRetargeter.ApplyRootScale && transform.localScale != rootScale)
            {
                transform.localScale = rootScale;
            }

            var headJoint = _joints[_skeletonRetargeter.HeadJointIndex];
            if (_skeletonRetargeter.ApplyHeadScale && headJoint.localScale != headScale)
            {
                headJoint.localScale = headScale;
            }

            // Hide the lower body by setting the scale of the leg joints to zero.
            if (_skeletonRetargeter.HideLowerBodyWhenUpperBodyTracking)
            {
                if (_dataProvider != null &&
                    _dataProvider.GetManifestation() == MetaSourceDataProvider.HalfBodyManifestation)
                {
                    // Check only a single joint.
                    if (_joints[_skeletonRetargeter.LeftUpperLegJointIndex].localScale !=
                        _skeletonRetargeter.HideLegScale)
                    {
                        _joints[_skeletonRetargeter.LeftUpperLegJointIndex].localScale =
                            _skeletonRetargeter.HideLegScale;
                        _joints[_skeletonRetargeter.RightUpperLegJointIndex].localScale =
                            _skeletonRetargeter.HideLegScale;
                        _joints[_skeletonRetargeter.LeftLowerLegJointIndex].localScale = Vector3.zero;
                        _joints[_skeletonRetargeter.RightLowerLegJointIndex].localScale = Vector3.zero;
                    }
                }
                else if (_joints[_skeletonRetargeter.LeftUpperLegJointIndex].localScale ==
                         _skeletonRetargeter.HideLegScale)
                {
                    _joints[_skeletonRetargeter.LeftUpperLegJointIndex].localScale = Vector3.one;
                    _joints[_skeletonRetargeter.RightUpperLegJointIndex].localScale = Vector3.one;
                    _joints[_skeletonRetargeter.LeftLowerLegJointIndex].localScale = Vector3.one;
                    _joints[_skeletonRetargeter.RightLowerLegJointIndex].localScale = Vector3.one;
                }
            }

            _skeletonRetargeter.ApplyPose(ref _joints);
        }
    }
}
