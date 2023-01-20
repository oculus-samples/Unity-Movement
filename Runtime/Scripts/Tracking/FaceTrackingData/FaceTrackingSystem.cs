// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Updates the blendshapes and eyes on the face with tracking data.
    /// </summary>
    [ExecuteInEditMode]
    public class FaceTrackingSystem : MonoBehaviour
    {
        /// <summary>
        /// Blendshape mapping component.
        /// </summary>
        [SerializeField]
        [Tooltip(FaceTrackingSystemTooltips.BlendshapeMapping)]
        protected BlendshapeMapping _blendShapeMapping;

        /// <summary>
        /// OVR face expressions component.
        /// </summary>
        [SerializeField]
        [Tooltip(FaceTrackingSystemTooltips.OVRFaceExpressions)]
        protected OVRFaceExpressions _ovrFaceExpressions;

        /// <summary>
        /// Optional corrective shapes driver component.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(FaceTrackingSystemTooltips.CorrectiveShapesDriver)]
        protected CorrectiveShapesDriver _correctiveShapesDriver;

        /// <summary>
        /// Optional blendshape modifier component.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(FaceTrackingSystemTooltips.BlendshapeModifier)]
        protected BlendshapeModifier _blendshapeModifier;

        /// <summary>
        /// If true, the correctives driver will apply correctives.
        /// </summary>
        public bool CorrectivesEnabled { get; set; }

        /// <summary>
        /// Last updated expression weights.
        /// </summary>
        public float[] ExpressionWeights { get; private set; }

        /// <summary>
        /// Allows one to freeze current values obtained from facial expressions component.
        /// </summary>
        public bool FreezeExpressionWeights { get; set; }

        private void Awake()
        {
            Assert.IsNotNull(_blendShapeMapping);
            Assert.IsNotNull(_ovrFaceExpressions);
            ExpressionWeights = new float[(int)OVRFaceExpressions.FaceExpression.Max];

            CorrectivesEnabled = true;
        }

        private void Update()
        {
            if (ExpressionWeights == null || ExpressionWeights.Length != (int)OVRFaceExpressions.FaceExpression.Max)
            {
                ExpressionWeights = new float[(int)OVRFaceExpressions.FaceExpression.Max];
            }

            if (_ovrFaceExpressions.enabled &&
                _ovrFaceExpressions.FaceTrackingEnabled &&
                _ovrFaceExpressions.ValidExpressions)
            {
                UpdateExpressionWeights();
                UpdateAllMeshesUsingFaceTracking();
            }

            if (CorrectivesEnabled && _correctiveShapesDriver != null)
            {
                _correctiveShapesDriver.ApplyCorrectives();
            }
        }

        private void UpdateExpressionWeights()
        {
            if (FreezeExpressionWeights)
            {
                return;
            }
            for (var expressionIndex = 0;
                    expressionIndex < (int)OVRFaceExpressions.FaceExpression.Max;
                    ++expressionIndex)
            {
                var blendshape = (OVRFaceExpressions.FaceExpression)expressionIndex;
                ExpressionWeights[expressionIndex] = _ovrFaceExpressions[blendshape];
            }
        }

        private void UpdateAllMeshesUsingFaceTracking()
        {
            foreach (var m in _blendShapeMapping.Meshes)
            {
                UpdateSkinnedMeshUsingFaceTracking(m.Mesh, m.Blendshapes);
            }
        }

        private void UpdateSkinnedMeshUsingFaceTracking(
            SkinnedMeshRenderer renderer,
            List<OVRFaceExpressions.FaceExpression> mapping)
        {
            if (renderer == null)
            {
                return;
            }

            if (renderer.sharedMesh != null)
            {
                int numBlendshapes = Mathf.Min(mapping.Count,
                    renderer.sharedMesh.blendShapeCount);
                for (int blendShapeIndex = 0; blendShapeIndex < numBlendshapes; ++blendShapeIndex)
                {
                    var blendShapeToFaceExpression = mapping[blendShapeIndex];
                    if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.Max)
                    {
                        continue;
                    }
                    float currentWeight = ExpressionWeights[(int)blendShapeToFaceExpression];

                    // Recover true eyes closed values
                    if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.EyesClosedL)
                    {
                        currentWeight += ExpressionWeights[(int)OVRFaceExpressions.FaceExpression.EyesLookDownL];
                    }
                    else if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.EyesClosedR)
                    {
                        currentWeight += ExpressionWeights[(int)OVRFaceExpressions.FaceExpression.EyesLookDownR];
                    }

                    if (_blendshapeModifier != null)
                    {
                        currentWeight = _blendshapeModifier.GetModifiedWeight(blendShapeToFaceExpression, currentWeight);

                    }
                    renderer.SetBlendShapeWeight(blendShapeIndex, currentWeight * 100.0f);
                }
            }
            else
            {
                Debug.LogError("Renderer.sharedMesh is null.");
            }
        }
    }
}
