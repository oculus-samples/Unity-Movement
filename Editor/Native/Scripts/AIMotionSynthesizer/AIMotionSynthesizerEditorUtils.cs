// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.AI.Editor
{
    internal static class AIMotionSynthesizerEditorUtils
    {
        internal static void DrawSection(string title, Color color, Action content)
        {
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = orig;

            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, padding = new RectOffset(4, 0, 1, 1) };
            EditorGUILayout.LabelField(title, style);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        internal static void DrawPropertyWithBar(SerializedProperty prop, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, GUILayout.ExpandWidth(true));
            var val = prop.floatValue;
            var rect = GUILayoutUtility.GetRect(50, EditorGUIUtility.singleLineHeight, GUILayout.Width(50));
            EditorGUI.ProgressBar(rect, Mathf.InverseLerp(min, max, val), $"{val:F1}");
            EditorGUILayout.EndHorizontal();
        }
    }
}
