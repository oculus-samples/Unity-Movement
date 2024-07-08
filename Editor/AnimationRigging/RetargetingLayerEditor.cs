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
            var retargetingLayer = serializedObject.targetObject as RetargetingLayer;
            var animatorComponent = retargetingLayer.GetComponent<Animator>();
            var animatorProperlyConfigured = IsAnimatorProperlyConfigured(animatorComponent);

            if (!animatorProperlyConfigured)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Requires Animator component with a humanoid " +
                    "avatar, and Translation DoF enabled in avatar's Muscles & Settings.", MessageType.Error);
                GUILayout.EndVertical();
            }

            base.OnInspectorGUI();

            if (animatorProperlyConfigured)
            {
                if (GUILayout.Button("Calculate Adjustments"))
                {
                    AddComponentsHelper.AddJointAdjustments(animatorComponent, retargetingLayer);
                    EditorUtility.SetDirty(retargetingLayer);
                }

                if (GUILayout.Button("Update to Animator Pose"))
                {
                    retargetingLayer.UpdateToAnimatorPose(animatorComponent);
                    EditorUtility.SetDirty(retargetingLayer);
                }
            }

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();
        }

        internal static bool IsAnimatorProperlyConfigured(Animator animatorComponent)
        {
            return animatorComponent != null && animatorComponent.avatar != null &&
                animatorComponent.avatar.isHuman &&
                animatorComponent.avatar.humanDescription.hasTranslationDoF;
        }
    }
}
