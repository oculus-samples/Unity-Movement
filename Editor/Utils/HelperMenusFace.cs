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

        private const string _A2E_FACE_MENU =
            "A2E Face";
        private const string _A2E_ARKIT_FACE_MENU =
            "A2E ARKit Face";

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU +
            _A2E_FACE_MENU)]
        private static void SetupCharacterForA2EFace()
        {
            AddComponentsHelper.SetUpCharacterForA2EFace(Selection.activeGameObject, false);
        }

        [MenuItem(AddComponentsHelper._MOVEMENT_SAMPLES_MENU + _MOVEMENT_SAMPLES_FT_MENU +
            _A2E_ARKIT_FACE_MENU)]
        private static void SetupCharacterForA2EARKitFace()
        {
            AddComponentsHelper.SetUpCharacterForA2EARKitFace(Selection.activeGameObject, false);
        }
    }
}
