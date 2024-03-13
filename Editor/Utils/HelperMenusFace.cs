// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

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

            AddComponentsHelper.SetUpCharacterForCorrectivesFace(activeGameObject, true);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU +
            _CORRECTIVES_FACE_MENU + _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForCorrectivesFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            AddComponentsHelper.SetUpCharacterForCorrectivesFace(activeGameObject, false);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU)]
        private static void SetupCharacterForARKitFace()
        {
            var activeGameObject = Selection.activeGameObject;

            AddComponentsHelper.SetUpCharacterForARKitFace(activeGameObject, true);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU + _ARKIT_FACE_MENU
            + _NO_DUPLICATES_SUFFIX)]
        private static void SetupCharacterForARKitFaceNoDuplicates()
        {
            var activeGameObject = Selection.activeGameObject;

            AddComponentsHelper.SetUpCharacterForARKitFace(activeGameObject, false);
        }
    }
}
