// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Tracking;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Helper menus for face face tracking.
    /// </summary>
    internal static class HelperMenusFace
    {
        private const string _MOVEMENT_SAMPLES_FT_MENU =
            "Face Tracking/";
        private const string _CORRECTIVES_FACE_MENU =
            "Correctives Face";
        private const string _ARKIT_FACE_MENU =
            "ARKit Face";
        private const string _NO_DUPLICATES_SUFFIX =
            " (duplicate mapping off)";

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU +
            _CORRECTIVES_FACE_MENU)]
        private static void SetupCharacterForCorrectivesFace()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForCorrectivesFace(activeGameObject);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU +
            _CORRECTIVES_FACE_MENU + _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForCorrectivesFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForCorrectivesFace(activeGameObject, false);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU)]
        private static void SetupCharacterForARKitFace()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForARKitFace(activeGameObject);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU
            + _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForARKitFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            try
            {
                ValidateGameObjectForFaceMapping(activeGameObject);
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                return;
            }

            SetUpCharacterForARKitFace(activeGameObject, false);
        }

        /// <summary>
        /// Validates GameObject for face mapping.
        /// </summary>
        /// <param name="go">GameObject to check.</param>
        /// <exception cref="InvalidOperationException">Exception thrown if GameObject fails check.</exception>
        public static void ValidateGameObjectForFaceMapping(GameObject go)
        {
            var renderer = go.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
            {
                throw new InvalidOperationException(
                    $"Adding a Face Tracking component requires a {nameof(SkinnedMeshRenderer)} " +
                    $"that contains blendshapes.");
            }
        }

        private static void SetUpCharacterForCorrectivesFace(GameObject gameObject,
            bool allowDuplicates = true)
        {
            Undo.IncrementCurrentGroup();

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
                Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
            }

            var face = gameObject.GetComponent<CorrectivesFace>();
            if (!face)
            {
                face = gameObject.AddComponent<CorrectivesFace>();
                face.FaceExpressions = faceExpressions;
                Undo.RegisterCreatedObjectUndo(face, "Create CorrectivesFace component");
            }

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
                Undo.RecordObject(face, "Assign to BlendshapeModifier field");
            }

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map Correctives blendshapes");
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for Correctives Tracking");
        }

        private static void SetUpCharacterForARKitFace(GameObject gameObject,
            bool allowDuplicates = true)
        {
            Undo.IncrementCurrentGroup();

            var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
            if (!faceExpressions)
            {
                faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
                Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
            }

            var face = gameObject.GetComponent<ARKitFace>();
            if (!face)
            {
                face = gameObject.AddComponent<ARKitFace>();
                face.FaceExpressions = faceExpressions;
                Undo.RegisterCreatedObjectUndo(face, "Create ARKit component");
            }
            face.RetargetingTypeField = OVRCustomFace.RetargetingType.Custom;

            if (face.BlendshapeModifier == null)
            {
                face.BlendshapeModifier = gameObject.GetComponentInParent<BlendshapeModifier>();
                Undo.RecordObject(face, "Assign to BlendshapeModifier field");
            }

            Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map ARKit blendshapes");
            face.AllowDuplicateMappingField = allowDuplicates;
            face.AutoMapBlendshapes();
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

            Undo.SetCurrentGroupName($"Setup Character for ARKit Tracking");
        }
    }
}
