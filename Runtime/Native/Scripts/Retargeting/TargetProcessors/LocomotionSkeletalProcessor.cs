// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
#endif
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    [System.Serializable]
    public class LocomotionSkeletalProcessor : TargetProcessor
    {
#if ISDK_DEFINED
        /// <summary>
        /// Gets or sets the ILocomotionEventHandler component used for locomotion events.
        /// </summary>
        public ILocomotionEventHandler LocomotionEventHandler
        {
            get => _locomotionEventHandler;
            set => _locomotionEventHandler = value;
        }
#endif

        /// <summary>
        /// Gets or sets the Transform of the camera rig that the character follows.
        /// </summary>
        public Transform CameraRig
        {
            get => _cameraRig;
            set => _cameraRig = value;
        }

        /// <summary>
        /// Gets or sets the Animator component used for controlling character animations.
        /// </summary>
        public Animator Animator
        {
            get => _animator;
            set => _animator = value;
        }

        public string AnimatorVerticalParam
        {
            get => _animatorVerticalParam;
            set => _animatorVerticalParam = value;
        }

        public string AnimatorHorizontalParam
        {
            get => _animatorVerticalParam;
            set => _animatorVerticalParam = value;
        }

        public float AnimationSpeed
        {
            get => _animationSpeed;
            set => _animationSpeed = value;
        }

#if ISDK_DEFINED
        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        private Object _locomotionEventHandlerObject;
        private ILocomotionEventHandler _locomotionEventHandler;
#endif

        [SerializeField]
        private Transform _cameraRig;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private string _animatorVerticalParam;

        [SerializeField]
        private string _animatorHorizontalParam;

        [SerializeField]
        private float _animationSpeed;

        private static int _verticalHash;
        private static int _horizontalHash;
        private AnimationSkeletalProcessor _animProcessor;
        private Transform _characterRetargeter;
        private Vector3 _velocity;
        private Quaternion _rotationalVelocity;

        /// <summary>
        /// Initializes the locomotion processor by setting up event handlers and finding required components.
        /// </summary>
        /// <param name="retargeter">The character retargeter that owns this processor.</param>
        public override void Initialize(CharacterRetargeter retargeter)
        {
#if ISDK_DEFINED
            if (_locomotionEventHandlerObject != null)
            {
                _locomotionEventHandler = (ILocomotionEventHandler)_locomotionEventHandlerObject;
                _locomotionEventHandler.WhenLocomotionEventHandled += OnWhenLocomotionEventHandled;
            }
#endif
            _characterRetargeter = retargeter.transform;
            if (_animator == null)
            {
                return;
            }
            _verticalHash = Animator.StringToHash(_animatorVerticalParam);
            _horizontalHash = Animator.StringToHash(_animatorHorizontalParam);
            _animProcessor = retargeter.GetTargetProcessor<AnimationSkeletalProcessor>();
        }

        /// <summary>
        /// Cleans up resources and unsubscribes from events when the processor is destroyed.
        /// </summary>
        public override void Destroy()
        {
#if ISDK_DEFINED
            if (_locomotionEventHandler != null)
            {
                _locomotionEventHandler.WhenLocomotionEventHandled -= OnWhenLocomotionEventHandled;
            }
#endif
        }

        /// <summary>
        /// Updates the character's position, rotation, and animation parameters based on locomotion input.
        /// </summary>
        /// <param name="pose">The pose to be updated.</param>
        public override void UpdatePose(ref NativeArray<MSDKUtility.NativeTransform> pose)
        {
            if (_weight <= 0.0f)
            {
                return;
            }

            var targetPos = _cameraRig.position;
            var targetRot = _cameraRig.rotation;
            _characterRetargeter.rotation = targetRot;
            _characterRetargeter.position = targetPos;

            _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, _animationSpeed * Time.deltaTime);
            _rotationalVelocity =
                Quaternion.RotateTowards(_rotationalVelocity, Quaternion.identity,
                    360f * _animationSpeed * Time.deltaTime);

            if (_animProcessor == null || _animator == null)
            {
                return;
            }
            var animationWeight = _animProcessor.Weight;
            if (!_animator.enabled && animationWeight > 0.0f)
            {
                _animator.enabled = true;
            }
            if (_animator.enabled && animationWeight <= 0.0f)
            {
                _animator.enabled = false;
            }
            _animator.SetFloat(_horizontalHash, _velocity.x * _animationSpeed);
            _animator.SetFloat(_verticalHash, _velocity.z * _animationSpeed);
            _animProcessor.Weight = _velocity.sqrMagnitude > 0.0f
                ? Mathf.MoveTowards(animationWeight, 1.0f, Time.deltaTime * _animationSpeed)
                : Mathf.MoveTowards(animationWeight, 0.0f, Time.deltaTime * _animationSpeed);
        }

        /// <summary>
        /// Performs any late update processing on the pose after the main update.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be updated.</param>
        public override void LateUpdatePose(ref NativeArray<MSDKUtility.NativeTransform> currentPose,
            ref NativeArray<MSDKUtility.NativeTransform> targetPose)
        {
        }

#if ISDK_DEFINED
        private void OnWhenLocomotionEventHandled(LocomotionEvent locomotionEvent, Pose pose)
        {
            _velocity = locomotionEvent.Translation != LocomotionEvent.TranslationType.Absolute
                ? _characterRetargeter.InverseTransformDirection(pose.position)
                : Vector3.zero;
            _rotationalVelocity = locomotionEvent.Rotation != LocomotionEvent.RotationType.Absolute
                ? pose.rotation
                : Quaternion.identity;
        }
#endif
    }
}
