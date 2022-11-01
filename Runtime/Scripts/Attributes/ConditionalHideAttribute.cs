// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Attributes
{
    /// <summary>
    /// Used on a SerializedField to conditionally hide this field.
    /// Class borrowed from the InteractionSDK.
    /// </summary>
    public class ConditionalHideAttribute : PropertyAttribute
    {
        /// <summary>
        /// Field path.
        /// </summary>
        public string ConditionalFieldPath { get; private set; }

        /// <summary>
        /// Field's hide value.
        /// </summary>
        public object HideValue { get; private set; }

        /// <summary>
        /// ConditionalHideAttribute constructor that expects a field path
        /// and hide value to be set.
        /// </summary>
        /// <param name="fieldName">Initial value for field path.</param>
        /// <param name="hideValue">Initial hide value.</param>
        public ConditionalHideAttribute(string fieldName, object hideValue)
        {
            ConditionalFieldPath = fieldName;
            HideValue = hideValue;
        }
    }
}
