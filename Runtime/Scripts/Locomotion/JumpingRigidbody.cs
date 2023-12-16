// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// Controls jumping details for a <see cref="Rigidbody"/> based character controller.
    /// </summary>
    public class JumpingRigidbody : MonoBehaviour
    {
        /// <summary>
        /// Callbacks to trigger at certain stages of jumping
        /// </summary>
        [System.Serializable]
        public class JumpEvents
        {
            public UnityEvent OnJump = new UnityEvent();
            public UnityEvent OnCantJump = new UnityEvent();
            public UnityEvent OnJumpApexReached = new UnityEvent();
            public UnityEvent OnJumpCollided = new UnityEvent();
            public UnityEvent OnJumpFinished = new UnityEvent();
        }

        private static readonly Vector3 GravityDirection = Vector3.down;

        private static float GravityAcceleration => Mathf.Abs(Physics.gravity.y);

        /// <summary>
        /// Should reference the main <see cref="Rigidbody"/> of the character controller
        /// </summary>
        [Tooltip(JumpingRigidbodyTooltips.Rigidbody)]
        [SerializeField]
        private Rigidbody _rigidbody;

        /// <summary>
        /// How many units high the character controller should jump
        /// </summary>
        [Tooltip(JumpingRigidbodyTooltips.TargetJumpHeight)]
        [SerializeField]
        private float _targetJumpHeight = 1;

        /// <summary>
        /// Prevents jump impulse if the character controller is not grounded
        /// </summary>
        [Tooltip(JumpingRigidbodyTooltips.CanOnlyJumpOnGround)]
        [SerializeField]
        private bool _canOnlyJumpOnGround = true;

        /// <summary>
        /// These collision layers will be checked for collision as valid ground to jump from
        /// </summary>
        [Tooltip(JumpingRigidbodyTooltips.FloorLayerMask)]
        [SerializeField]
        private LayerMask _floorLayerMask = 1;

        /// <summary>
        /// Callbacks to trigger at certain stages of jumping
        /// </summary>
        [Tooltip(JumpingRigidbodyTooltips.JumpEvents)]
        [SerializeField]
        private JumpEvents _jumpEvents = new JumpEvents();

        /// <summary>
        /// True while controller is moving up during jump
        /// </summary>
        private bool _isJumpAscending;

        /// <summary>
        /// True while controller is falling, after a jump
        /// </summary>
        private bool _isFalling;

        /// <summary>
        /// Used to ignore collision with the ground when a jump starts.
        /// </summary>
        private int _framesSinceJumpStarted;

        /// <summary>
        /// If the user wants the controller to jump
        /// </summary>
        private bool _isJumpCurrentlyPressed;

        /// <summary>
        /// If the controller requested a jump this frame already (prevents multiple jump application)
        /// </summary>
        private bool _receivedScriptedJumpButtonPressThisFrame;

        /// <summary>
        /// how wide the character controller's standing base is
        /// </summary>
        private float _footRadius = .125f;

        /// <summary>
        /// Distance expected of ray to determine grounding
        /// </summary>
        private float _defaultHeightAboveGround = 1;

        /// <summary>
        /// The +/- wiggle-room for the height raycast calculation to determine grounding
        /// </summary>
        private float _heightEpsilon = 1f / 1024;

        /// <summary>
        /// The jump velocity necessary to achieve the desired <see cref="_targetJumpHeight"/>
        /// </summary>
        private float _jumpVelocity;

        /// <summary>
        /// Timestamp of when a jump started
        /// </summary>
        private float _jumpStart;

        /// <summary>
        /// Timestamp of when a jump's apex should be reached
        /// </summary>
        private float _jumpHalfway;

        /// <summary>
        /// maximum distance to check for floor below the player
        /// </summary>
        private const float MaxStandDistance = 10;

        /// <summary>
        /// Callbacks to trigger at certain stages of jumping
        /// </summary>
        public JumpEvents Events => _jumpEvents;

        /// <inheritdoc cref="_isJumpCurrentlyPressed"/>
        public bool JumpInput
        {
            get => _isJumpCurrentlyPressed;
            set
            {
                if (value)
                {
                    JumpButtonPressed();
                }
                _receivedScriptedJumpButtonPressThisFrame = true;
            }
        }

        /// <summary>
        /// Used to blend between jump/fall animations. Positive is jump, negative is fall.
        /// If 1, the jump is starting. If 0, then we're at the apex. If negative, we're falling.
        /// </summary>
        public float CurrentJumpPower
        {
            get
            {
                float now = Time.time;
                float secondsUntilApex = now - _jumpHalfway;
                float jumpHalfDuration = _jumpHalfway - _jumpStart;
                float power = secondsUntilApex / (float)jumpHalfDuration;
                return Mathf.Clamp(power, -1, 1);
            }
        }

        /// <inheritdoc cref="_targetJumpHeight"/>
        public float TargetJupmHeight
        {
            get => _targetJumpHeight;
            set
            {
                _targetJumpHeight = value;
                RefreshJumpVelocity();
            }
        }

        /// <inheritdoc cref="_isFalling"/>
        public bool IsFalling
        {
            get => _isFalling;
            set => _isFalling = value;
        }

        /// <summary>
        /// Explicitly press the jump button. Do the jump physics if not already jumping.
        /// </summary>
        public void JumpButtonPressed()
        {
            _receivedScriptedJumpButtonPressThisFrame = true;
            bool jumpWasOnlyJustPressed = !_isJumpCurrentlyPressed;
            JumpOrNot(jumpWasOnlyJustPressed);
            _isJumpCurrentlyPressed = true;
        }

        /// <summary>
        /// Jump if possible, or don't if logic should prevent it
        /// </summary>
        /// <param name="canTriggerJumpFail">if true, failure to jump will trigger an event</param>
        private void JumpOrNot(bool canTriggerJumpFail)
        {
            if (_canOnlyJumpOnGround && !IsOnGround())
            {
                if (canTriggerJumpFail)
                {
                    _jumpEvents.OnCantJump.Invoke();
                }
                return;
            }
            DoJump();
        }

        /// <summary>
        /// Explicitly force the jump physics to happen
        /// </summary>
        public void DoJump()
        {
            Vector3 velocity = _rigidbody.velocity;
            float fallSpeed = Vector3.Dot(_rigidbody.velocity, GravityDirection);
            velocity -= GravityDirection * fallSpeed;
            velocity += -GravityDirection * _jumpVelocity;
            _rigidbody.velocity = velocity;
            _jumpEvents.OnJump.Invoke();
            _jumpStart = Time.time;
            float jumpArcDurationSeconds = CalculateCompleteJumpDuration(_jumpVelocity, GravityAcceleration);
            float jumpHalfDuration = jumpArcDurationSeconds / 2;
            _jumpHalfway = _jumpStart + jumpHalfDuration;
            _isJumpAscending = true;
            _isFalling = false;
            _framesSinceJumpStarted = 0;
        }

        /// <summary>
        /// Determine if the character controller is on the ground, and able to jump
        /// </summary>
        public bool IsOnGround()
        {
            float height = HeightFromFloor();
            return height < _defaultHeightAboveGround + _heightEpsilon;
        }

        private void RefreshJumpVelocity()
        {
            _jumpVelocity = CalcJumpVelocity(_targetJumpHeight, GravityAcceleration);
        }

        public static float CalcJumpVelocity(float jumpHeight, float gForce)
        {
            return Mathf.Sqrt(2 * jumpHeight * gForce);
        }

        private void Start()
        {
            RefreshJumpVelocity();
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInParent<Rigidbody>();
                Debug.LogWarning($"{this} is missing a {nameof(_rigidbody)} value, using \"{_rigidbody}\"");
            }
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                _defaultHeightAboveGround = capsule.height / 2 - capsule.center.y;
            }
            else
            {
                _defaultHeightAboveGround = HeightFromFloor();
            }
            JumpCollision jumpCollision = _rigidbody.gameObject.AddComponent<JumpCollision>();
            jumpCollision.JupingRigidbody = this;
            if (IsOnGround())
            {
                _jumpEvents.OnJumpFinished.Invoke();
            }
            else
            {
                _jumpEvents.OnJumpApexReached.Invoke();
            }
        }

        private void FixedUpdate()
        {
            if (!_receivedScriptedJumpButtonPressThisFrame)
            {
                if (_isJumpCurrentlyPressed)
                {
                    JumpButtonPressed();
                }
                _isJumpCurrentlyPressed = false;
            }
            _receivedScriptedJumpButtonPressThisFrame = false;

            if (_isJumpAscending && Time.time > _jumpHalfway)
            {
                _isJumpAscending = false;
                _isFalling = true;
                _jumpEvents.OnJumpApexReached.Invoke();
            }
            ++_framesSinceJumpStarted;
        }

        private float HeightFromFloor()
        {
            // start ray slightly above position, we may be exactly on the floor
            float extraOffset = _footRadius + _heightEpsilon;
            Vector3 rayStart = _rigidbody.position - GravityDirection * extraOffset;
            if (!Physics.SphereCast(rayStart, _footRadius, GravityDirection, out RaycastHit hit,
                MaxStandDistance, _floorLayerMask))
            {
                return float.PositiveInfinity;
            }
            return hit.distance - _heightEpsilon;
        }

        private static float CalculateCompleteJumpDuration(float jumpVelocity, float gForce)
        {
            return 2 * jumpVelocity / gForce;
        }

        /// <summary>
        /// Called by <see cref="JumpCollision.OnCollisionEnter(Collision)"/>
        /// </summary>
        /// <param name="collision"></param>
        internal void Collided(Collision collision)
        {
            if (_framesSinceJumpStarted <= 1)
            {
                return;
            }
            _isJumpAscending = false;
            float collisionDotProduct = Vector3.Dot(collision.contacts[0].normal, -GravityDirection);
            const float minimumStableGroundValue = 1f / 32;
            if (collisionDotProduct > minimumStableGroundValue)
            {
                if (_isFalling || _isJumpAscending)
                {
                    _isFalling = _isJumpAscending = false;
                    _jumpEvents.OnJumpFinished.Invoke();
                }
            }
            else
            {
                _isFalling = true;
                _jumpEvents.OnJumpCollided.Invoke();
            }
        }
    }

    /// <summary>
    /// <see cref="OnCollisionEnter"/> listener that notifies a <see cref="JumpingRigidbody"/>
    /// </summary>
    public class JumpCollision : MonoBehaviour
    {
        public JumpingRigidbody JupingRigidbody;

        private void OnCollisionEnter(Collision collision)
        {
            if (!enabled)
            {
                return;
            }
            JupingRigidbody.Collided(collision);
        }
    }
}
