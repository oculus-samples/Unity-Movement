// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Tracking;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Take screenshots of the specified blendshape mapping.
    /// </summary>
    public class ScreenshotFaceExpressionsCapture : MonoBehaviour
    {
        /// <summary>
        /// All face components to drive while capturing screenshots.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.CorrectivesFaceComponents)]
        protected CorrectivesFace[] _correctivesFaceComponents;

        /// <summary>
        /// The camera which its viewport will be used to take the screenshot.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.Camera)]
        protected Camera _camera;

        /// <summary>
        /// The width of the screenshot texture.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.ScreenshotWidth)]
        protected int _screenshotWidth = 512;

        /// <summary>
        /// The height of the screenshot texture.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.ScreenshotHeight)]
        protected int _screenshotHeight = 512;

        /// <summary>
        /// If true, take a screenshot of the viewport without any blendshapes.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.ScreenshotNeutral)]
        protected bool _screenshotNeutral = true;

        /// <summary>
        /// The path to the screenshots folder.
        /// </summary>
        [SerializeField]
        [Tooltip(ScreenshotFaceExpressionsCaptureTooltips.ScreenshotFolder)]
        protected string _screenshotFolder = "Screenshots";

        private void Awake()
        {
            Assert.IsTrue(_correctivesFaceComponents != null &&
                _correctivesFaceComponents.Length > 0);
            foreach (var correctiveComponent in _correctivesFaceComponents)
            {
                Assert.IsNotNull(correctiveComponent);
            }
            Assert.IsNotNull(_camera);
        }

        /// <summary>
        /// Start the coroutine to take screenshots of all of the face expressions.
        /// </summary>
        public void StartTakingFaceExpressionScreenshots()
        {
            StartCoroutine(TakeFaceExpressionScreenshots());
        }

        private IEnumerator TakeFaceExpressionScreenshots()
        {
            if (_screenshotNeutral)
            {
                CaptureAndSaveScreenshot("XR_Face_Expression_Neutral", ".png");
            }
            SetCorrectivesFreezeState(true);
            for (int i = 0; i < (int)OVRFaceExpressions.FaceExpression.Max; i++)
            {
                var faceExpression = (OVRFaceExpressions.FaceExpression)i;
                ActiveFaceExpressionOnAllFaceComponents(faceExpression);
                yield return null;

                var faceExpressionName = "XR_Face_Expression_" + faceExpression;
                Debug.Log($"Taking screenshot {faceExpressionName}.");
                CaptureAndSaveScreenshot(faceExpressionName, ".png");
            }
            SetCorrectivesFreezeState(false);
        }

        private void SetCorrectivesFreezeState(bool freeze)
        {
            foreach (var face in _correctivesFaceComponents)
            {
                face.FreezeExpressionWeights = freeze;
            }
        }

        private void ActiveFaceExpressionOnAllFaceComponents(OVRFaceExpressions.FaceExpression faceExpression)
        {
            foreach (var face in _correctivesFaceComponents)
            {
                face.ActivateFaceExpression(faceExpression);
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
            File.WriteAllBytes(Path.Combine(containingFolder,
                $"{imageName}{imageFormat}"), screenshotBytes);
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
