// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for retargeting data.
    /// </summary>
    public interface IRetargetingData
    {
        /// <summary>
        /// Used to create job information in case it becomes
        /// allocated before the constraint has a chance to run.
        /// </summary>
        public Transform DummyTransform { get; }

        /// <summary>
        /// Source transforms used for retargeting.
        /// </summary>
        public Transform[] SourceTransforms { get; }

        /// <summary>
        /// Target transforms affected by retargeting.
        /// </summary>
        public Transform[] TargetTransforms { get; }

        /// <summary>
        /// Indicates if target transform's position should be updated.
        /// Once a position is updated, the original position will be lost.
        /// </summary>
        public bool[] ShouldUpdatePosition { get; }

        /// <summary>
        /// Rotation offset to be applied during retargeting.
        /// </summary>
        public Quaternion[] RotationOffsets { get; }

        /// <summary>
        /// Optional rotational adjustment to be applied during retargeting.
        /// </summary>
        public Quaternion[] RotationAdjustments { get; }

        /// <summary>
        /// Allows updating any dynamic data at runtime.
        /// </summary>
        public void UpdateDynamicMetadata();

        /// <summary>
        /// Indicates if data has initialized or not.
        /// </summary>
        /// <returns>True if data has initialized, false if not.</returns>
        public bool HasDataInitialized();
    }

    /// <summary>
    /// Retargeting data used by the constraint.
    /// Implements the retargeting interface.
    /// </summary>
    [System.Serializable]
    public struct RetargetingConstraintData : IAnimationJobData, IRetargetingData
    {
        /// <summary>
        /// The OVRSkeleton component.
        /// </summary>
        public OVRSkeleton Skeleton => _retargetingLayer;

        // Interface implementation
        /// <inheritdoc />
        public Transform DummyTransform => _retargetingLayer.transform;

        /// <inheritdoc />
        Transform[] IRetargetingData.SourceTransforms => _sourceTransforms;

        /// <inheritdoc />
        Transform[] IRetargetingData.TargetTransforms => _targetTransforms;

        /// <inheritdoc />
        bool[] IRetargetingData.ShouldUpdatePosition => _shouldUpdatePositions;

        /// <inheritdoc />
        Quaternion[] IRetargetingData.RotationOffsets => _rotationOffsets;

        /// <inheritdoc />
        Quaternion[] IRetargetingData.RotationAdjustments => _rotationAdjustments;

        /// <summary>
        /// Retargeting layer component to get data from.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingConstraintDataTooltips.RetargetingLayer)]
        private RetargetingLayer _retargetingLayer;

        /// <inheritdoc cref="_retargetingLayer"/>
        public RetargetingLayer RetargetingLayerComp
        {
            get { return _retargetingLayer; }
            set { _retargetingLayer = value; }
        }

        /// <summary>
        /// Allow dynamic adjustments at runtime.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingConstraintDataTooltips.AllowDynamicAdjustmentsRuntime)]
        private bool _allowDynamicAdjustmentsRuntime;

        /// <inheritdoc cref="_allowDynamicAdjustmentsRuntime"/>
        public bool AllowDynamicAdjustmentsRuntime
        {
            get { return _allowDynamicAdjustmentsRuntime; }
            set { _allowDynamicAdjustmentsRuntime = value; }
        }

        /// <summary>
        /// Avatar mask to restrict retargeting. While the humanoid retargeter
        /// class has similar fields, this one is easier to use.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingConstraintDataTooltips.AvatarMask)]
        private AvatarMask _avatarMask;

        /// <inheritdoc cref="IRetargetingData.SourceTransforms"/>
        [SyncSceneToStream]
        [Tooltip(RetargetingConstraintDataTooltips.SourceTransforms)]
        private Transform[] _sourceTransforms;

        /// <inheritdoc cref="IRetargetingData.TargetTransforms"/>
        [SyncSceneToStream]
        [Tooltip(RetargetingConstraintDataTooltips.TargetTransforms)]
        private Transform[] _targetTransforms;

        /// <inheritdoc cref="IRetargetingData.ShouldUpdatePosition"/>
        [NotKeyable]
        [Tooltip(RetargetingConstraintDataTooltips.ShouldUpdatePositions)]
        private bool[] _shouldUpdatePositions;

        /// <inheritdoc cref="IRetargetingData.RotationOffsets"/>
        [NotKeyable]
        [Tooltip(RetargetingConstraintDataTooltips.RotationOffsets)]
        private Quaternion[] _rotationOffsets;

        /// <inheritdoc cref="IRetargetingData.RotationAdjustments"/>
        [NotKeyable]
        [Tooltip(RetargetingConstraintDataTooltips.RotationAdjustments)]
        private Quaternion[] _rotationAdjustments;

        private bool _hasInitialized;

        /// <inheritdoc />
        public bool HasDataInitialized()
        {
            return _hasInitialized;
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            return _retargetingLayer != null;
        }

        /// <inheritdoc />
        public void SetDefaultValues()
        {
            _retargetingLayer = null;
            _allowDynamicAdjustmentsRuntime = true;
            _avatarMask = new AvatarMask();
            foreach (AvatarMaskBodyPart part in (AvatarMaskBodyPart[])Enum.GetValues(typeof(AvatarMaskBodyPart)))
            {
                if (part == AvatarMaskBodyPart.LastBodyPart)
                {
                    continue;
                }
                _avatarMask.SetHumanoidBodyPartActive(part, true);
            }
        }

        /// <summary>
        /// Set up all job data.
        /// </summary>
        /// <param name="dummySourceObject">Fallback source object if skeleton is not ready.</param>
        /// <param name="dummyTargetObject">Fallback target object if skeleton is not ready.</param>
        public void SetUp(GameObject dummySourceObject, GameObject dummyTargetObject)
        {
            BuildArraysForJob(dummySourceObject, dummyTargetObject);
            UpdateDataArraysWithAdjustments();
            _hasInitialized = true;
        }

        /// <summary>
        /// Update dynamic data, can be useful if user changes it at runtime.
        /// </summary>
        public void UpdateDynamicMetadata()
        {
            if (!_allowDynamicAdjustmentsRuntime)
            {
                return;
            }
            UpdateDataArraysWithAdjustments();
        }

        private void BuildArraysForJob(GameObject dummySourceObject, GameObject dummyTargetObject)
        {
            if (IsSourceSkeletonNotInitialized())
            {
                CreateDummyData(dummySourceObject, dummyTargetObject);
                Debug.LogWarning("Skeleton not initialized so creating dummy data for retargeting.");
                return;
            }

            Debug.LogWarning("Build arrays for retargeting job.");
            List<Transform> sourceTransforms = new List<Transform>();
            List<Transform> targetTransforms = new List<Transform>();

            List<bool> shouldUpdatePositions = new List<bool>();
            List<Quaternion> rotationOffsets = new List<Quaternion>();

            List<Quaternion> rotationAdjustments = new List<Quaternion>();

            _retargetingLayer.FillTransformArrays(
                sourceTransforms, targetTransforms,
                shouldUpdatePositions, rotationOffsets,
                rotationAdjustments, _avatarMask);

            _sourceTransforms = sourceTransforms.ToArray();
            _targetTransforms = targetTransforms.ToArray();
            _shouldUpdatePositions = shouldUpdatePositions.ToArray();
            _rotationOffsets = rotationOffsets.ToArray();
            _rotationAdjustments = rotationAdjustments.ToArray();

            if (_rotationOffsets.Length == 0)
            {
                Debug.LogWarning("No valid transforms available for job. Perhaps the source " +
                    "skeleton metadata is not available yet.");
            }
        }

        private void UpdateDataArraysWithAdjustments()
        {
            if (IsSourceSkeletonNotInitialized())
            {
                return;
            }

            // if data isn't available yet, then bail.
            if (_rotationAdjustments.Length <= 1)
            {
                return;
            }

            _retargetingLayer.UpdateAdjustments(_rotationOffsets,
                _shouldUpdatePositions, _rotationAdjustments,
                _avatarMask);
        }

        private bool IsSourceSkeletonNotInitialized()
        {
            return (!_retargetingLayer.IsInitialized ||
                _retargetingLayer.BindPoses == null ||
                _retargetingLayer.BindPoses.Count == 0);
        }

        /// <summary>
        /// Fill in with dummy data to make sure animation system doesn't freak out.
        /// This can happen if this constraint is enabled and the source skeleton
        /// is not ready yet.
        /// </summary>
        private void CreateDummyData(GameObject dummySourceObject, GameObject dummyTargetObject)
        {
            _sourceTransforms = new Transform[1];
            _sourceTransforms[0] = dummySourceObject.transform;

            _targetTransforms = new Transform[1];
            _targetTransforms[0] = dummyTargetObject.transform;
            _shouldUpdatePositions = new bool[1];
            _rotationOffsets = new Quaternion[1];
            _rotationOffsets[0] = Quaternion.identity;
            _rotationAdjustments = new Quaternion[1];
            _rotationAdjustments[0] = Quaternion.identity;
            _hasInitialized = false;
        }
    }

    /// <summary>
    /// Retargeting constraint. Keep game object disabled until
    /// RegenerateData is called.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Retargeting Constraint")]
    public class RetargetingAnimationConstraint : RigConstraint<
        RetargetingAnimationJob,
        RetargetingConstraintData,
        RetargetingAnimationJobBinder<RetargetingConstraintData>>,
        IOVRSkeletonConstraint
    {
        private GameObject _dummySource, _dummyTarget;

        /// <summary>
        /// Retargeting layer accessors.
        /// </summary>
        public RetargetingLayer RetargetingLayerComp
        {
            get { return m_Data.RetargetingLayerComp; }
            set { m_Data.RetargetingLayerComp = value; }
        }

        private void Awake()
        {
            _dummySource = new GameObject("Retargeting Constraint Dummy Source");
            _dummyTarget = new GameObject("Retargeting Constraint Dummy Target");
            _dummySource.transform.SetParent(this.transform);
            _dummyTarget.transform.SetParent(this.transform);

            data.SetUp(_dummySource, _dummyTarget);
        }

        private void Update()
        {
            data.UpdateDynamicMetadata();
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
            gameObject.SetActive(true);
            data.SetUp(_dummySource, _dummyTarget);
            Debug.LogWarning("Generated new constraint data.");
        }
    }
}
