// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Custom editor class for comparing vector hashes.
    /// </summary>
    [CustomEditor(typeof(CompareVectorHashes))]
    public class CompareVectorHashesEditor : Editor
    {
        /// <summary>
        /// Defines the look of the script's GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CompareVectorHashes myScript = (CompareVectorHashes)target;
            if (GUILayout.Button("Compare Hashes"))
            {
                myScript.CompareHashesAgainstEachOther();
            }
        }
    }
}
