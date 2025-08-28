// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Input;
#endif
using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="ISDKSkeletalProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(ISDKSkeletalProcessor))]
    public class ISDKSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_cameraRig", new GUIContent("Camera Rig")),
            ("_leftHand", new GUIContent("Left Hand")),
            ("_rightHand", new GUIContent("Right Hand")),
            ("_moveHandBackToOriginalPosition", new GUIContent("Move to original position")),
            ("_maxDisplacementDistance", new GUIContent("Max displacement distance"))
        };

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(
                position.x,
                position.y - EditorGUIUtility.singleLineHeight * 1.1f,
                position.width,
                EditorGUIUtility.singleLineHeight);
            var isFoldoutExpanded = property.FindPropertyRelative("_isFoldoutExpanded");
            isFoldoutExpanded.boolValue = EditorGUI.Foldout(foldoutRect, isFoldoutExpanded.boolValue, GUIContent.none);

            if (!isFoldoutExpanded.boolValue)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            // Draw properties
            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var propertyRect = new Rect(
                    position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;
                EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
            }

            var buttonRect = new Rect(
                position.x,
                position.y + elementVerticalSpacing * _propertiesToDraw.Length,
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Setup"))
            {
                RunSetup(property);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void RunSetup(SerializedProperty property)
        {
#if ISDK_DEFINED
            // First search for HandVisual components
            var handVisuals = Object.FindObjectsByType<HandVisual>(FindObjectsSortMode.None);
            GameObject leftHand = null;
            GameObject rightHand = null;

            if (handVisuals is { Length: >= 2 })
            {
                // Find left and right hands from HandVisual components
                foreach (var handVisual in handVisuals)
                {
                    if (IsLeftHand(handVisual.gameObject))
                    {
                        leftHand = handVisual.gameObject;
                    }
                    else if (IsRightHand(handVisual.gameObject))
                    {
                        rightHand = handVisual.gameObject;
                    }
                }
            }

            // If we didn't find both hands from HandVisual, search for SyntheticHand
            if (leftHand == null || rightHand == null)
            {
                var synthHands = Object.FindObjectsByType<SyntheticHand>(FindObjectsSortMode.None);

                if (synthHands is { Length: >= 2 })
                {
                    foreach (var synthHand in synthHands)
                    {
                        if (leftHand == null && IsLeftHand(synthHand.gameObject))
                        {
                            leftHand = synthHand.gameObject;
                        }
                        else if (rightHand == null && IsRightHand(synthHand.gameObject))
                        {
                            rightHand = synthHand.gameObject;
                        }
                    }
                }
            }

            if (leftHand != null && rightHand != null &&
                MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<ISDKSkeletalProcessor>(
                    property, true, out var isdkProcessor))
            {
                var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                Undo.RecordObject(retargeter, "Update ISDK processor");
                isdkProcessor.CameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
                isdkProcessor.LeftHand = leftHand;
                isdkProcessor.RightHand = rightHand;
                isdkProcessor.MaxDisplacementDistance = 0.05f;
            }
            else
            {
                Debug.LogError($"Error! Could not set up ISDK processor. Could not find both left and right hands.");
            }
#endif
        }

        private bool IsLeftHand(GameObject handObject)
        {
            return ContainsLeftInHierarchy(handObject);
        }

        private bool IsRightHand(GameObject handObject)
        {
            return ContainsRightInHierarchy(handObject);
        }

        private bool ContainsLeftInHierarchy(GameObject obj)
        {
            // Check the object's name
            if (obj.name.ToLower().Contains("left"))
                return true;

            // Check parent names
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("left"))
                    return true;
                parent = parent.parent;
            }

            // Check child names
            return CheckChildrenForLeft(obj.transform);
        }

        private bool ContainsRightInHierarchy(GameObject obj)
        {
            // Check the object's name
            if (obj.name.ToLower().Contains("right"))
                return true;

            // Check parent names
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("right"))
                    return true;
                parent = parent.parent;
            }

            // Check child names
            return CheckChildrenForRight(obj.transform);
        }

        private bool CheckChildrenForLeft(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLower().Contains("left"))
                    return true;
                if (CheckChildrenForLeft(child))
                    return true;
            }
            return false;
        }

        private bool CheckChildrenForRight(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLower().Contains("right"))
                    return true;
                if (CheckChildrenForRight(child))
                    return true;
            }
            return false;
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var numberOfProperties = _propertiesToDraw.Length;
            return
                // first is number of properties plus one for the button
                EditorGUIUtility.singleLineHeight * (numberOfProperties + 1) +
                // next is just number of properties (button excluded since it's last item)
                EditorGUIUtility.standardVerticalSpacing * numberOfProperties;
        }
    }
}
