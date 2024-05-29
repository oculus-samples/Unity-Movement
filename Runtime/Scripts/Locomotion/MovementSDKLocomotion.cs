// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// A rigidbody physics based locomotion character controller designed to work with
    /// <see cref="OVRCameraRig"/>.
    /// </summary>
    public class MovementSDKLocomotion : MonoBehaviour
    {
        /// <summary>
        /// Triggered by changes to this transform's rotation
        /// </summary>
        [System.Serializable]
        public class UnityEvent_Quaternion : UnityEvent<Quaternion> { }

        /// <summary>
        /// Triggered by notification that the controlling joystick has changed
        /// </summary>
        [System.Serializable]
        public class UnityEvent_Vector2 : UnityEvent<Vector2> { }

        /// <summary>
        /// Callbacks to trigger on certain joystick input events
        /// </summary>
        [System.Serializable]
        public class JoystickEvents
        {
            /// <summary>
            /// Locomotion direction.
            /// </summary>
            public UnityEvent_Quaternion OnDirectionChange = new UnityEvent_Quaternion();
            /// <summary>
            /// User input, follows character direction.
            /// </summary>
            public UnityEvent_Vector2 OnUserInputChange = new UnityEvent_Vector2();
            /// <summary>
            /// User input, follows joystick direction.
            /// </summary>
            public UnityEvent_Vector2 OnUserInputChangeJoystickDir = new UnityEvent_Vector2();
            /// <summary>
            /// Start move event.
            /// </summary>
            public UnityEvent OnStartMove = new UnityEvent();
            /// <summary>
            /// Stop move event.
            /// </summary>
            public UnityEvent OnStopMove = new UnityEvent();
        }

        /// <summary>
        /// The <see cref="Rigidbody"/> that is controlled by this character controller.
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.Rigidbody)]
        [SerializeField]
        protected Rigidbody _rigidbody;

        /// <summary>
        /// The <see cref="Collider"/> that counts as this character's feet
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.Collider)]
        [SerializeField]
        protected Collider _collider;

        /// <summary>
        /// Joystick will move forward and backward, as well as strafe left and right at Speed
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.EnableMovement)]
        [SerializeField]
        private bool _enableMovement = true;

        /// <summary>
        /// Joystick will turn according to RotationAngle
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.EnableRotation)]
        [SerializeField]
        private bool _enableRotation = true;

        /// <summary>
        /// If Input given to input events reflects obstacles hampering velocity
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.ScaleInputByActualVelocity)]
        [SerializeField]
        private bool _scaleInputByActualVelocity = true;

        /// <summary>
        /// Default snap turn amount.
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.RotationAngle)]
        [SerializeField]
        private float _rotationAngle = 360f / 8;

        /// <summary>
        /// Default turn speed, when rotating smoothly
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.RotationPerSecond)]
        [SerializeField]
        private float _rotationPerSecond = 360f / 2;

        /// <summary>
        /// How quickly the controller will move with fully extended joystick
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.Speed)]
        [SerializeField]
        private float _speed = 3.0f;

        /// <summary>
        /// The Camera Rig
        /// </summary>
        [Tooltip(MovementSDKLocomotionTooltips.CameraRig)]
        [SerializeField]
        private OVRCameraRig _cameraRig;

        /// <summary>
        /// Callbacks to trigger on certain movement input events
        /// </summary>
        [FormerlySerializedAs("_joystickEvents")]
        [Tooltip(MovementSDKLocomotionTooltips.MovementEvents)]
        [SerializeField]
        private JoystickEvents _movementEvents = new JoystickEvents();

        /// <summary>
        /// Keeps track of whether input is being received. If not, zero out the input vector.
        /// </summary>
        private bool _receivedScriptedJoystickInputThisFrame;

        /// <summary>
        /// semaphore that prevents more than one snap per left/right joystick press
        /// </summary>
        private bool _readyToSnapTurn;

        /// <summary>
        /// If true, will cause smooth left turning at runtime
        /// </summary>
        private bool _smoothTurnLeft;

        /// <summary>
        /// If true, will cause smooth right turning at runtime
        /// </summary>
        private bool _smoothTurnRight;

        /// <summary>
        /// If true, will cause left snap turning at runtime
        /// </summary>
        private bool _snapTurnLeft;

        /// <summary>
        /// If true, will cause right snap turning at runtime
        /// </summary>
        private bool _snapTurnRight;

        /// <summary>
        /// Not-normalized 3D direction that the user is indicating they want to travel in
        /// </summary>
        private Vector3 _locomotionDirection;

        /// <summary>
        /// The <see cref="_locomotionDirection"/> value from last frame
        /// </summary>
        private Vector3 _lastLocomotionDirection;

        /// <summary>
        /// Actual joystick input value
        /// </summary>
        private Vector2 _joystickInput;

        /// <summary>
        /// <see cref="_joystickInput"/> value from last frame
        /// </summary>
        private Vector2 _lastReportedUserInput;

        /// <inheritdoc cref="_locomotionDirection"/>
        public Vector3 Direction => _locomotionDirection;

        /// <inheritdoc cref="_joystickInput"/>
        public Vector2 UserInput
        {
            get => _joystickInput;
            set => SetJoystickInput(value);
        }

        /// <inheritdoc cref="_smoothTurnLeft"/>
        public bool SmoothTurnLeft
        {
            get => _smoothTurnLeft;
            set => _smoothTurnLeft = value;
        }

        /// <inheritdoc cref="_smoothTurnRight"/>
        public bool SmoothTurnRight
        {
            get => _smoothTurnRight;
            set => _smoothTurnRight = value;
        }

        /// <inheritdoc cref="_snapTurnLeft"/>
        public bool SnapTurnLeft
        {
            get => _snapTurnLeft;
            set => _snapTurnLeft = value;
        }

        /// <inheritdoc cref="_snapTurnRight"/>
        public bool SnapTurnRight
        {
            get => _snapTurnRight;
            set => _snapTurnRight = value;
        }

        private bool _canUpdate;

        /// <inheritdoc/>
        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInParent<Rigidbody>();
                Debug.LogWarning($"{this} is missing a {nameof(_rigidbody)} value, using \"{_rigidbody}\"");
            }
            if (_cameraRig == null)
            {
                _cameraRig = GetComponentInChildren<OVRCameraRig>();
            }
            _canUpdate = true;
        }

        /// <inheritdoc/>
        private void Start()
        {
            _rigidbody.freezeRotation = true;
        }

        /// <inheritdoc/>
        private void FixedUpdate()
        {
            if (!_canUpdate)
            {
                // Reset the inputs while the locomotion can't receive any updates, so that no erroneous input is
                // captured and applied when update is enabled again.
                SetJoystickInput(Vector2.zero);
                _receivedScriptedJoystickInputThisFrame = false;
                _rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
                _locomotionDirection = Vector3.zero;
                return;
            }
            if (_smoothTurnLeft)
            {
                transform.Rotate(0, -_rotationPerSecond * Time.deltaTime, 0);
            }
            if (_smoothTurnRight)
            {
                transform.Rotate(0, _rotationPerSecond * Time.deltaTime, 0);
            }
            if (_enableMovement)
            {
                if (!_receivedScriptedJoystickInputThisFrame)
                {
                    SetJoystickInput(Vector2.zero);
                }
                UpdateMovement();
                _receivedScriptedJoystickInputThisFrame = false;
            }
            if (_enableRotation)
            {
                SnapTurn();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _canUpdate = hasFocus;
            _rigidbody.isKinematic = !hasFocus;
            _rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Calculates the move direction based on input
        /// </summary>
        private void SetJoystickInput(Vector2 joystickInput)
        {
            _receivedScriptedJoystickInputThisFrame = true;
            Quaternion cameraOrientation = _cameraRig.centerEyeAnchor.rotation;
            Quaternion avatarOrientation = transform.rotation;
            Vector3 cameraOrientationEuler = cameraOrientation.eulerAngles;
            cameraOrientationEuler.z = cameraOrientationEuler.x = 0f;
            cameraOrientation = Quaternion.Euler(cameraOrientationEuler);

            _locomotionDirection = Vector3.zero;
            _joystickInput = joystickInput;
            _locomotionDirection += cameraOrientation * (_joystickInput.x * Vector3.right);
            _locomotionDirection += cameraOrientation * (_joystickInput.y * Vector3.forward);

            Vector3 avatarDirection = CalculateDirectionBasedOnRigAndReferenceTransform(
                cameraOrientation, avatarOrientation, _locomotionDirection);

            Vector2 userInput = _scaleInputByActualVelocity ? ScaledByVelocity(avatarDirection)
                : new Vector2(avatarDirection.x, avatarDirection.z);
            Vector2 userInputJoystick = new Vector2(_joystickInput.x, _joystickInput.y);
            userInputJoystick = _scaleInputByActualVelocity ?
                ScaledByVelocity(new Vector3(userInputJoystick.x, 0.0f, userInputJoystick.y)) :
                userInputJoystick;

            if (_locomotionDirection != _lastLocomotionDirection || _lastReportedUserInput != userInput)
            {
                if (_joystickInput == Vector2.zero)
                {
                    _movementEvents.OnUserInputChange.Invoke(Vector2.zero);
                    _movementEvents.OnUserInputChangeJoystickDir.Invoke(Vector2.zero);
                    _movementEvents.OnStopMove.Invoke();
                }
                else
                {
                    _movementEvents.OnUserInputChange.Invoke(userInput);
                    _movementEvents.OnUserInputChangeJoystickDir.Invoke(userInputJoystick);
                    Quaternion newDirection = Quaternion.LookRotation(_locomotionDirection, Vector3.up);
                    _movementEvents.OnDirectionChange.Invoke(newDirection);
                    if (_lastLocomotionDirection == Vector3.zero)
                    {
                        _movementEvents.OnStartMove.Invoke();
                    }
                }
                _lastLocomotionDirection = _locomotionDirection;
                _lastReportedUserInput = userInput;
            }
        }

        private Vector3 CalculateDirectionBasedOnRigAndReferenceTransform(
            Quaternion cameraOrientation,
            Quaternion avatarOrientation,
            Vector3 fallbackLocomotionDirection)
        {
            Vector3 avatarDirection;

            bool avatarCameraRigOrientationMismatch = cameraOrientation != avatarOrientation;
            if (avatarCameraRigOrientationMismatch)
            {
                Vector3 avatarOrientationEuler = avatarOrientation.eulerAngles;
                Quaternion avatarOrientationOffset = Quaternion.Euler(0, -avatarOrientationEuler.y, 0);
                Quaternion combinedOrientation = cameraOrientation * avatarOrientationOffset;
                avatarDirection = combinedOrientation * (_joystickInput.x * Vector3.right);
                avatarDirection += combinedOrientation * (_joystickInput.y * Vector3.forward);
            }
            else
            {
                avatarDirection = fallbackLocomotionDirection;
            }

            return avatarDirection;
        }

        private Vector2 ScaledByVelocity(Vector3 directionInput)
        {
            if (_joystickInput == Vector2.zero)
            {
                return Vector2.zero;
            }
            Vector3 realDirection = _locomotionDirection.normalized;
            float actualSpeed = Vector3.Dot(realDirection, _rigidbody.velocity);
            Vector3 scaledDirection = directionInput.normalized * (actualSpeed / _speed);
            return new Vector2(scaledDirection.x, scaledDirection.z);
        }

        private void UpdateMovement()
        {
            if (_rigidbody.isKinematic)
            {
                _rigidbody.MovePosition(_rigidbody.position + _locomotionDirection * (_speed * Time.fixedDeltaTime));
            }
            else
            {
                Vector3 locomotionVelocity = _locomotionDirection * _speed;
                _rigidbody.velocity = locomotionVelocity + CurrentFallVelocity();
            }
        }

        private Vector3 CurrentFallVelocity()
        {
            float currentFallSpeed = Vector3.Dot(_rigidbody.velocity, Vector3.down);
            return Vector3.down * currentFallSpeed;
        }

        private void SnapTurn()
        {
            float amountToRotate = 0;
            if (_snapTurnLeft)
            {
                if (_readyToSnapTurn)
                {
                    amountToRotate = -_rotationAngle;
                }
                _snapTurnLeft = false;
            }
            else if (_snapTurnRight)
            {
                if (_readyToSnapTurn)
                {
                    amountToRotate = _rotationAngle;
                }
                _snapTurnRight = false;
            }
            else
            {
                _readyToSnapTurn = true;
            }
            if (amountToRotate != 0)
            {
                _readyToSnapTurn = false;
                Vector3 centerPoint = _collider.bounds.center;
                Transform _transform = transform;
                _transform.RotateAround(centerPoint, Vector3.up, amountToRotate);
                _movementEvents.OnDirectionChange.Invoke(_transform.rotation);
            }
        }
    }
}
