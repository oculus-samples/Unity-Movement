// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Reflection;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.XR.Movement
{
    /// <summary>
    /// Attribute that creates a button in the inspector for a field that can invoke a method when clicked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets or sets the width of the button in the inspector.
        /// </summary>
        public float ButtonWidth { get; set; } = _presetButtonWidth;

        private const float _presetButtonWidth = 80;
        private const float _presetButtonHeight = 20;

        /// <summary>
        /// The name of the method to invoke when the button is clicked.
        /// </summary>
        public readonly string _methodName;

        /// <summary>
        /// The height of the button in the inspector.
        /// </summary>
        public readonly float _buttonHeight;

        /// <summary>
        /// Creates a new inspector button attribute with default height.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke when the button is clicked.</param>
        public InspectorButtonAttribute(string methodName)
        {
            _methodName = methodName;
            _buttonHeight = _presetButtonHeight;
        }
        /// <summary>
        /// Creates a new inspector button attribute with custom height.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke when the button is clicked.</param>
        /// <param name="buttonHeight">The height of the button in the inspector.</param>
        public InspectorButtonAttribute(string methodName, float buttonHeight)
        {
            _methodName = methodName;
            _buttonHeight = buttonHeight;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _method = null;

        public override void OnGUI(Rect positionRect, SerializedProperty prop, GUIContent label)
        {
            var inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            var rect = positionRect;
            rect.height = inspectorButtonAttribute._buttonHeight;
            if (!GUI.Button(rect, label.text))
            {
                return;
            }
            var eventType = prop.serializedObject.targetObject.GetType();
            var eventName = inspectorButtonAttribute._methodName;
            if (_method == null)
            {
                _method = eventType.GetMethod(eventName,
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static);
            }
            _method?.Invoke(prop.serializedObject.targetObject, null);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            return inspectorButtonAttribute._buttonHeight;
        }
    }
#endif
}
