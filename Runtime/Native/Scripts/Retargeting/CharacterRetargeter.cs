// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using static OVRSkeleton;
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
        public bool IsValid => _isValid;

        /// <summary>
        /// If retargeted data was applied to the character.
        /// </summary>
        public bool HasValidRetargetedData => _skeletonRetargeter.IsValid;

        /// <summary>
        /// Retargeting handle.
        /// </summary>
        public ulong RetargetingHandle => _skeletonRetargeter?.NativeHandle ?? 0;

        /// <summary>
        /// The data provider.
        /// </summary>
        public IOVRSkeletonDataProvider DataProvider => _dataProvider;

        /// <summary>
        /// Source processor containers.
        /// </summary>
        public SourceProcessorContainer[] SourceProcessorContainers => _sourceProcessorContainers;


        /// <summary>
        /// Source processor containers.
        /// </summary>
        public TargetProcessorContainer[] TargetProcessorContainers => _targetProcessorContainers;

        /// <summary>
        /// The retargeting.
        /// </summary>
        public SkeletonRetargeter Retargeting
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
        /// Returns the last source poses cached, after they have been
        /// affected by any processors enabled.
        /// </summary>
        public NativeTransform[] LastSourcePosesCached => _lastSourcePosesCached;

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
        /// The skeleton retargeter instance used for retargeting operations.
        /// </summary>
        [SerializeField]
        protected SkeletonRetargeter _skeletonRetargeter = new();

        /// <summary>
        /// The delay in seconds before body tracking data is considered valid for retargeting.
        /// </summary>
        [SerializeField]
        protected float _validBodyTrackingDelay = 0.25f;

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

        // OVRSkeleton.
        private IOVRSkeletonDataProvider _dataProvider;
        private OVRPlugin.Skeleton2 _skeleton = new();
        private int _skeletalChangedCount = -1;
        private int _currentSkeletalChangeCount = -1;
        private float _currentValidBodyTrackingTime;
        private bool _isValid;

        // Jobs.
        protected Transform _debugDrawTransform;
        private JobHandle _convertPoseJobHandle;
        private JobHandle _applyPoseJobHandle;

        private NativeTransform[] _lastSourcePosesCached;

        /// <summary>
        /// Initializes the character retargeter by finding required components and validating configuration.
        /// </summary>
        public virtual void Awake()
        {
            _dataProvider = gameObject.GetComponent<IOVRSkeletonDataProvider>();
            _debugDrawTransform = transform;
            Assert.IsNotNull(_config, "Must have a reference to a config; none are defined.");
            Assert.IsNotNull(_dataProvider);
            Assert.IsTrue(_dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.Body ||
                          _dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody,
                "Data provider must be configured to use body tracking.");
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
            var sourcePose = SkeletonUtilities.GetPosesFromTheTracker(
                _skeletonRetargeter.SourceJointCount, _dataProvider, Pose.identity,
                out _currentSkeletalChangeCount,
                out _isValid);
            if (!_isValid || !_skeletonRetargeter.IsValid)
            {
                return;
            }

            // Wait some time for the body tracking data to be accurate before retargeting.
            if (_currentValidBodyTrackingTime < _validBodyTrackingDelay)
            {
                _currentValidBodyTrackingTime += Time.deltaTime;
                return;
            }

            // Perform retargeting.
            UpdateSkeletalTPose();
            CalculatePose(sourcePose);

            int posesTotal = sourcePose.Length;
            if (_lastSourcePosesCached == null || _lastSourcePosesCached.Length != sourcePose.Length)
            {
                _lastSourcePosesCached = new NativeTransform[posesTotal];
            }
            for (int i = 0; i < posesTotal; i++)
            {
                _lastSourcePosesCached[i] = sourcePose[i];
            }
        }

        /// <summary>
        /// Performs late update processing for the character retargeter, applying the final pose to the character.
        /// </summary>
        public virtual void LateUpdate()
        {
            if (!_skeletonRetargeter.IsValid || !_skeletonRetargeter.AppliedPose)
            {
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
        /// Returns the root scale.
        /// </summary>
        /// <returns>Root scale.</returns>
        public Vector3 RootScale()
        {
            if (_skeletonRetargeter == null || !_skeletonRetargeter.RetargetedPose.IsCreated ||
                !_skeletonRetargeter.ApplyScale)
            {
                return Vector3.one;
            }

            var rootJointIndex = _skeletonRetargeter.RootJointIndex;
            var rootScale = _skeletonRetargeter.RetargetedPose[rootJointIndex].Scale;
            return rootScale;
        }

        /// <summary>
        /// Sets up the character retargeter with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration string containing retargeting data.</param>
        public void Setup(string config)
        {
            _skeletonRetargeter.Dispose();
            _skeletonRetargeter.Setup(config);
            foreach (var sourceProcessorContainer in _sourceProcessorContainers)
            {
                sourceProcessorContainer?.GetCurrentProcessor()?.Initialize(this);
            }

            foreach (var targetProcessorContainer in _targetProcessorContainers)
            {
                targetProcessorContainer?.GetCurrentProcessor()?.Initialize(this);
            }

            if (_skeletonRetargeter.ApplyScale)
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
            _skeletonRetargeter = null;
            if (_joints.isCreated)
            {
                _joints.Dispose();
            }
        }

        /// <summary>
        /// Calculates the retargeted pose from the source pose.
        /// </summary>
        /// <param name="sourcePose">The source pose to retarget.</param>
        public void CalculatePose(NativeArray<NativeTransform> sourcePose)
        {
            // Run processors with source pose and retargeting target pose data.
            foreach (var processor in _sourceProcessorContainers)
            {
                processor.GetCurrentProcessor()?.ProcessSkeleton(sourcePose);
            }

            _skeletonRetargeter.Update(sourcePose);
            foreach (var targetProcessorContainer in _targetProcessorContainers)
            {
                targetProcessorContainer?.GetCurrentProcessor()?.UpdatePose(ref _skeletonRetargeter.RetargetedPose);
            }

            // Create job to convert world pose to local pose.
            var job = new SkeletonJobs.ConvertWorldToLocalPoseJob
            {
                RootJointIndex = _skeletonRetargeter.RootJointIndex,
                ParentIndices = _skeletonRetargeter.NativeTargetParentIndices,
                WorldPose = _skeletonRetargeter.RetargetedPose,
                LocalPose = _skeletonRetargeter.RetargetedPoseLocal
            };
            _convertPoseJobHandle = job.Schedule();

            if (!_skeletonRetargeter.IsValid)
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
                targetProcessorContainer?.GetCurrentProcessor()
                    ?.LateUpdatePose(ref currentPose, ref _skeletonRetargeter.RetargetedPoseLocal);
            }
            currentPose.Dispose();

            if (!_skeletonRetargeter.IsValid)
            {
                return;
            }

            ApplyPose();
            if (_debugDrawTargetSkeleton)
            {
                _skeletonRetargeter.DrawDebugTargetPose(_debugDrawTransform, _debugDrawTargetSkeletonColor);
            }
        }

        /// <summary>
        /// Updates the T-pose reference used for retargeting.
        /// </summary>
        /// <param name="sourcePose">The source pose to use as reference for the T-pose.</param>
        public void UpdateTPose(NativeArray<NativeTransform> sourcePose)
        {
            UpdateSourceReferenceTPose(_skeletonRetargeter.NativeHandle, sourcePose);
            _skeletonRetargeter.UpdateScale(sourcePose);
        }

        private void UpdateSkeletalTPose()
        {
            if (_currentSkeletalChangeCount == _skeletalChangedCount)
            {
                return;
            }

            var sourcePose = SkeletonUtilities.GetBindPoses(_skeletonRetargeter.SourceJointCount, ref _skeleton);
            _skeletalChangedCount = _currentSkeletalChangeCount;
            UpdateTPose(sourcePose);
        }

        private void ApplyPose()
        {
            // Apply scale first.
            var rootJointIndex = _skeletonRetargeter.RootJointIndex;
            var headJointIndex = _skeletonRetargeter.HeadJointIndex;
            var rootScale = _skeletonRetargeter.RetargetedPose[rootJointIndex].Scale;
            var headScale = _skeletonRetargeter.RetargetedPose[headJointIndex].Scale;
            if (_skeletonRetargeter.ApplyScale && transform.localScale != rootScale)
            {
                transform.localScale = rootScale;
                _joints[headJointIndex].localScale = headScale;
            }

            // Create job to apply the pose.
            var job = new SkeletonJobs.ApplyPoseJob
            {
                BodyPose = _skeletonRetargeter.RetargetedPoseLocal
            };
            _applyPoseJobHandle = job.Schedule(_joints);
            _applyPoseJobHandle.Complete();
        }
    }
}
