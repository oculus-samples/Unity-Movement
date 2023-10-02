// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Listens to Unity <see cref="KeyCode"/> press and release, as well as
    /// <see cref="Input.GetAxisRaw(string)"/> "Hoizontal" and "Vertical"
    /// </summary>
    public class UnityInputBinding : MonoBehaviour
    {
        /// <summary>
        /// Triggered by <see cref="KeyCode"/> press and release
        /// </summary>
        [System.Serializable]
        public class UnityEvent_bool : UnityEvent<bool> { }

        /// <summary>
        /// Triggered by <see cref="Input.GetAxisRaw(string)"/> "Hoizontal" and "Vertical"
        /// </summary>
        [System.Serializable]
        public class UnityEvent_Vector2 : UnityEvent<Vector2> { }

        /// <summary>
        /// Input binding meta data that wraps around <see cref="UnityEvent_bool"/>
        /// </summary>
        [System.Serializable]
        public class KeyBinding
        {
            public string name;
            public KeyCode KeyCode = KeyCode.Escape;
            public UnityEvent_bool Notify;
            private bool _valueLastCheck;

            public bool IsActive => Input.GetKey(KeyCode);

            public void Update()
            {
                bool active = IsActive;
                if (active != _valueLastCheck)
                {
                    Notify.Invoke(active);
                    _valueLastCheck = active;
                }
            }

            public override string ToString()
            {
                return $"{KeyCode}";
            }
        }

        /// <summary>
        /// Triggered by <see cref="Input.GetButton(string)"/> "Jump"
        /// </summary>
        [Tooltip(UnityInputBindingTooltips.ButtonJump)]
        [SerializeField]
        private UnityEvent_bool _buttonJump;

        /// <summary>
        /// Triggered by <see cref="Input.GetAxisRaw(string)"/> "Hoizontal" and "Vertical"
        /// </summary>
        [Tooltip(UnityInputBindingTooltips.AxisHorizontalVertical)]
        [SerializeField]
        private UnityEvent_Vector2 _axisHorizontalVertical;

        /// <summary>
        /// Key bindings listening to <see cref="Input.GetKey(KeyCode)"/>
        /// </summary>
        [Tooltip(UnityInputBindingTooltips.KeyBindings)]
        [SerializeField]
        private KeyBinding[] _keyBindings;

        /// <summary>
        /// Check input bindings on <see cref="Update"/>
        /// </summary>
        [Tooltip(OVRInputBindingTooltips.OnUpdate)]
        [SerializeField]
        private bool _onUpdate;

        /// <summary>
        /// Check input bindings on <see cref="FixedUpdate"/>
        /// </summary>
        [Tooltip(OVRInputBindingTooltips.OnFixedUpdate)]
        [SerializeField]
        private bool _onFixedUpdate = true;

        private bool _jumpLastCheck;
        private Vector2 _axisLastCheck;

        private void Update()
        {
            if (_onUpdate)
            {
                MoveControls();
                JumpControls();
                UpdateBindings();
            }
        }

        private void FixedUpdate()
        {
            if (_onFixedUpdate)
            {
                MoveControls();
                JumpControls();
                UpdateBindings();
            }
        }

        private void MoveControls()
        {
            Vector2 axisInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool inputIsMeaningful = axisInput != Vector2.zero;
            bool inputIsDiffrentFromLastFrame = axisInput != _axisLastCheck;
            if (inputIsMeaningful || inputIsDiffrentFromLastFrame)
            {
                _axisLastCheck = axisInput;
                _axisHorizontalVertical.Invoke(axisInput);
            }
        }

        private void JumpControls()
        {
            bool jump = Input.GetButton("Jump");
            if (jump != _jumpLastCheck)
            {
                _jumpLastCheck = jump;
                _buttonJump.Invoke(_jumpLastCheck);
            }
        }

        private void UpdateBindings()
        {
            for (int i = 0; i < _keyBindings.Length; ++i)
            {
                _keyBindings[i].Update();
            }
        }
    }
}
