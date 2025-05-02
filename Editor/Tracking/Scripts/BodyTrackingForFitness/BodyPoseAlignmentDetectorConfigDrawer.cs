// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
#if INTERACTION_OVR_DEFINED
    /// <summary>
    /// Used to simplify drawing of a
    /// <see cref="BodyPoseAlignmentDetector.AlignmentState"/> element.
    /// </summary>
    [CustomPropertyDrawer(typeof(BodyPoseAlignmentDetector.AlignmentState))]
    public class BodyPoseAlignmentDetectorStateEditor : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BoneTupleGUI(position, property, label);
        }

        /// <summary>
        /// Call from OnGUI to draw a <see cref="BodyPoseAlignmentDetector.AlignmentState"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        private static void BoneTupleGUI(Rect position, SerializedProperty property,
        GUIContent label)
        {
            SerializedProperty angle = property.FindPropertyRelative(
                nameof(BodyPoseAlignmentDetector.AlignmentState.AngleDelta));
            EditorGUI.BeginProperty(position, label, property);
            const string ElementLabel = "Element ";
            if (!label.text.StartsWith(ElementLabel))
            {
                position = EditorGUI.PrefixLabel(position,
                    GUIUtility.GetControlID(FocusType.Passive), label);
            }
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Rect rect = new Rect(position.x, position.y, position.width, position.height);
            EditorGUI.Slider(rect, "", angle.floatValue, 0, 180);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif
}
