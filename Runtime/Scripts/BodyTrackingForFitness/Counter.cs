// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Increments an integer, accessible in editor using <see cref="UnityEvent_int"/> and
    /// <see cref="UnityEvent_string"/> callbacks.
    /// </summary>
    public class Counter : MonoBehaviour
    {
        private static class CounterTooltips
        {
            public const string Value = "The counter's value";

            public const string CallbackInt = "Notified whenever the counter's value changes";

            public const string CallbackString = "Notified whenever the counter's value changes";
        }

        /// <summary>
        /// Callback to send an integer once the integer is known
        /// </summary>
        [Serializable]
        public class UnityEvent_int : UnityEvent<int> { }

        /// <summary>
        /// Callback to send a string once the string is known
        /// </summary>
        [Serializable]
        public class UnityEvent_string : UnityEvent<string> { }

        /// <summary>
        /// The counter's value
        /// </summary>
        [Tooltip(CounterTooltips.Value)]
        [SerializeField]
        private int _value;

        /// <summary>
        /// Notified whenever the counter's value changes
        /// </summary>
        [Tooltip(CounterTooltips.CallbackInt)]
        [SerializeField]
        private UnityEvent_int callbackInt = new UnityEvent_int();

        /// <summary>
        /// Notified whenever the counter's value changes
        /// </summary>
        [Tooltip(CounterTooltips.CallbackString)]
        [SerializeField]
        private UnityEvent_string callbackString = new UnityEvent_string();

        /// <inheritdoc cref="_value"/>
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                callbackInt.Invoke(_value);
                callbackString.Invoke(_value.ToString());
            }
        }

        /// <summary>
        /// Increments (or decrements with negative value) the counter's value
        /// </summary>
        public void Add(int value)
        {
            Value += value;
        }
    }
}
