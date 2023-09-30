// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// This class interfaces with animators using specifically architected
    /// <see cref="RuntimeAnimatorController"/>s. Multiple simultaneous
    /// animators are controleld in parallel, for easy swapping, or layering.
    /// <code></code>
    /// Variables
    /// <code>
    /// * Horizontal (float)
    /// * Vertical (float)
    /// * Grounded (bool)
    /// </code>
    /// States
    /// <code>
    /// * Grounded
    /// * NotGrounded
    /// </code>
    /// </summary>
    public class AnimatorHooks : MonoBehaviour
    {
        /// <summary>
        /// Variables expected to exist in the Animator Controller
        /// </summary>
        public enum Variables
        {
            Horizontal,
            Vertical,
            Grounded
        }

        /// <summary>
        /// States expected to exist in the Animator Controller
        /// </summary>
        public enum States
        {
            Grounded,
            NotGrounded
        }

        /// <summary>
        /// Converts <see cref="Variables"/> string values into Hash IDs for fast lookup.
        /// </summary>
        private static int[] _variableIdLookup;

        /// <summary>
        /// Converts <see cref="States"/> string values into Hash IDs for fast lookup.
        /// </summary>
        private static int[] _stateIdLookup;

        /// <summary>
        /// String array of the <see cref="Variables"/> enum.
        /// </summary>
        private static string[] _variableNames = System.Enum.GetNames(typeof(Variables));

        /// <summary>
        /// String array of the <see cref="States"/> enum.
        /// </summary>
        private static string[] _stateNames = System.Enum.GetNames(typeof(States));

        /// <summary>
        /// If true, <see cref="_animators"/> will be dynamically set
        /// </summary>
        [Tooltip(AnimatorHooksTooltips.AutoAssignAnimatorsFromChildren)]
        [SerializeField]
        private bool _autoAssignAnimatorsFromChildren = true;

        /// <summary>
        /// Animators who should receive signals to animate
        /// </summary>
        [Tooltip(AnimatorHooksTooltips.Animators)]
        [SerializeField]
        [ContextMenuItem(nameof(RefreshAnimators),nameof(RefreshAnimators))]
        private Animator[] _animators;

        /// <summary>
        /// A valid animator to querey for variable values
        /// </summary>
        private Animator _cachedValidAnimator;

        static AnimatorHooks()
        {
            CalculateHashIds(_variableNames, out _variableIdLookup);
            CalculateHashIds(_stateNames, out _stateIdLookup);
        }

        /// <summary>
        /// Converts the given strings into a list of hash IDs
        /// </summary>
        /// <param name="names"></param>
        /// <param name="hashIds"></param>
        protected static void CalculateHashIds(string[] names, out int[] hashIds)
        {
            hashIds = new int[names.Length];
            for (int i = 0; i < names.Length; ++i)
            {
                int hashId = Animator.StringToHash(names[i]);
                hashIds[i] = hashId;
                for (int j = 0; j < i; ++j)
                {
                    if (hashIds[j] != hashId)
                    {
                        continue;
                    }
                    Debug.LogError($"Hash Collision '{names[i]}' vs '{names[j]}'");
                }
            }
        }

        /// <summary>
        /// The current Animators managed by this class.
        /// </summary>
        public Animator[] Animators
        {
            get => _animators;
            set
            {
                _animators = value;
                _cachedValidAnimator = null;
            }
        }

        /// <summary>
        /// The Horizontal axis value, as though it were from Input.GetAxis("Horizontal")
        /// </summary>
        public float Horizontal
        {
            get => GetFloat(Variables.Horizontal);
            set
            {
                SetFloat(Variables.Horizontal, value);
            }
        }

        /// <summary>
        /// The Vertical axis value, as though it were from Input.GetAxis("Vertical")
        /// </summary>
        public float Vertical
        {
            get => GetFloat(Variables.Vertical);
            set
            {
                SetFloat(Variables.Vertical, value);
            }
        }

        /// <summary>
        /// User axis input, from Horizontal and Vertical
        /// </summary>
        public Vector2 InputHorizontalVertical
        {
            get => new Vector2(GetFloat(Variables.Horizontal), GetFloat(Variables.Vertical));
            set
            {
                if (ValidAnimator == null)
                {
                    return;
                }
                if (GetBool(Variables.Grounded))
                {
                    SetFloat(Variables.Horizontal, value.x);
                    SetFloat(Variables.Vertical, value.y);
                }
            }
        }

        /// <summary>
        /// Is the animator doing grounded animations (eg: walking/running)
        /// </summary>
        public bool Grounded
        {
            get => GetBool(Variables.Grounded);
            set
            {
                SetBool(Variables.Grounded, value);
            }
        }

        /// <summary>
        /// Ensures a valid animator is provided to querey variables from
        /// </summary>
        private Animator ValidAnimator
        {
            get
            {
                if (IsAnimatorValid(_cachedValidAnimator))
                {
                    return _cachedValidAnimator;
                }
                _cachedValidAnimator = null;
                for (int i = 0; i < _animators.Length; ++i)
                {
                    if (!IsAnimatorValid(_animators[i]))
                    {
                        continue;
                    }
                    _cachedValidAnimator = _animators[i];
                    break;
                }
                return _cachedValidAnimator;
            }
        }

        /// <summary>
        /// Facade for <see cref="Animator.GetFloat(int)"/>
        /// </summary>
        public float GetFloat(Variables variable)
        {
            return ValidAnimator.GetFloat(_variableIdLookup[(int)variable]);
        }

        /// <summary>
        /// Applies a function to each active animator.
        /// </summary>
        /// <param name="action"></param>
        public void ForEachActiveAnimator(System.Action<Animator> action)
        {
            for (int i = 0; i < _animators.Length; ++i)
            {
                if (!IsAnimatorValid(_animators[i]))
                {
                    continue;
                }
                action.Invoke(_animators[i]);
            }
        }

        private bool IsAnimatorValid(Animator animator)
        {
            return animator != null && animator.isActiveAndEnabled;
        }

        /// <summary>
        /// Sets float values for all active Animators of this object
        /// </summary>
        public void SetFloat(Variables variable, float value)
        {
            for (int i = 0; i < _animators.Length; ++i)
            {
                if (!IsAnimatorValid(_animators[i]))
                {
                    continue;
                }
                _animators[i].SetFloat(_variableIdLookup[(int)variable], value);
            }
        }

        /// <summary>
        /// Facade for <see cref="Animator.GetBool(int)"/>
        /// </summary>
        public bool GetBool(Variables variable)
        {
            return ValidAnimator.GetBool(_variableIdLookup[(int)variable]);
        }

        /// <summary>
        /// Sets bool values for all active Animators of this object
        /// </summary>
        public void SetBool(Variables variable, bool value)
        {
            for (int i = 0; i < _animators.Length; ++i)
            {
                if (!IsAnimatorValid(_animators[i]))
                {
                    continue;
                }
                _animators[i].SetBool(_variableIdLookup[(int)variable], value);
            }
        }

        /// <summary>
        /// Sets bool values for all active Animators of this object
        /// </summary>
        public void SetState(States state)
        {
            for (int i = 0; i < _animators.Length; ++i)
            {
                if (!IsAnimatorValid(_animators[i]))
                {
                    continue;
                }
                _animators[i].Play(_stateIdLookup[(int)state]);
            }
        }

        private void Start()
        {
            RefreshAnimators();
        }

        /// <summary>
        /// Recalculates list of <see cref="Animators"/> to send duplicate signals to.
        /// Exits early if <see cref="_autoAssignAnimatorsFromChildren"/> is false.
        /// </summary>
        public void RefreshAnimators()
        {
            if (!_autoAssignAnimatorsFromChildren)
            {
                return;
            }
            Animator[] childAnimators = GetComponentsInChildren<Animator>(true);
            List<Animator> finalList = new List<Animator>();
            for (int i = 0; i < childAnimators.Length; ++i)
            {
                Animator anim = childAnimators[i];
                if (anim == null)
                {
                    continue;
                }
                int missingVariableCount = CountMissingVariables(anim, _variableNames);
                if (missingVariableCount != 0 && missingVariableCount != _variableNames.Length)
                {
                    Debug.LogWarning($"Animation \"{anim.name}\" does not have all variables:"
                        + $" [{string.Join(", ", _variableNames)}]");
                }
                if (missingVariableCount != 0)
                {
                    continue;
                }
                finalList.Add(anim);
            }
            Animators = finalList.ToArray();
        }

        /// <summary>
        /// Check if the given animator has the expected animations. This method should be
        /// refactored if <see cref="Variables"/> or <see cref="States"/> have more than a few
        /// elements each.
        /// </summary>
        /// <returns>variables in the given list that are missing from the given animator</returns>
        private static int CountMissingVariables(Animator animator, string[] variableNames)
        {
            List<string> hasParameters = new List<string>();
            for (int i = 0; i < animator.parameterCount; ++i)
            {
                int found = System.Array.IndexOf(variableNames, animator.GetParameter(i).name);
                if (found < 0)
                {
                    continue;
                }
                hasParameters.Add(variableNames[found]);
            }
            return variableNames.Length - hasParameters.Count;
        }
    }
}
