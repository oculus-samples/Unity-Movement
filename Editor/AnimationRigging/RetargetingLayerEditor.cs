// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Utils;
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

            if (GUILayout.Button("Calculate Adjustments"))
            {
                var retargetingLayer = serializedObject.targetObject as RetargetingLayer;
                if (retargetingLayer != null)
                {
                    var animator = retargetingLayer.GetComponent<Animator>();
                    AddComponentsHelper.AddJointAdjustments(animator, retargetingLayer);
                    EditorUtility.SetDirty(retargetingLayer);
                }
            }

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
