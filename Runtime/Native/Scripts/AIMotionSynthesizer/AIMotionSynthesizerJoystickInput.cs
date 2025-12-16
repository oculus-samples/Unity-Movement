// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;
#if USE_UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.XR.Movement.AI
{
    /// <summary>
    /// Input system mode for <see cref="AIMotionSynthesizerJoystickInput"/>.
    /// </summary>
    public enum InputMode
    {
        /// <summary>Use OVRInput for controller input.</summary>
        OVRInput,

        /// <summary>Use Unity's new Input System.</summary>
        UnityInputSystem,

        /// <summary>Use both systems, preferring whichever has larger input magnitude.</summary>
        Both
    }

    /// <summary>
    /// Joystick input provider for AI Motion Synthesizer.
    /// Supports OVRInput, Unity Input System, or both simultaneously.
    /// </summary>
    public class AIMotionSynthesizerJoystickInput : MonoBehaviour, IAIMotionSynthesizerInputProvider
    {
        /// <inheritdoc/>
        public Vector3 GetVelocity() => _currentVelocity;

        /// <inheritdoc/>
        public Vector3 GetDirection() => _currentDirection;

        /// <inheritdoc/>
        public bool IsInputActive() => _inputActive;

        /// <inheritdoc/>
        public Transform GetReferenceTransform() => _referenceTransform;

        [SerializeField]
        [Tooltip("Select which input system to use")]
        private InputMode _inputMode = InputMode.OVRInput;

        [SerializeField]
        [Tooltip("Reference transform for input calculations. Determines coordinate space for velocity/direction. Defaults to camera rig if not set.")]
        private Transform _referenceTransform;

        private OVRCameraRig _cameraRig;

#if USE_UNITY_INPUT_SYSTEM
        [SerializeField]
        [Tooltip("Move action (left stick) using Unity's Input System")]
        private InputActionReference _moveAction;

        [SerializeField]
        [Tooltip("Look action (right stick) using Unity's Input System")]
        private InputActionReference _lookAction;

        [SerializeField]
        [Tooltip("Sprint action (optional)")]
        private InputActionReference _sprintAction;
#endif

        [SerializeField]
        [Tooltip("The main controller to use for input when using OVRInput mode")]
        private OVRInput.Controller _mainController = OVRInput.Controller.LTouch;

        [SerializeField]
        [Tooltip("Axis to use for movement (left thumbstick)")]
        private OVRInput.Axis2D _moveAxis = OVRInput.Axis2D.PrimaryThumbstick;

        [SerializeField]
        [Tooltip("Axis to use for looking (right thumbstick)")]
        private OVRInput.Axis2D _lookAxis = OVRInput.Axis2D.SecondaryThumbstick;

        [SerializeField]
        [Tooltip("Button to hold for sprint (optional)")]
        private OVRInput.Button _sprintButton = OVRInput.Button.PrimaryThumbstickDown;

        [SerializeField]
        [Tooltip("Speed multiplier applied while moving normally (m/s)")]
        private float _speedFactor = 2.5f;

        [SerializeField]
        [Tooltip("Speed multiplier applied while sprinting (m/s)")]
        private float _sprintSpeedFactor = 4.5f;

        [SerializeField]
        [Tooltip("The rate of acceleration during movement")]
        private float _acceleration = 5f;

        [SerializeField]
        [Tooltip("The rate of damping on movement while grounded")]
        private float _groundDamping = 40f;

        [SerializeField]
        [Tooltip("Joystick dead zone threshold")]
        [Range(0f, 1f)]
        private float _joystickThreshold = 0.25f;

        private bool _inputActive;
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _currentDirection = Vector3.forward;
        private Vector3 _targetVelocity = Vector3.zero;
        private Vector3 _actualVelocity = Vector3.zero;

        private void Start()
        {
            AutoAssignCameraRig();
            if (_referenceTransform == null && _cameraRig != null)
            {
                _referenceTransform = _cameraRig.transform;
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            AutoAssignCameraRig();
            if (_referenceTransform == null && _cameraRig != null)
            {
                _referenceTransform = _cameraRig.transform;
                EditorUtility.SetDirty(this);
            }
        }

        private void OnValidate()
        {
            AutoAssignCameraRig();
            if (_referenceTransform == null && _cameraRig != null)
            {
                _referenceTransform = _cameraRig.transform;
                EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Update()
        {
            CalculateVelocityAndDirection();
        }

        private void AutoAssignCameraRig()
        {
            if (_cameraRig == null)
            {
                _cameraRig = FindAnyObjectByType<OVRCameraRig>();
#if UNITY_EDITOR
                if (_cameraRig != null)
                {
                    EditorUtility.SetDirty(this);
                }
#endif
            }
        }

        private (Vector2 move, Vector2 look, bool sprint) ReadOVRInput()
        {
            return (
                OVRInput.Get(_moveAxis, _mainController),
                OVRInput.Get(_lookAxis, _mainController),
                OVRInput.Get(_sprintButton, _mainController)
            );
        }

#if USE_UNITY_INPUT_SYSTEM
        private (Vector2 move, Vector2 look, bool sprint) ReadUnityInput()
        {
            return (
                _moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero,
                _lookAction?.action?.ReadValue<Vector2>() ?? Vector2.zero,
                _sprintAction?.action?.IsPressed() ?? false
            );
        }

        private (Vector2 move, Vector2 look, bool sprint) ReadCombinedInput()
        {
            var (unityMove, unityLook, unitySprint) = ReadUnityInput();

            var ovrMove = Vector2.zero;
            var ovrLook = Vector2.zero;
            var ovrSprint = false;

            if (OVRInput.IsControllerConnected(_mainController))
            {
                (ovrMove, ovrLook, ovrSprint) = ReadOVRInput();
            }

            return (
                unityMove.magnitude > ovrMove.magnitude ? unityMove : ovrMove,
                unityLook.magnitude > ovrLook.magnitude ? unityLook : ovrLook,
                unitySprint || ovrSprint
            );
        }
#endif

        private (Vector2 move, Vector2 look, bool sprint) ReadInputForMode()
        {
            switch (_inputMode)
            {
#if USE_UNITY_INPUT_SYSTEM
                case InputMode.UnityInputSystem:
                    return ReadUnityInput();
                case InputMode.Both:
                    return ReadCombinedInput();
#endif
                case InputMode.OVRInput:
                default:
                    return ReadOVRInput();
            }
        }

        private Vector3 ApplyDeadzone(Vector3 input)
        {
            return input.magnitude < _joystickThreshold
                ? Vector3.zero
                : Vector3.ClampMagnitude(input, 1f);
        }

        private void CalculateVelocityAndDirection()
        {
            var (moveInput, lookInput, isSprinting) = ReadInputForMode();

            var referenceRotation = _referenceTransform != null
                ? Quaternion.Euler(0, _referenceTransform.eulerAngles.y, 0)
                : Quaternion.identity;

            var move = ApplyDeadzone(new Vector3(moveInput.x, 0f, moveInput.y));
            var look = ApplyDeadzone(new Vector3(lookInput.x, 0f, lookInput.y));

            _inputActive = move.magnitude > 0f || look.magnitude > 0f;

            _targetVelocity = Vector3.zero;
            if (move.magnitude > 0f)
            {
                var rotatedMove = referenceRotation * move;
                var speedMultiplier = isSprinting ? _sprintSpeedFactor : _speedFactor;
                _targetVelocity = rotatedMove * speedMultiplier;
            }

            float deltaTime = Time.deltaTime;
            var lerpFactor = _targetVelocity.magnitude > 0f ? _acceleration : _groundDamping;
            var lerpTarget = _targetVelocity.magnitude > 0f ? _targetVelocity : Vector3.zero;
            _actualVelocity = Vector3.Lerp(_actualVelocity, lerpTarget, lerpFactor * deltaTime);

            _currentVelocity = move.magnitude > 0f ? _targetVelocity : Vector3.zero;

            // Direction represents where the character wants to face.
            // When look input is active (right stick), use the look direction for turn-in-place.
            // Otherwise, default to camera forward.
            _currentDirection = look.magnitude > 0f
                ? (referenceRotation * look).normalized
                : referenceRotation * Vector3.forward;
        }

#if USE_UNITY_INPUT_SYSTEM
        /// <summary>Sets the Unity Input System action for movement input.</summary>
        public void SetMoveAction(InputActionReference moveAction) => _moveAction = moveAction;

        /// <summary>Sets the Unity Input System action for sprint input.</summary>
        public void SetSprintAction(InputActionReference sprintAction) => _sprintAction = sprintAction;

        /// <summary>Sets the Unity Input System action for look/rotation input.</summary>
        public void SetLookAction(InputActionReference lookAction) => _lookAction = lookAction;
#endif

        /// <summary>Sets the OVR camera rig used for input coordinate space.</summary>
        public void SetCameraRig(OVRCameraRig cameraRig) => _cameraRig = cameraRig;
    }
}
