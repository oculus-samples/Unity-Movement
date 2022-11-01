// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Attributes
{
    /// <summary>
    /// Used on a SerializedField surfaces the expectation that this
    /// field can remain empty. Class borrowed from the InteractionSDK.
    /// </summary>
    public class OptionalAttribute : PropertyAttribute
    {
        public OptionalAttribute()
        {
        }
    }
}
