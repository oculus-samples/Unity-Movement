// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Attribute used to display an int as an enum.
    /// </summary>
    public class IntAsEnumAttribute : PropertyAttribute
    {
        public System.Type Type { get; private set; }
        public IntAsEnumAttribute(System.Type type)
        {
            Type = type;
        }
    }
}
