// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Custom Editor for <see cref="AnimatorBoneVisualizer"/> component.
    /// </summary>
    [CustomEditor(typeof(AnimatorBoneVisualizer))]
    public class AnimatorBoneVisualizerEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var script = target as AnimatorBoneVisualizer;
            if (GUILayout.Button("Select all bones"))
            {
                if (script)
                {
                    script.SelectAllBones();
                }
            }

            if (GUILayout.Button("Clear all data"))
            {
                if (script)
                {
                    script.ClearData();
                }
            }
            DrawDefaultInspector();
        }
    }
}
