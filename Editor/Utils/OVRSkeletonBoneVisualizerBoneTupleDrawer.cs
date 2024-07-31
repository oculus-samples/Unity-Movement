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
    [CustomPropertyDrawer(typeof(OVRSkeletonBoneVisualizer.BoneTuple))]
    public class OVRSkeletonBoneVisualizerBoneTupleDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BoneTupleGUI(position, property, label);
        }

        /// <summary>
        /// Call from OnGUI to draw a <see cref="BoneVisualizer{BoneType}.BoneTuple"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        public static void BoneTupleGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OVRSkeletonBoneVisualizer.BoneTuple tuple;
            SerializedProperty hide = property.FindPropertyRelative(nameof(tuple.Hide));
            SerializedProperty fromBone = property.FindPropertyRelative(nameof(tuple.FirstBone));
            SerializedProperty toBone = property.FindPropertyRelative(nameof(tuple.SecondBone));

            EditorGUI.BeginProperty(position, label, property);
            const string ElementLabel = "Element ";
            if (!label.text.StartsWith(ElementLabel))
            {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            }
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            const int CheckboxWidth = 16;
            float remainingWidth = position.width - CheckboxWidth;
            Rect checkboxRect = new Rect(position.x, position.y, CheckboxWidth, position.height);
            Rect rectFromBone = new Rect(position.x + CheckboxWidth, position.y,
                remainingWidth / 2, position.height);
            Rect rectToBone = new Rect(position.x + CheckboxWidth + remainingWidth / 2, position.y,
                remainingWidth / 2, position.height);
            hide.boolValue = !EditorGUI.Toggle(checkboxRect, !hide.boolValue);
            EditorGUI.PropertyField(rectFromBone, fromBone, GUIContent.none);
            EditorGUI.PropertyField(rectToBone, toBone, GUIContent.none);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
