// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.FaceTracking.Samples;
using Object = UnityEngine.Object;
using System;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.FaceTracking.Samples.FaceDriver;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Has functions that allow adding components via the editor or runtime.
    /// These functions detect if they are being called in the editor and if so,
    /// affect the undo state.
    /// </summary>
    public class AddComponentsHelper
    {
        /// <summary>
        /// Prefix for movement samples one-click menus.
        /// </summary>
        public const string _MOVEMENT_SAMPLES_MENU =
            "GameObject/Movement SDK/";

        /// <summary>
        /// Sets up a character for A2E face tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to add to.</param>
        /// <param name="allowDuplicates">Allow duplicates mapping or not.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void SetUpCharacterForA2EFace(
            GameObject gameObject,
            bool allowDuplicates = true,
            bool runtimeInvocation = false)
        {
            try
            {
                ValidateChildGameObjectsForFaceMapping(gameObject);
            }
            catch (InvalidOperationException e)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    Debug.LogWarning($"Face tracking setup error: {e.Message}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                }
#else
                Debug.LogWarning($"Face tracking setup error: {e.Message}");
#endif
                return;
            }

            // Get reference for source weights provider.
            var ovrWeightsProvider = EnsureProperOVRWeightsProvider(runtimeInvocation);
            // Get reference for retargeter component.
            var faceRetargeter = EnsureFaceRetargeterComponent(
                gameObject, ovrWeightsProvider, runtimeInvocation, "retargeting_vr_to_aura_a2e_v9.json");
            // ensure final component.
            EnsureFaceDriverComponent(gameObject, faceRetargeter, runtimeInvocation, RigType.XRTech);
        }

        /// <summary>
        /// Sets up an ARKit character for A2E face tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to add to.</param>
        /// <param name="allowDuplicates">Allow duplicates mapping or not.</param>
        /// <param name="runtimeInvocation">If activated from runtime code. We want to possibly
        /// support one-click during playmode, so we can't necessarily use Application.isPlaying.</param>
        public static void SetUpCharacterForA2EARKitFace(GameObject gameObject,
            bool allowDuplicates = true,
            bool runtimeInvocation = false)
        {
            try
            {
                AddComponentsHelper.ValidateChildGameObjectsForFaceMapping(gameObject);
            }
            catch (InvalidOperationException e)
            {
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    Debug.LogWarning($"Face tracking setup error: {e.Message}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Face Tracking setup error.", e.Message, "Ok");
                }
#else
                Debug.LogWarning($"Face tracking setup error: {e.Message}");
#endif
                return;
            }

            // Get reference for source weights provider.
            var ovrWeightsProvider = EnsureProperOVRWeightsProvider(runtimeInvocation);
            // Get reference for retargeter component.
            var faceRetargeter = EnsureFaceRetargeterComponent(
                gameObject, ovrWeightsProvider, runtimeInvocation, "arkit_retarget_a2e_v10.json");
            // ensure final component.
            EnsureFaceDriverComponent(gameObject, faceRetargeter, runtimeInvocation, RigType.Simple);
        }

        private static OVRWeightsProvider EnsureProperOVRWeightsProvider(bool runtimeInvocation)
        {
            OVRWeightsProvider ovrWeightsProvider = GameObject.FindFirstObjectByType<OVRWeightsProvider>();

            if (ovrWeightsProvider == null)
            {
                GameObject ovrExpressionsProviderObject = new GameObject("OVRExpressionsProvider");
                ovrWeightsProvider =
                    ovrExpressionsProviderObject.AddComponent<OVRWeightsProvider>();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RegisterCreatedObjectUndo(ovrExpressionsProviderObject, "Create OVRExpressionsProvider object");
                    Undo.RegisterCompleteObjectUndo(ovrExpressionsProviderObject, "OVRExpressionsProvider init");
                }
#endif
            }

            if (ovrWeightsProvider.OVRFaceExpressionComp == null)
            {
                OVRFaceExpressions ovrFaceExpressions = GameObject.FindFirstObjectByType<OVRFaceExpressions>();
                if (ovrFaceExpressions == null)
                {
                    ovrFaceExpressions = ovrWeightsProvider.gameObject.AddComponent<OVRFaceExpressions>();
#if UNITY_EDITOR
                    if (!runtimeInvocation)
                    {
                        Undo.RegisterCreatedObjectUndo(ovrFaceExpressions, "Add OVRFaceExpressions");
                    }
#endif
                }

                ovrWeightsProvider.OVRFaceExpressionComp = ovrFaceExpressions;
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(ovrWeightsProvider, "Assign to OVRFaceExpressionComp field");
                }
#endif
            }

            return ovrWeightsProvider;
        }

        private static FaceRetargeterComponent EnsureFaceRetargeterComponent(
            GameObject parentObject,
            OVRWeightsProvider ovrWeightsProvider,
            bool runtimeInvocation,
            string presetName)
        {
            FaceRetargeterComponent retargeterComponent = parentObject.GetComponentInChildren<FaceRetargeterComponent>();

            if (retargeterComponent == null)
            {
                GameObject retargeterObject = new GameObject("FaceRetargeter");
                retargeterComponent =
                    retargeterObject.AddComponent<FaceRetargeterComponent>();
#if UNITY_EDITOR
                if (runtimeInvocation)
                {
                    retargeterObject.transform.SetParent(parentObject.transform);
                }
                else
                {
                    Undo.RegisterCreatedObjectUndo(retargeterObject, "Create RetargeterComponent object");
                    Undo.SetTransformParent(retargeterObject.transform, parentObject.transform, "Add face retargeter component to parent");
                    Undo.RegisterCompleteObjectUndo(retargeterObject, "RetargeterComponent init");
                }
#else
                retargeterObject.transform.SetParent(parentObject.transform);
#endif
            }

            if (retargeterComponent.RetargeterConfig == null)
            {
                // This code can only be done via the editor. At runtime the user
                // will have to man
#if UNITY_EDITOR
                var packagesPath = Path.Combine(new string[] {
                    "Packages", "com.meta.xr.sdk.movement", "Shared", "Data", "TrackingPresets"});
                var presetPath = Path.Combine(packagesPath, presetName);
                var textAsset = AssetDatabase.LoadAssetAtPath(presetPath, typeof(TextAsset));
                Assert.IsNotNull(textAsset, "Config text asset should exist.");
                retargeterComponent.RetargeterConfig = textAsset as TextAsset;
#endif

#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(retargeterComponent, "Assign text asset field");
                }
#endif
            }

            if (retargeterComponent.WeightsProvider == null)
            {
                retargeterComponent.WeightsProvider = ovrWeightsProvider;
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(ovrWeightsProvider, "Assign source weights provider");
                }
#endif
            }

            return retargeterComponent;
        }

        public static void EnsureFaceDriverComponent(
            GameObject parentObject,
            FaceRetargeterComponent faceRetargeter,
            bool runtimeInvocation,
            RigType rigType)
        {
            FaceDriver faceDriver = parentObject.GetComponent<FaceDriver>();

            if (faceDriver == null)
            {
                faceDriver = parentObject.GetComponent<FaceDriver>();
                if (faceDriver == null)
                {
                    faceDriver = parentObject.AddComponent<FaceDriver>();
#if UNITY_EDITOR
                    if (!runtimeInvocation)
                    {
                        Undo.RegisterCreatedObjectUndo(faceDriver, "Add FaceDriver");
                    }
#endif
                }
            }

            if (faceDriver.Meshes == null || faceDriver.Meshes.Length == 0)
            {
                var skinnedMeshRenderers = parentObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                List<SkinnedMeshRenderer> blendshapeMeshRenders = new List<SkinnedMeshRenderer>();
                foreach (var mesh in skinnedMeshRenderers)
                {
                    if (mesh.sharedMesh.blendShapeCount > 0)
                    {
                        blendshapeMeshRenders.Add(mesh);
                    }
                }

                faceDriver.Meshes = blendshapeMeshRenders.ToArray();
#if UNITY_EDITOR
                if (!runtimeInvocation)
                {
                    Undo.RecordObject(faceDriver, "Assign skinned mesh renderers");
                }
#endif
            }

            faceDriver.WeightsProvider = faceRetargeter;
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RecordObject(faceDriver, "Assign face retargeter as weights provider");
            }
#endif

            faceDriver.RigTypeValue = rigType;
#if UNITY_EDITOR
            if (!runtimeInvocation)
            {
                Undo.RecordObject(faceDriver, "Assign rig type to facedriver");
            }
#endif
        }

        public static void ValidateChildGameObjectsForFaceMapping(GameObject gameObject)
        {
            var childRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (childRenderers == null || childRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot add FaceTracking components to a GameObject that does not contain " +
                    $"{nameof(SkinnedMeshRenderer)}s");
            }
            bool foundBlendshapes = false;
            foreach (var childRenderer in childRenderers)
            {
                if (childRenderer.sharedMesh != null &&
                    childRenderer.sharedMesh.blendShapeCount > 0)
                {
                    foundBlendshapes = true;
                }
            }

            if (!foundBlendshapes)
            {
                throw new InvalidOperationException(
                    $"Adding a FaceTracking component requires a {nameof(SkinnedMeshRenderer)} " +
                    $"that contains blendshapes.");
            }
        }
    }
}
