// Copyright (c) Meta Platforms, Inc. and affiliates.

#if INTERACTION_OVR_DEFINED
using Oculus.Interaction.Locomotion;
#endif
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Retargeting
{
    [System.Serializable]
    public class LocomotionSkeletalProcessor : TargetProcessor
    {
#if INTERACTION_OVR_DEFINED
        /// <summary>
        /// Gets or sets the FirstPersonLocomotor component used for locomotion events.
        /// </summary>
        public FirstPersonLocomotor FirstPersonLocomotor
        {
            get => _firstPersonLocomotor;
            set => _firstPersonLocomotor = value;
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

#if INTERACTION_OVR_DEFINED
        [SerializeField]
        private FirstPersonLocomotor _firstPersonLocomotor;
#endif

        [SerializeField]
        private Transform _cameraRig;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private string _animatorVerticalParam = "Vertical";

        [SerializeField]
        private string _animatorHorizontalParam = "Horizontal";

        [SerializeField]
        private float _animationSpeed = 2.0f;

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
#if INTERACTION_OVR_DEFINED
            Assert.IsNotNull(_firstPersonLocomotor);
            _firstPersonLocomotor.WhenLocomotionEventHandled += OnWhenLocomotionEventHandled;
#endif
            _verticalHash = Animator.StringToHash(_animatorVerticalParam);
            _horizontalHash = Animator.StringToHash(_animatorHorizontalParam);
            _characterRetargeter = retargeter.transform;
            foreach (var processor in retargeter.TargetProcessorContainers)
            {
                if (processor.GetCurrentProcessor() is AnimationSkeletalProcessor animProcessor)
                {
                    _animProcessor = animProcessor;
                }
            }
        }

        /// <summary>
        /// Cleans up resources and unsubscribes from events when the processor is destroyed.
        /// </summary>
        public override void Destroy()
        {
#if INTERACTION_OVR_DEFINED
            _firstPersonLocomotor.WhenLocomotionEventHandled -= OnWhenLocomotionEventHandled;
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

#if INTERACTION_OVR_DEFINED
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
