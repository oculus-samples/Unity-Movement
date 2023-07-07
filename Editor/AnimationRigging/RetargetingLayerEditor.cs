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
        private SerializedProperty _positionCorrectMask;
        private SerializedProperty _tPoseMask;

        private void OnEnable()
        {
            _positionCorrectMask =
                serializedObject.FindProperty("_positionsToCorrectLateUpdate");
            _tPoseMask = serializedObject.FindProperty("_maskToSetToTPose");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            RetargetingLayer retargetingLayer = (RetargetingLayer)target;
            var previousPositionsCorrectValue = _positionCorrectMask.objectReferenceValue;
            var previousTPoseValue = _tPoseMask.objectReferenceValue;

            base.OnInspectorGUI();

            serializedObject.Update();

            if (Application.isEditor && Application.isPlaying)
            {
                if (_tPoseMask.objectReferenceValue != previousTPoseValue)
                {
                    retargetingLayer.CreatePositionsToCorrectLateUpdateMaskInstance();
                }
                if (_positionCorrectMask.objectReferenceValue != previousPositionsCorrectValue)
                {
                    retargetingLayer.CreateTPoseMaskInstance();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
