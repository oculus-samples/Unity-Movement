// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Utils;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the full body deformation constraint.
    /// </summary>
    [CustomEditor(typeof(FullBodyDeformationConstraint))]
    public class FullBodyDeformationConstraintEditor : Editor
    {
        private static class Content
        {
            public static readonly GUIContent Weight = EditorGUIUtility.TrTextContent
            (
                "Weight",
                "The weight of the constraint."
            );

            public static readonly GUIContent References = EditorGUIUtility.TrTextContent
            (
                "References",
                "References for deformation."
            );

            public static readonly GUIContent Settings = EditorGUIUtility.TrTextContent
            (
                "Settings",
                "Settings for deformation parameters."
            );

            public static readonly GUIContent AdvancedSettings = EditorGUIUtility.TrTextContent
            (
                "Advanced Settings",
                "Additional settings that allow for fine-tuning more advanced parameters."
            );

            public static readonly GUIContent ShouldersWeight = EditorGUIUtility.TrTextContent
            (
                "Shoulders Weight",
                "The deformation weight for both shoulders."
            );

            public static readonly GUIContent ArmsWeight = EditorGUIUtility.TrTextContent
            (
                "Arms Weight",
                "The deformation weight for both arms."
            );

            public static readonly GUIContent HandsWeight = EditorGUIUtility.TrTextContent
            (
                "Hands Weight",
                "The deformation weight for both hands."
            );

            public static readonly GUIContent LegsWeight = EditorGUIUtility.TrTextContent
            (
                "Legs Weight",
                "The deformation weight for both legs."
            );

            public static readonly GUIContent FeetAlignmentWeight = EditorGUIUtility.TrTextContent
            (
                "Feet Alignment Weight",
                "The deformation weight for feet alignment."
            );

            public static readonly GUIContent ToesWeight = EditorGUIUtility.TrTextContent
            (
                "Toes Weight",
                "The deformation weight for both toes."
            );
        }

        private readonly EditorPrefsBool _referencesFoldout =
            EditorPrefsBool.Create<FullBodyDeformationConstraint>(Content.References.text, true);
        private readonly EditorPrefsBool _settingsFoldout =
            EditorPrefsBool.Create<FullBodyDeformationConstraint>(Content.Settings.text, true);
        private readonly EditorPrefsBool _advancedSettingsFoldout =
            EditorPrefsBool.Create<FullBodyDeformationConstraint>(Content.AdvancedSettings.text, false);

        private SerializedProperty _weightProperty;

        private SerializedProperty _animatorProperty;
        private SerializedProperty _customSkeletonProperty;

        private SerializedProperty _spineTranslationCorrectionTypeProperty;
        private SerializedProperty _spineLowerAlignmentWeightProperty;
        private SerializedProperty _spineUpperAlignmentWeightProperty;
        private SerializedProperty _chestAlignmentWeightProperty;
        private SerializedProperty _shouldersHeightReductionWeightProperty;
        private SerializedProperty _shouldersWidthReductionWeightProperty;
        private SerializedProperty _affectArmsBySpineCorrectionProperty;
        private SerializedProperty _alignFeetWeightProperty;

        private SerializedProperty _leftShoulderWeightProperty;
        private SerializedProperty _rightShoulderWeightProperty;
        private SerializedProperty _leftArmWeightProperty;
        private SerializedProperty _rightArmWeightProperty;
        private SerializedProperty _leftHandWeightProperty;
        private SerializedProperty _rightHandWeightProperty;
        private SerializedProperty _leftLegWeightProperty;
        private SerializedProperty _rightLegWeightProperty;
        private SerializedProperty _leftToesWeightProperty;
        private SerializedProperty _rightToesWeightProperty;

        private SerializedProperty _hipsToHeadBonesProperty;
        private SerializedProperty _hipsToHeadBoneTargetsProperty;
        private SerializedProperty _feetToToesBoneTargetsProperty;
        private SerializedProperty _leftArmDataProperty;
        private SerializedProperty _rightArmDataProperty;
        private SerializedProperty _leftLegDataProperty;
        private SerializedProperty _rightLegDataProperty;
        private SerializedProperty _bonePairDataProperty;
        private SerializedProperty _boneAdjustmentDataProperty;

        private SerializedProperty _startingScaleProperty;
        private SerializedProperty _hipsToHeadDistanceProperty;
        private SerializedProperty _hipsToFootDistanceProperty;

        private readonly int _indentSpacing = 10;

        private void OnEnable()
        {
            _weightProperty = serializedObject.FindProperty("m_Weight");
            var data = serializedObject.FindProperty("m_Data");

            _animatorProperty = data.FindPropertyRelative("_animator");
            _customSkeletonProperty = data.FindPropertyRelative("_customSkeleton");

            _spineTranslationCorrectionTypeProperty = data.FindPropertyRelative("_spineTranslationCorrectionType");
            _spineLowerAlignmentWeightProperty = data.FindPropertyRelative("_spineLowerAlignmentWeight");
            _spineUpperAlignmentWeightProperty = data.FindPropertyRelative("_spineUpperAlignmentWeight");
            _chestAlignmentWeightProperty = data.FindPropertyRelative("_chestAlignmentWeight");
            _shouldersHeightReductionWeightProperty = data.FindPropertyRelative("_shouldersHeightReductionWeight");
            _shouldersWidthReductionWeightProperty = data.FindPropertyRelative("_shouldersWidthReductionWeight");
            _affectArmsBySpineCorrectionProperty = data.FindPropertyRelative("_affectArmsBySpineCorrection");

            _leftShoulderWeightProperty = data.FindPropertyRelative("_leftShoulderWeight");
            _rightShoulderWeightProperty = data.FindPropertyRelative("_rightShoulderWeight");
            _leftArmWeightProperty = data.FindPropertyRelative("_leftArmWeight");
            _rightArmWeightProperty = data.FindPropertyRelative("_rightArmWeight");
            _leftHandWeightProperty = data.FindPropertyRelative("_leftHandWeight");
            _rightHandWeightProperty = data.FindPropertyRelative("_rightHandWeight");
            _leftLegWeightProperty = data.FindPropertyRelative("_leftLegWeight");
            _rightLegWeightProperty = data.FindPropertyRelative("_rightLegWeight");
            _leftToesWeightProperty = data.FindPropertyRelative("_leftToesWeight");
            _rightToesWeightProperty = data.FindPropertyRelative("_rightToesWeight");
            _alignFeetWeightProperty = data.FindPropertyRelative("_alignFeetWeight");

            _hipsToHeadBonesProperty = data.FindPropertyRelative("_hipsToHeadBones");
            _hipsToHeadBoneTargetsProperty = data.FindPropertyRelative("_hipsToHeadBoneTargets");
            _feetToToesBoneTargetsProperty = data.FindPropertyRelative("_feetToToesBoneTargets");
            _leftArmDataProperty = data.FindPropertyRelative("_leftArmData");
            _rightArmDataProperty = data.FindPropertyRelative("_rightArmData");
            _leftLegDataProperty = data.FindPropertyRelative("_leftLegData");
            _rightLegDataProperty = data.FindPropertyRelative("_rightLegData");
            _bonePairDataProperty = data.FindPropertyRelative("_bonePairData");
            _boneAdjustmentDataProperty = data.FindPropertyRelative("_boneAdjustmentData");

            _startingScaleProperty = data.FindPropertyRelative("_startingScale");
            _hipsToHeadDistanceProperty = data.FindPropertyRelative("_hipsToHeadDistance");
            _hipsToFootDistanceProperty = data.FindPropertyRelative("_hipsToFootDistance");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_weightProperty, Content.Weight);
            EditorGUILayout.Space();

            _referencesFoldout.Value =
                EditorGUILayout.BeginFoldoutHeaderGroup(_referencesFoldout.Value,
                    Content.References);
            if (_referencesFoldout.Value)
            {
                EditorGUI.indentLevel++;
                DisplayReferencesFoldoutContent();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            _settingsFoldout.Value =
                EditorGUILayout.BeginFoldoutHeaderGroup(_settingsFoldout.Value,
                    Content.Settings);
            if (_settingsFoldout.Value)
            {
                EditorGUI.indentLevel++;
                DisplaySettingsFoldoutContent();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-3);
            _advancedSettingsFoldout.Value =
                EditorGUILayout.Foldout(_advancedSettingsFoldout.Value,
                    Content.AdvancedSettings, true, EditorStyles.foldoutHeader);
            GUILayout.EndHorizontal();
            if (_advancedSettingsFoldout.Value)
            {
                EditorGUI.indentLevel++;
                DisplayAdvancedSettingsFoldoutContent();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            var constraint = (FullBodyDeformationConstraint)target;

            if (GUILayout.Button("Calculate bone data"))
            {
                DisplayCalculateBoneData(constraint);
            }
            if (Application.isPlaying && GUILayout.Button("Update job arrays"))
            {
                var rigBuilder = constraint.GetComponentInParent<RigBuilder>();
                rigBuilder.Build();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayCalculateBoneData(FullBodyDeformationConstraint constraint)
        {
            Undo.RecordObject(constraint, "Calculate bone data");
            var constraintData = constraint.data;
            var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
            var animator = constraint.GetComponentInParent<Animator>();
            if (skeleton != null)
            {
                constraintData.AssignOVRCustomSkeleton(skeleton);
            }
            else
            {
                constraintData.AssignAnimator(animator);
            }
            // Determine if this character is in T-pose or A-pose.
            var isTPose = HelperMenusCommon.CheckIfTPose(animator);

            constraintData.InitializeStartingScale();
            constraintData.ClearTransformData();
            constraintData.SetUpLeftArmData();
            constraintData.SetUpRightArmData();
            constraintData.SetUpLeftLegData();
            constraintData.SetUpRightLegData();
            constraintData.SetUpHipsAndHeadBones();
            constraintData.SetUpBonePairs();
            constraintData.SetUpBoneTargets(constraint.transform);
            constraintData.SetUpAdjustments(HelperMenusCommon.GetRestPoseObject(isTPose));
            constraint.data = constraintData;
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private void DisplayReferencesFoldoutContent()
        {
            if (_customSkeletonProperty.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(_animatorProperty);
            }
            if (_animatorProperty.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(_customSkeletonProperty);
            }
        }

        private void DisplaySettingsFoldoutContent()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Body"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_spineTranslationCorrectionTypeProperty);
            EditorGUILayout.PropertyField(_spineLowerAlignmentWeightProperty);
            EditorGUILayout.PropertyField(_spineUpperAlignmentWeightProperty);
            EditorGUILayout.PropertyField(_chestAlignmentWeightProperty);

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Shoulders"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            AverageWeightSlider(Content.ShouldersWeight,
                new[] { _leftShoulderWeightProperty, _rightShoulderWeightProperty });

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Arms"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_affectArmsBySpineCorrectionProperty);
            AverageWeightSlider(Content.ArmsWeight,
                new[] { _leftArmWeightProperty, _rightArmWeightProperty });
            AverageWeightSlider(Content.HandsWeight,
                new[] { _leftHandWeightProperty, _rightHandWeightProperty });

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Legs"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            AverageWeightSlider(Content.LegsWeight,
                new[] { _leftLegWeightProperty, _rightLegWeightProperty });
            EditorGUILayout.PropertyField(_alignFeetWeightProperty, Content.FeetAlignmentWeight);
            AverageWeightSlider(Content.ToesWeight,
                new[] { _leftToesWeightProperty, _rightToesWeightProperty });
        }

        private void DisplayAdvancedSettingsFoldoutContent()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Weights"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_shouldersHeightReductionWeightProperty);
            EditorGUILayout.PropertyField(_shouldersWidthReductionWeightProperty);
            EditorGUILayout.PropertyField(_leftShoulderWeightProperty);
            EditorGUILayout.PropertyField(_rightShoulderWeightProperty);
            EditorGUILayout.PropertyField(_leftArmWeightProperty);
            EditorGUILayout.PropertyField(_rightArmWeightProperty);
            EditorGUILayout.PropertyField(_leftHandWeightProperty);
            EditorGUILayout.PropertyField(_rightHandWeightProperty);
            EditorGUILayout.PropertyField(_leftLegWeightProperty);
            EditorGUILayout.PropertyField(_rightLegWeightProperty);
            EditorGUILayout.PropertyField(_leftToesWeightProperty);
            EditorGUILayout.PropertyField(_rightToesWeightProperty);

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * _indentSpacing);
            GUILayout.Label(new GUIContent("Cached Data"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            DisplayBonePairArray("Bone Pair Data", _bonePairDataProperty);
            DisplayBoneAdjustmentArray("Bone Adjustment Data", _boneAdjustmentDataProperty);
            EditorGUILayout.PropertyField(_hipsToHeadBonesProperty);
            EditorGUILayout.PropertyField(_hipsToHeadBoneTargetsProperty);
            EditorGUILayout.PropertyField(_feetToToesBoneTargetsProperty);
            EditorGUILayout.PropertyField(_leftArmDataProperty);
            EditorGUILayout.PropertyField(_rightArmDataProperty);
            EditorGUILayout.PropertyField(_leftLegDataProperty);
            EditorGUILayout.PropertyField(_rightLegDataProperty);
            EditorGUILayout.PropertyField(_startingScaleProperty);
            EditorGUILayout.PropertyField(_hipsToHeadDistanceProperty);
            EditorGUILayout.PropertyField(_hipsToFootDistanceProperty);
        }

        private void DisplayBonePairArray(string propertyName, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-3);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded,
                new GUIContent(propertyName, property.tooltip), true, EditorStyles.foldoutHeader);
            EditorGUILayout.Space();
            property.arraySize = EditorGUILayout.IntField(string.Empty, property.arraySize, GUILayout.MaxWidth(63));
            EditorGUILayout.EndHorizontal();
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < property.arraySize; i++)
                {
                    var propertyElement = property.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(propertyElement,
                        new GUIContent($"{propertyElement.FindPropertyRelative("StartBone").objectReferenceValue.name} -> " +
                            $"{propertyElement.FindPropertyRelative("EndBone").objectReferenceValue.name}",
                            "Bone pair."));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(3);
        }

        private void DisplayBoneAdjustmentArray(string propertyName, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-3);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded,
                new GUIContent(propertyName, property.tooltip), true, EditorStyles.foldoutHeader);
            EditorGUILayout.Space();
            property.arraySize = EditorGUILayout.IntField(string.Empty, property.arraySize, GUILayout.MaxWidth(63));
            EditorGUILayout.EndHorizontal();
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < property.arraySize; i++)
                {
                    var propertyElement = property.GetArrayElementAtIndex(i);
                    var boneProperty = propertyElement.FindPropertyRelative("Bone");
                    EditorGUILayout.PropertyField(propertyElement,
                        new GUIContent(boneProperty.enumNames[boneProperty.enumValueIndex],
                            "Bone adjustment."));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(3);
        }

        private void AverageWeightSlider(GUIContent content, SerializedProperty[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                return;
            }
            float averageWeight = weights.Sum(property => property.floatValue) / weights.Length;
            EditorGUI.BeginChangeCheck();
            averageWeight = EditorGUILayout.Slider(content, averageWeight, 0.0f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var weight in weights)
                {
                    weight.floatValue = averageWeight;
                }
            }
        }
    }
}
