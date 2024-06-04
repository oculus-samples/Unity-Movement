// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using Oculus.Movement.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Animations;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for playback animation data.
    /// </summary>
    public interface IPlaybackAnimationData
    {
        /// <summary>
        /// The animation playback type.
        /// </summary>
        public int PlaybackType { get; }

        /// <summary>
        /// The capture animation constraint to source animation data from.
        /// </summary>
        public CaptureAnimationConstraint SourceConstraint { get; }

        /// <summary>
        /// The avatar mask for masking the animation.
        /// </summary>
        public AvatarMask AnimationMask { get; }

        /// <summary>
        /// Bones array mask.
        /// </summary>
        public HumanBodyBones[] BonesArrayMask { get; }

        /// <summary>
        /// The animation playback type int property.
        /// </summary>
        public string PlaybackTypeIntProperty { get; }

        /// <summary>
        /// Affect positions via the animation.
        /// </summary>
        public string AffectPositionsBoolProperty { get; }

        /// <summary>
        /// Affect rotation via the animation.
        /// </summary>
        public string AffectRotationsBoolProperty { get; }

        /// <summary>
        /// Fixed hips position.
        /// </summary>
        public Vector3 FixedHipsPosition { get; }

        /// <summary>
        /// Fixed hips rotation (euler angles).
        /// </summary>
        public Vector3 FixedHipsRotationEuler { get; }

        /// <summary>
        /// Whether to use the fixed hips pose or not.
        /// </summary>
        public string UseFixedHipsPoseProperty { get; }

        /// <summary>
        /// Whether to affect hips position's Y value.
        /// </summary>
        public string AffectHipsPositionPropertyY { get; }

        /// <summary>
        /// Whether to affect hips rotation X.
        /// </summary>
        public string AffectHipsRotationPropertyX { get; }

        /// <summary>
        /// Whether to affect hips rotation Y.
        /// </summary>
        public string AffectHipsRotationPropertyY { get; }

        /// <summary>
        /// Whether to affect hips rotation Z.
        /// </summary>
        public string AffectHipsRotationPropertyZ { get; }
    }

    /// <summary>
    /// Data to handle animation playback.
    /// </summary>
    [Serializable]
    public struct PlaybackAnimationData : IAnimationJobData, IPlaybackAnimationData
    {
        /// <summary>
        /// The animation playback type.
        /// </summary>
        public enum AnimationPlaybackType
        {
            /// <summary>No animation is played back.</summary>
            None = 0,
            /// <summary>Animation is played back additively.</summary>
            Additive = 1,
            /// <summary>Animation is played back overriding any previous bone updates.</summary>
            Override = 2,
        }

        /// <inheritdoc />
        int IPlaybackAnimationData.PlaybackType => _animationPlaybackType;

        /// <inheritdoc />
        CaptureAnimationConstraint IPlaybackAnimationData.SourceConstraint => _captureAnimationConstraint;

        /// <inheritdoc />
        AvatarMask IPlaybackAnimationData.AnimationMask => AvatarMaskComp;

        /// <inheritdoc />
        string IPlaybackAnimationData.PlaybackTypeIntProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_animationPlaybackType));

        /// <inheritdoc />
        string IPlaybackAnimationData.AffectPositionsBoolProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectPositions));

        /// <inheritdoc />
        string IPlaybackAnimationData.AffectRotationsBoolProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectRotations));

        /// <inheritdoc />
        HumanBodyBones[] IPlaybackAnimationData.BonesArrayMask => _bonesArrayMask;

        /// <inheritdoc />
        Vector3 IPlaybackAnimationData.FixedHipsPosition => _fixedHipsPosition;

        /// <inheritdoc />
        Vector3 IPlaybackAnimationData.FixedHipsRotationEuler => _fixedHipRotation;

        /// <inheritdoc />
        string IPlaybackAnimationData.UseFixedHipsPoseProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_useFixedHipsPose));

        /// <inheritdoc />
        string IPlaybackAnimationData.AffectHipsPositionPropertyY =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectHipsPositionY));

        /// <inheritdoc />
        string IPlaybackAnimationData.AffectHipsRotationPropertyX =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectHipsRotationX));
        /// <inheritdoc />
        string IPlaybackAnimationData.AffectHipsRotationPropertyY =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectHipsRotationY));
        /// <inheritdoc />
        string IPlaybackAnimationData.AffectHipsRotationPropertyZ =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectHipsRotationZ));

        /// <summary>
        /// Avatar mask instance accessor.
        /// </summary>
        public AvatarMask AvatarMaskComp
        {
            get => _avatarMaskInst;
            set => _avatarMaskInst = value;
        }

        /// <inheritdoc cref="IPlaybackAnimationData.PlaybackType"/>
        [SyncSceneToStream, SerializeField, IntAsEnum(typeof(AnimationPlaybackType))]
        [Tooltip(PlaybackAnimationDataTooltips.AnimationPlaybackType)]
        private int _animationPlaybackType;

        /// <inheritdoc cref="IPlaybackAnimationData.SourceConstraint"/>
        [SerializeField]
        [Tooltip(PlaybackAnimationDataTooltips.SourceConstraint)]
        private CaptureAnimationConstraint _captureAnimationConstraint;

        /// <summary>
        /// The optional avatar mask to mask out parts of the animation to be played.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(PlaybackAnimationDataTooltips.AvatarMask)]
        private AvatarMask _avatarMask;

        /// <summary>
        /// Affect positions via the animation.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(PlaybackAnimationDataTooltips.AffectPositions)]
        private bool _affectPositions;

        /// <summary>
        /// Affect rotations via the animation.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(PlaybackAnimationDataTooltips.AffectRotations)]
        private bool _affectRotations;

        /// <summary>
        /// Bones to mask by array.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(PlaybackAnimationDataTooltips.BonesArrayMask)]
        private HumanBodyBones[] _bonesArrayMask;

        /// <summary>
        /// Allows setting hips to a fixed position.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.FixedHipsPosition)]
        [SyncSceneToStream, SerializeField]
        private Vector3 _fixedHipsPosition;

        /// <summary>
        /// Allows setting hips to a fixed rotation (angles).
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.FixedHipsRotation)]
        [SyncSceneToStream, SerializeField]
        private Vector3 _fixedHipRotation;

        /// <summary>
        /// Used fixed hips pose or not.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.UsedFixedHipsPose)]
        [SyncSceneToStream, SerializeField]
        private bool _useFixedHipsPose;

        /// <summary>
        /// Affect hips position Y-value.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.AffectHipsPositionY)]
        [SyncSceneToStream, SerializeField]
        private bool _affectHipsPositionY;

        /// <summary>
        /// Affect hips rotation X value.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.AffectHipsRotationX)]
        [SyncSceneToStream, SerializeField]
        private bool _affectHipsRotationX;

        /// <summary>
        /// Affect hips rotation Y value.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.AffectHipsRotationY)]
        [SyncSceneToStream, SerializeField]
        private bool _affectHipsRotationY;

        /// <summary>
        /// Affect hips rotation Z value.
        /// </summary>
        [Tooltip(PlaybackAnimationDataTooltips.AffectHipsRotationZ)]
        [SyncSceneToStream, SerializeField]
        private bool _affectHipsRotationZ;

        /// <summary>
        /// Don't allow changing the original field directly, as that
        /// has a side-effect of modifying the original mask object.
        /// </summary>
        private AvatarMask _avatarMaskInst;

        /// <summary>
        /// Initializes mask instances based on what value is set
        /// in the corresponding fields.
        /// </summary>
        public void CreateAvatarMaskInstances()
        {
            if (_avatarMask != null)
            {
                _avatarMaskInst = new AvatarMask();
                _avatarMaskInst.CopyOtherMaskBodyActiveValues(
                    _avatarMask);
            }
            else
            {
                _avatarMaskInst = null;
            }
            _avatarMask = _avatarMaskInst;
        }

        bool IAnimationJobData.IsValid()
        {
            if (_captureAnimationConstraint == null)
            {
                return false;
            }
            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _captureAnimationConstraint = null;
            _avatarMask = null;
            _avatarMaskInst = null;
            _affectPositions = true;
            _affectRotations = true;
            _bonesArrayMask = new HumanBodyBones[0];

            _fixedHipsPosition = Vector3.zero;
            _fixedHipRotation = Vector3.zero;
            _useFixedHipsPose = false;
            _affectHipsPositionY = false;
            _affectHipsRotationX = false;
            _affectHipsRotationY = false;
            _affectHipsRotationZ = false;
        }
    }

    /// <summary>
    /// Playback animation constraint. Uses captured animation data to playback the current animator pose
    /// additively or override.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Playback Animation Constraint")]
    public class PlaybackAnimationConstraint : RigConstraint<
            PlaybackAnimationJob,
            PlaybackAnimationData,
            PlaybackAnimationJobBinder<PlaybackAnimationData>>,
            IOVRSkeletonConstraint
    {
        private void Awake()
        {
            data.CreateAvatarMaskInstances();
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
