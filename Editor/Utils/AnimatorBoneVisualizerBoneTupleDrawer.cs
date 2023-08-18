// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Used to draw a <see cref="BoneVisualizer{BoneType}.BoneTuple"/> like:
    /// <code>
    /// [enabled] [FirstBone] [SecondBone]
    /// </code>
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimatorBoneVisualizer.BoneTuple))]
    public class AnimatorBoneVisualizerBoneTupleDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OVRSkeletonBoneVisualizerBoneTupleDrawer.BoneTupleGUI(position, property, label);
        }
    }
}
