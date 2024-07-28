// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Designed to turn on/off objects with <see cref="UnityEngine.UI.Button"/>
    /// </summary>
    public class ActivateToggle : MonoBehaviour
    {
        /// <summary>
        /// Callback notifies UI text element of state change
        /// </summary>
        [System.Serializable] public class TextChangeEvent : UnityEvent<string> { }

        /// <summary>
        /// Describes objects bound to a state and functionality triggered by state change
        /// </summary>
        [System.Serializable]
        public class State
        {
            /// <summary>
            /// Name of this state
            /// </summary>
            [Tooltip(ActivateToggleTooltips.Set_Name)]
            public string Name;

            /// <summary>
            /// keep track of whether or not this state is activated
            /// </summary>
            protected bool Active;

            /// <summary>
            /// If true, this state will be ignored by Prev() and Next() methods
            /// </summary>
            [Tooltip(ActivateToggleTooltips.Set_Ignored)]
            public bool Ignored;

            /// <summary>
            /// Objects that will activate and deactivate with this state
            /// </summary>
            [Tooltip(ActivateToggleTooltips.Set_ObjectsToActivate)]
            public List<GameObject> ObjectsToActivate = new List<GameObject>();

            public UnityEvent OnActivate = new UnityEvent();
            public UnityEvent OnDeactivate = new UnityEvent();

            public State() { }

            public State(string name, System.Action onActivate = null, System.Action onDeactivate = null)
            {
                Name = name;
                if (onActivate != null)
                {
                    OnActivate.AddListener(new UnityAction(onActivate));
                }
                if (onDeactivate != null)
                {
                    OnDeactivate.AddListener(new UnityAction(onDeactivate));
                }
            }

            /// <summary>
            /// Activates/Deactivates this state (unless it is already activated/deactivated)
            /// </summary>
            public void SetActive(bool active)
            {
                bool activate = active && !Active;
                bool deactivate = !active && Active;
                Active = active;
                if (activate)
                {
                    OnActivate.Invoke();
                }
                if (deactivate)
                {
                    OnDeactivate.Invoke();
                }
                ObjectsToActivate.ForEach(o => o?.SetActive(active));
            }
        }

        /// <summary>
        /// Which state is set from state list
        /// </summary>
        [Tooltip(ActivateToggleTooltips.Index)]
        [SerializeField]
        protected int _index;

        /// <summary>
        /// Whether or not Next/Prev will wrap back around after the limit
        /// </summary>
        [Tooltip(ActivateToggleTooltips.Index)]
        [SerializeField]
        protected bool _wrapIndex = true;

        /// <summary>
        /// The list of triggerable states
        /// </summary>
        [Tooltip(ActivateToggleTooltips.States)]
        [SerializeField]
        protected List<State> _states = new List<State>();

        /// <summary>
        /// When the Index changes, these callbacks will be called with the name of the triggered state
        /// </summary>
        [Tooltip(ActivateToggleTooltips.OnSetNameChange)]
        [SerializeField]
        protected TextChangeEvent _onSetNameChange;

        /// <summary>
        /// All current states
        /// </summary>
        public List<State> States => _states;

        /// <summary>
        /// The current state
        /// </summary>
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                Refresh();
            }
        }

        /// <summary>
        /// Changes the current state without triggering callbacks from the
        /// <see cref="Refresh()"/> method
        /// </summary>
        /// <param name="value"></param>
        public void SetIndexWithoutNotify(int value)
        {
            _index = value;
        }

        /// <summary>
        /// Calls <see cref="Refresh"/> to start the default state
        /// </summary>
        private void Start()
        {
            Refresh();
        }

        /// <summary>
        /// Deactivates all states that are not the current state, and
        /// activates the current state
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < _states.Count; i++)
            {
                if (i == _index)
                {
                    continue;
                }
                _states[i].SetActive(false);
            }
            _states[_index].SetActive(true);
            _onSetNameChange.Invoke(_states[_index].Name);
        }

        /// <summary>
        /// Sets the next state in the list, ignoring states marked to be ignored.
        /// </summary>
        public void Next()
        {
            int i = NextIndex(+1);
            if (IsValidIndex(i))
            {
                _index = i;
                Refresh();
            }
        }

        /// <summary>
        /// Sets the previous state in the list, ignoring states marked to be ignored.
        /// </summary>
        public void Prev()
        {
            int i = NextIndex(-1);
            if (IsValidIndex(i))
            {
                _index = i;
                Refresh();
            }
        }

        private int NextIndex(int direction)
        {
            int index = _index;
            do
            {
                index += direction;
                if (index == _index)
                {
                    break;
                }
                if (_wrapIndex)
                {
                    if (index >= _states.Count)
                    {
                        index = 0;
                    }
                    else if (index < 0)
                    {
                        index = _states.Count - 1;
                    }
                }
            }
            while (index >= 0 && index < _states.Count && _states[index].Ignored);
            return index;
        }

        /// <summary>
        /// Determines if the given value references a valid state
        /// </summary>
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < _states.Count && !_states[index].Ignored;
        }

        /// <summary>
        /// Allows external scripts/UI to change available state options
        /// </summary>
        /// <param name="index"></param>
        public void IgnoreState(int index)
        {
            _states[index].Ignored = true;
        }

        /// <summary>
        /// Allows external scripts/UI to change available state options
        /// </summary>
        /// <param name="index"></param>
        public void UnignoreState(int index)
        {
            _states[index].Ignored = true;
        }
    }
}
