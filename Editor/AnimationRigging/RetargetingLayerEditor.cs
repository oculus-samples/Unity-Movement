// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for <see cref="RetargetingLayer"/>.
    /// </summary>
    [CustomEditor(typeof(RetargetingLayer)), CanEditMultipleObjects]
    public class RetargetingLayerEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
