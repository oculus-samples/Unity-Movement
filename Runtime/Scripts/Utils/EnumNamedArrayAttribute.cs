// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Used to label lists and arrays with an Enum instead of the default 'Element #'
    /// </summary>
    public class EnumNamedArrayAttribute : PropertyAttribute
    {
        /// <summary>
        /// <see cref="Enum"/> values replacing "Element X" when rendering lists in Unity Editor
        /// </summary>
        public Type TargetEnum;

        /// <summary>
        /// Cached names, to avoid multiple reflection invocations and allocations
        /// </summary>
        private static Dictionary<Type, string[]> _cachedNames = new Dictionary<Type, string[]>();

        /// <summary>
        /// Used to label lists and arrays with an Enum instead of the default 'Element #'
        /// </summary>
        /// <param name="targetEnum">Which <see cref="Enum"/> to use as a label</param>
        public EnumNamedArrayAttribute(Type targetEnum)
        {
            TargetEnum = targetEnum;
        }

        /// <summary>
        /// Get cached names of enums. This method reduces memory thrash at Unity editor time
        /// </summary>
        public string[] GetNames()
        {
            return GetNames(TargetEnum);
        }

        private static string[] GetNames(Type enumType)
        {
            if (!_cachedNames.TryGetValue(enumType, out string[] names))
            {
                _cachedNames[enumType] = names = Enum.GetNames(enumType);
            }
            return names;
        }
    }
}
