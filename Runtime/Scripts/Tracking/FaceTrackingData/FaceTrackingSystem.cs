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
        /// Corrective shapes driver component.
        /// </summary>
        [SerializeField]
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
        /// If true, the corrective driver will run and apply correctives.
        /// </summary>
        public bool CorrectivesEnabled { get; set; }

        private void Awake()
        {
            Assert.IsNotNull(_blendShapeMapping);
            Assert.IsNotNull(_ovrFaceExpressions);
            Assert.IsNotNull(_correctiveShapesDriver);

            CorrectivesEnabled = true;
        }

        private void Update()
        {
            if (_ovrFaceExpressions.enabled &&
                _ovrFaceExpressions.FaceTrackingEnabled &&
                _ovrFaceExpressions.ValidExpressions)
            {
                UpdateAllMeshesUsingFaceTracking();
            }

            if (CorrectivesEnabled)
            {
                _correctiveShapesDriver.ApplyCorrectives();
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
                    float currentWeight = _ovrFaceExpressions[blendShapeToFaceExpression];

                    // Recover true eyes closed values
                    if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.EyesClosedL)
                    {
                        currentWeight += _ovrFaceExpressions[OVRFaceExpressions.FaceExpression.EyesLookDownL];
                    }
                    else if (blendShapeToFaceExpression == OVRFaceExpressions.FaceExpression.EyesClosedR)
                    {
                        currentWeight += _ovrFaceExpressions[OVRFaceExpressions.FaceExpression.EyesLookDownR];
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
