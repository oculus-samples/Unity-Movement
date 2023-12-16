// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Reads inputs from <see cref="OVRPlugin"/> and passes that input to Unity objects
    /// </summary>
    public class OVRInputBinding : MonoBehaviour
    {
        /// <summary>
        /// Triggered by changes to <see cref="ButtonBinding.IsActive"/>
        /// </summary>
        [System.Serializable]
        public class UnityEvent_bool : UnityEvent<bool> { }

        /// <summary>
        /// Triggered by changes to <see cref="JoystickBinding.Input"/>
        /// </summary>
        [System.Serializable]
        public class UnityEvent_Vector2 : UnityEvent<Vector2> { }

        /// <summary>
        /// Data structure identifying an OVRInput to bind to a boolean event
        /// </summary>
        [System.Serializable]
        public class ButtonBinding
        {
            public string name;
            public OVRInput.Button Button = OVRInput.Button.Two;
            public OVRInput.Controller Controller = OVRInput.Controller.RTouch;
            public UnityEvent_bool Notify;
            private bool _valueLastCheck;

            public bool IsActive => OVRInput.Get(Button, Controller);

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
                return $"{Controller}:{Button}";
            }
        }

        /// <summary>
        /// Data structure identifying an OVRInput to bind to a Vector2 event
        /// </summary>
        [System.Serializable]
        public class JoystickBinding
        {
            public string name;
            public OVRInput.Axis2D Axis;
            public UnityEvent_Vector2 Notify;
            private Vector2 _valueLastCheck;

            public Vector2 Input => OVRInput.Get(Axis);

            public void Update()
            {
                Vector2 active = Input;
                if (active != _valueLastCheck)
                {
                    Notify.Invoke(active);
                    _valueLastCheck = active;
                }
            }

            public override string ToString()
            {
                return $"{Axis}";
            }
        }

        /// <summary>
        /// Button state listeners
        /// </summary>
        [Tooltip(OVRInputBindingTooltips.ButtonBindings)]
        [SerializeField]
        private ButtonBinding[] _buttonBindings;

        /// <summary>
        /// Joysticks state listeners
        /// </summary>
        [Tooltip(OVRInputBindingTooltips.JoystickBindings)]
        [SerializeField]
        private JoystickBinding[] joystickBindings;

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

        private void UpdateBindings()
        {
            for (int i = 0; i < _buttonBindings.Length; ++i)
            {
                _buttonBindings[i].Update();
            }
        }

        /// <inheritdoc/>
        private void Update()
        {
            if (_onUpdate)
            {
                UpdateBindings();
            }
        }

        /// <inheritdoc/>
        private void FixedUpdate()
        {
            if (_onFixedUpdate)
            {
                UpdateBindings();
            }
        }
    }
}
