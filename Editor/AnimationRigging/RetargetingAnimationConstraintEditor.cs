// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for <see cref="RetargetingAnimationConstraint"/>.
    /// </summary>
    [CustomEditor(typeof(RetargetingAnimationConstraint)), CanEditMultipleObjects]
    public class RetargetingAnimationConstraintEditor : Editor
    {
        private SerializedProperty _avatarMask;

        private void OnEnable()
        {
            _avatarMask =
                serializedObject.FindProperty("m_Data._avatarMask");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            RetargetingAnimationConstraint retargetingAnimConstraint = (RetargetingAnimationConstraint)target;
            var previousMaskValue = _avatarMask.objectReferenceValue;

            base.OnInspectorGUI();

            serializedObject.Update();

            if ((Application.isEditor && Application.isPlaying) &&
                (_avatarMask.objectReferenceValue != previousMaskValue))
            {
                retargetingAnimConstraint.data.CreateAvatarMaskInstances();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
