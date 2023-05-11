// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Tracking.Deprecated;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Take screenshots of the specified blendshape mapping.
    /// </summary>
    public class ScreenshotFaceExpressions : MonoBehaviour
    {
        /// <summary>
        /// The camera which its viewport will be used to take the screenshot.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.Camera)]
        protected Camera _camera;

        /// <summary>
        /// The target blendshape mapping.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.Mapping)]
        protected BlendshapeMapping _mapping;

        /// <summary>
        /// The target corrective shapes driver.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.Correctives)]
        protected CorrectiveShapesDriver _correctives;

        /// <summary>
        /// The width of the screenshot texture.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.ScreenshotWidth)]
        protected int _screenshotWidth = 512;

        /// <summary>
        /// The height of the screenshot texture.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.ScreenshotHeight)]
        protected int _screenshotHeight = 512;

        /// <summary>
        /// If true, take a screenshot of the viewport without any blendshapes.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.ScreenshotNeutral)]
        protected bool _screenshotNeutral = true;

        /// <summary>
        /// The path to the screenshots folder.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsTooltips.ScreenshotFolder)]
        protected string _screenshotFolder = "Screenshots";

        /// <summary>
        /// Start the coroutine to take screenshots of all of the blendshapes.
        /// </summary>
        public void StartTakingBlendshapeScreenshots()
        {
            StartCoroutine(TakeBlendshapeScreenshots());
        }

        private IEnumerator TakeBlendshapeScreenshots()
        {
            if (_screenshotNeutral)
            {
                CaptureAndSaveScreenshot("XR_Face_Expression_Neutral", ".png");
            }
            foreach (var mesh in _mapping.Meshes)
            {
                foreach (var blendshape in mesh.Blendshapes)
                {
                    if (blendshape != OVRFaceExpressions.FaceExpression.Max)
                    {
                        UpdateBlendshapes(_mapping.Meshes, blendshape);
                        _correctives.ApplyCorrectives();
                        yield return null;

                        var blendshapeName = "XR_Face_Expression_" + (OVRPlugin.FaceExpression)blendshape;
                        CaptureAndSaveScreenshot(blendshapeName, ".png");
                    }
                }
            }
        }

        private void CaptureAndSaveScreenshot(string imageName, string imageFormat)
        {
            var containingFolder = Path.Combine(Application.dataPath, _screenshotFolder);
            if (!Directory.Exists(containingFolder))
            {
                Directory.CreateDirectory(containingFolder);
            }
            var screenshot = CaptureScreenshot(_screenshotWidth, _screenshotHeight, _camera);
            var screenshotBytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(System.IO.Path.Combine(containingFolder,
                $"{imageName}{imageFormat}"), screenshotBytes);
        }

        private void UpdateBlendshapes(List<BlendshapeMapping.MeshMapping> meshes,
                                        OVRFaceExpressions.FaceExpression targetBlendshape)
        {
            foreach (var mesh in meshes)
            {
                var skinnedMeshRenderer = mesh.Mesh;
                int numBlendshapes = Mathf.Min(mesh.Blendshapes.Count,
                    skinnedMeshRenderer.sharedMesh.blendShapeCount);
                for (int blendShapeIndex = 0; blendShapeIndex < numBlendshapes; ++blendShapeIndex)
                {
                    var blendShapeToFaceExpression = mesh.Blendshapes[blendShapeIndex];
                    if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.Max)
                    {
                        continue;
                    }
                    if (blendShapeToFaceExpression == targetBlendshape)
                    {
                        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 100.0f);
                    }
                    else
                    {
                        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 0.0f);
                    }
                }
            }
        }

        private static Texture2D CaptureScreenshot(int width, int height, Camera cam)
        {
            var renderTexture = new RenderTexture(width, height, 0);
            renderTexture.depth = 24;
            renderTexture.filterMode = FilterMode.Point;
            RenderTexture.active = renderTexture;

            cam.targetTexture = renderTexture;
            cam.Render();

            var outputTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var rect = new Rect(0, 0, width, height);
#if UNITY_EDITOR
            outputTexture.alphaIsTransparency = true;
#endif
            outputTexture.ReadPixels(rect, 0, 0);
            outputTexture.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);

            return outputTexture;
        }
    }
}
