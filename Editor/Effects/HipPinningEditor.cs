// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Editor class defining interface for HipPinningLogic.
    /// </summary>
    [CustomEditor(typeof(HipPinningLogic))]
    public class HipPinningLogicEditor : Editor
    {
        private readonly string[] _hipBoneNames = { "Hips" };
        private readonly string[] _spineLowerBoneNames = { "SpineLower" };
        private readonly string[] _spineMiddleBoneNames = { "SpineMiddle" };
        private readonly string[] _spineUpperBoneNames = { "SpineUpper" };
        private readonly string[] _chestBoneNames = { "Chest" };

        private SerializedProperty _skeletonProp = null;
        private SerializedProperty _dataProviderProp = null;
        private SerializedProperty _hipPinningTargetProp = null;

        private SerializedProperty _hipPropertiesProp = null;

        private List<Transform> _bones;
        private List<string> _hipBoneIdLabels;

        private void OnEnable()
        {
            _skeletonProp = serializedObject.FindProperty("_skeleton");
            _dataProviderProp = serializedObject.FindProperty("_dataProvider");
            _hipPinningTargetProp = serializedObject.FindProperty("_hipPinningTargets");
            _hipPropertiesProp = serializedObject.FindProperty("_hipPinningProperties");
        }

        private void ApplyChanges()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            else
            {
                EditorUtility.SetDirty((HipPinningLogic)target);
            }
        }

        private void AssignMissingJoints(HipPinningLogic script)
        {
            FillBones(script);
            RemoveConstraints(script);
            script.HipPinProperties.BodyJointProperties.Clear();
            script.HipPinProperties.PositionConstraints.Clear();
            script.HipPinProperties.BodyJointProperties.Add(new HipPinningLogic.BodyJointProperties
            {
                BodyJoint = FindJoint(script, _hipBoneNames)
            });
            script.HipPinProperties.PositionConstraints.Add(null);
            script.HipPinProperties.BodyJointProperties.Add(new HipPinningLogic.BodyJointProperties
            {
                BodyJoint = FindJoint(script, _spineLowerBoneNames)
            });
            script.HipPinProperties.PositionConstraints.Add(null);
            script.HipPinProperties.BodyJointProperties.Add(new HipPinningLogic.BodyJointProperties
            {
                BodyJoint = FindJoint(script, _spineMiddleBoneNames)
            });
            script.HipPinProperties.PositionConstraints.Add(null);
            script.HipPinProperties.BodyJointProperties.Add(new HipPinningLogic.BodyJointProperties
            {
                BodyJoint = FindJoint(script, _spineUpperBoneNames)
            });
            script.HipPinProperties.PositionConstraints.Add(null);
            script.HipPinProperties.BodyJointProperties.Add(new HipPinningLogic.BodyJointProperties
            {
                BodyJoint = FindJoint(script, _chestBoneNames)
            });
            script.HipPinProperties.PositionConstraints.Add(null);
        }

        private GameObject FindJoint(HipPinningLogic script, string[] boneNames)
        {
            foreach (var boneName in boneNames)
            {
                foreach (var bone in _bones)
                {
                    if (NameMatchesExact(boneName, bone.name))
                    {
                        return bone.gameObject;
                    }
                }
                foreach (var bone in _bones)
                {
                    if (NameMatchesPattern(boneName, bone.name))
                    {
                        return bone.gameObject;
                    }
                }
            }
            return null;
        }

        private static bool NameMatchesExact(string sourceName, string targetName)
        {
            return string.Equals(targetName, sourceName, StringComparison.CurrentCultureIgnoreCase);
        }
        private static bool NameMatchesPattern(string sourceName, string targetName)
        {
            return targetName.ToLower().Contains(sourceName.ToLower());
        }

        private void FillBones(Component script)
        {
            _bones = new List<Transform>();
            FillChildBone(script.gameObject.transform);
        }

        private void FillChildBone(Transform bone)
        {
            _bones.Add(bone);
            for (int i = 0; i < bone.childCount; i++)
            {
                FillChildBone(bone.GetChild(i));
            }
        }

        private static void RemoveConstraints(HipPinningLogic script)
        {
            if (script.HipPinProperties.PositionConstraints.Count > 0)
            {
                for (int i = 0; i < script.HipPinProperties.PositionConstraints.Count; i++)
                {
                    var positionConstraint = script.HipPinProperties.PositionConstraints[i];
                    DestroyImmediate(positionConstraint);
                    script.HipPinProperties.PositionConstraints[i] = null;
                }
            }
        }

        private void CreateHipPropertiesUI(HipPinningLogic script)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Body Joints");
            if (GUILayout.Button("Add"))
            {
                script.HipPinProperties.BodyJointProperties.Add(null);
                script.HipPinProperties.PositionConstraints.Add(null);
                return;
            }
            if (GUILayout.Button("Remove"))
            {
                int removeIndex = script.HipPinProperties.BodyJointProperties.Count - 1;
                script.HipPinProperties.BodyJointProperties.RemoveAt(removeIndex);
                if (script.HipPinProperties.PositionConstraints[removeIndex] != null)
                {
                    DestroyImmediate(script.HipPinProperties.PositionConstraints[removeIndex]);
                }
                script.HipPinProperties.PositionConstraints.RemoveAt(removeIndex);
                return;
            }
            EditorGUILayout.EndHorizontal();
            if (_hipBoneIdLabels == null || _hipBoneIdLabels.Count == 0)
            {
                _hipBoneIdLabels = new List<string>();
                for (int i = (int)OVRSkeleton.BoneId.Body_Hips; i < (int)OVRSkeleton.BoneId.Body_End; i++)
                {
                    _hipBoneIdLabels.Add(OVRSkeleton.BoneLabelFromBoneId(OVRSkeleton.SkeletonType.Body, (OVRSkeleton.BoneId)i));
                }
            }
            if (script.HipPinProperties.BodyJointProperties != null)
            {
                for (int i = 0; i < script.HipPinProperties.BodyJointProperties.Count; i++)
                {
                    var bodyJointProperties = _hipPropertiesProp.FindPropertyRelative("BodyJointProperties")
                        .GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(
                        bodyJointProperties.FindPropertyRelative("BodyJoint"),
                        new GUIContent($"{_hipBoneIdLabels[i]} Joint",
                            HipPinningLogicTooltips.BodyJointPropertiesTooltips.BodyJoint),
                        GUILayout.Height(20));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(
                        bodyJointProperties.FindPropertyRelative("ConstraintWeight"),
                        new GUIContent("   Constraint Weight",
                            HipPinningLogicTooltips.BodyJointPropertiesTooltips.ConstraintWeight),
                        GUILayout.Height(20));
                    GUILayout.Space(10);
                    EditorGUILayout.PropertyField(
                        bodyJointProperties.FindPropertyRelative("OffsetWeight"),
                        new GUIContent("Offset Weight",
                            HipPinningLogicTooltips.BodyJointPropertiesTooltips.OffsetWeight),
                        GUILayout.Height(20));
                    EditorGUILayout.EndHorizontal();

                    if (!script.EnableApplyTransformations)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(
                            bodyJointProperties.FindPropertyRelative("PositionDistanceThreshold"),
                            new GUIContent("Position Distance Threshold",
                                HipPinningLogicTooltips.BodyJointPropertiesTooltips.PositionDistanceThreshold),
                            GUILayout.Height(20));
                        GUILayout.Space(10);
                        EditorGUILayout.PropertyField(
                            bodyJointProperties.FindPropertyRelative("PositionDistanceWeight"),
                            new GUIContent("Position Distance Weight",
                                HipPinningLogicTooltips.BodyJointPropertiesTooltips.PositionDistanceWeight),
                            GUILayout.Height(20));
                        EditorGUILayout.EndHorizontal();
                    }

                    using (new EditorGUI.DisabledScope(script.HipPinProperties.PositionConstraints.Count > i))
                    {
                        EditorGUILayout.PropertyField(
                            bodyJointProperties.FindPropertyRelative("InitialLocalRotation"),
                            new GUIContent("Joint Initial Rotation",
                                HipPinningLogicTooltips.BodyJointPropertiesTooltips.InitialLocalRotation),
                            GUILayout.Height(20));
                    }

                    EditorGUILayout.PropertyField(
                        _hipPropertiesProp.FindPropertyRelative("PositionConstraints").GetArrayElementAtIndex(i),
                        new GUIContent($"   {_hipBoneIdLabels[i]} Position Constraint",
                            HipPinningLogicTooltips.HipPropertiesTooltips.PositionConstraint),
                        GUILayout.Height(20));
                }
            }

            if (serializedObject.FindProperty("_enableLegRotationLimits").boolValue)
            {
                GUILayout.Space(10);
                if (_hipPropertiesProp.FindPropertyRelative("RotationLimits").arraySize <
                    (int)HipPinningLogic.HipPinningProperties.RotationLimit.Count)
                {
                    script.HipPinProperties.RotationLimits =
                        new float[(int)HipPinningLogic.HipPinningProperties.RotationLimit.Count];
                }
                for (int i = 0; i < (int)HipPinningLogic.HipPinningProperties.RotationLimit.NegativeY + 1; i++)
                {
                    EditorGUILayout.PropertyField(
                        _hipPropertiesProp.FindPropertyRelative("RotationLimits").GetArrayElementAtIndex(i),
                        new GUIContent($"{(HipPinningLogic.HipPinningProperties.RotationLimit)i} Rotation Limit",
                            HipPinningLogicTooltips.HipPropertiesTooltips.RotationLimit),
                        GUILayout.Height(20));
                }
            }
        }

        private void CheckValidity(HipPinningLogic script)
        {
            if (script.HipPinProperties == null || script.HipPinProperties.BodyJointProperties.Count == 0)
            {
                EditorGUILayout.HelpBox("The Hip Joint is unassigned!", MessageType.Error);
            }
            if (_hipPinningTargetProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("The Hip Pinning Target is unassigned!", MessageType.Error);
                return;
            }

            var constraintSources = new List<ConstraintSource>();
            if (script.HipPinProperties != null && script.HipPinProperties.PositionConstraints.Count > 0)
            {
                foreach (var positionConstraint in script.HipPinProperties.PositionConstraints)
                {
                    if (positionConstraint == null)
                    {
                        EditorGUILayout.HelpBox("Missing a Body Joint Positional Constraint!", MessageType.Error);
                        break;
                    }
                    positionConstraint.GetSources(constraintSources);
                    if (!AreConstraintSourcesValid(constraintSources))
                    {
                        EditorGUILayout.HelpBox("Invalid constraint sources for the Body Joint Positional Constraint!", MessageType.Error);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Missing the Hip Positional Constraint!", MessageType.Error);
            }
        }

        private static bool AreConstraintSourcesValid(List<ConstraintSource> constraintSources)
        {
            if (constraintSources.Count == 0)
            {
                return false;
            }
            foreach (var constraintSource in constraintSources)
            {
                if (constraintSource.sourceTransform == null)
                {
                    return false;
                }
            }
            return true;
        }

        private static void SaveHipPinningOffsets(HipPinningLogic script)
        {
            foreach (var jointProperties in script.HipPinProperties.BodyJointProperties)
            {
                jointProperties.InitialLocalRotation = jointProperties.BodyJoint.transform.localRotation;
            }
        }

        /// <summary>
        /// Defines the script's GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var script = (HipPinningLogic)target;
            CheckValidity(script);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_hipPinningActive"),
                new GUIContent("Hip Pinning Active", HipPinningLogicTooltips.HipPinningActive),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                _skeletonProp,
                new GUIContent("OVR Skeleton", HipPinningLogicTooltips.Skeleton),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                _dataProviderProp,
                new GUIContent("OVR Skeleton Data Provider", HipPinningLogicTooltips.DataProvider),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                _hipPinningTargetProp,
                new GUIContent("Hip Pinning Targets", HipPinningLogicTooltips.HipPinningTargets),
                true);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableLegRotation"),
                new GUIContent("Enable Leg Rotation", HipPinningLogicTooltips.EnableLegRotation),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableLegRotationLimits"),
                new GUIContent("Enable Leg Rotation Limits", HipPinningLogicTooltips.EnableLegRotationLimits),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableConstrainedMovement"),
                new GUIContent("Enable Constrained Movement", HipPinningLogicTooltips.EnableConstrainedMovement),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableApplyTransformations"),
                new GUIContent("Enable Transformations", HipPinningLogicTooltips.EnableApplyTransformations),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableHipPinningHeightAdjustment"),
                new GUIContent("Enable Hip Pinning Height Adjustment", HipPinningLogicTooltips.HipPinningHeightAdjustment),
                GUILayout.Height(20));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_enableHipPinningLeave"),
                new GUIContent("Enable Hip Pinning Detection", HipPinningLogicTooltips.HipPinningLeave),
                GUILayout.Height(20));
            if (script.EnableHipPinningLeave)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("_hipPinningLeaveRange"),
                    new GUIContent("Hip Pinning Leave Range: ", HipPinningLogicTooltips.HipPinningLeaveRange),
                    GUILayout.Height(20));
            }

            if (_hipPinningTargetProp.arraySize > 0 && script.HipPinProperties.BodyJointProperties.Count > 0 &&
                script.HipPinProperties.HipBodyJointProperties != null)
            {
                if (GUILayout.Button("Initialize Hip Pinning"))
                {
                    script.InitializeHipPinning((HipPinningTarget)_hipPinningTargetProp.GetArrayElementAtIndex(0).objectReferenceValue);
                    SaveHipPinningOffsets(script);
                    ApplyChanges();
                }
                if (GUILayout.Button("Save Hip Pinning Offsets"))
                {
                    SaveHipPinningOffsets(script);
                    ApplyChanges();
                }

                GUILayout.Space(10);
                if (script.HipPinProperties.PositionConstraints.Count > 0 && script.HipPinProperties.PositionConstraints[0] != null)
                {
                    EditorGUILayout.HelpBox($"Constraints are currently {(script.HipPinProperties.PositionConstraints[0].constraintActive ? "active" : "inactive")}.",
                        script.HipPinProperties.PositionConstraints[0].constraintActive ? MessageType.Warning : MessageType.Info);
                    if (GUILayout.Button("Activate Hip Pinning"))
                    {
                        script.SetHipPinningActive(true);
                    }
                    if (GUILayout.Button("Deactivate Hip Pinning"))
                    {
                        script.SetHipPinningActive(false);
                    }
                    GUILayout.Space(10);
                }
            }
            if (GUILayout.Button("Assign Missing Joints"))
            {
                AssignMissingJoints(script);
                ApplyChanges();
                return;
            }
            if (GUILayout.Button("Remove Constraints"))
            {
                RemoveConstraints(script);
                ApplyChanges();
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Hip Properties:");
            CreateHipPropertiesUI(script);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
