// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Attributes
{
    /// <summary>
    /// Adds an [Optional] label in the inspector over any SerializedField with this attribute.
    /// Class borrowed from the InteractionSDK.
    /// </summary>
    [CustomPropertyDrawer(typeof(OptionalAttribute))]
    public class OptionalDrawer : DecoratorDrawer
    {
        private static readonly float HEADER_SIZE_AS_PERCENT = 0.25f;

        public override float GetHeight()
        {
            return base.GetHeight() * ( 1f + HEADER_SIZE_AS_PERCENT );
        }

        public override void OnGUI(Rect position)
        {
            position.y += GetHeight() * HEADER_SIZE_AS_PERCENT / ( 1f + HEADER_SIZE_AS_PERCENT );
            EditorGUI.LabelField(position, "[Optional]");
        }
    }
}
