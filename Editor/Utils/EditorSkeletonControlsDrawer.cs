// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Used to create a single button hidden in a
    /// <see cref="BoneVisualizer{BoneType}.CustomBoneVisualData"/> element.
    /// </summary>
    [CustomPropertyDrawer(typeof(EditorSkeletonControls))]
    public class EditorSkeletonControlsDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(
            Rect position, SerializedProperty property, GUIContent label)
        {
            IListGenerating listGenerator =
                GetPropertyInstance(property, -2) as IListGenerating;
            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
            if (GUI.Button(position, nameof(listGenerator.GenerateList)))
            {
                listGenerator.GenerateList();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets <see cref="System.Object"/>s out of a <see cref="SerializedProperty"/>
        /// </summary>
        /// <param name="property">property to find the object instance of</param>
        /// <param name="offset">If zero, provides the raw (unserialized) object.
        /// -1 gives it's parent, if the property is in a serialized hierarchy.</param>
        public object GetPropertyInstance(SerializedProperty property, int offset = 0)
        {
            string path = property.propertyPath;
            object obj = property.serializedObject.targetObject;
            var type = obj.GetType();
            string[] fields = path.Split('.');
            int limit = fields.Length + offset;
            for (int i = 0; i < limit; i++)
            {
                FieldInfo info = type.GetField(fields[i], BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.Instance);
                if (info == null)
                {
                    break;
                }
                obj = info.GetValue(obj);
                type = info.FieldType;
            }
            return obj;
        }
    }
}
