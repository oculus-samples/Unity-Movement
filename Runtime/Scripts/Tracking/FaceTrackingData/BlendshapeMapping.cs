// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Defines all supported blendshapes for face tracking.
    /// </summary>
    public class BlendshapeMapping : MonoBehaviour
    {
        /// <summary>
        /// List of supported eye look blendshapes.
        /// </summary>
        public static readonly OVRFaceExpressions.FaceExpression[] EyeLookBlendshapes =
        {
            OVRFaceExpressions.FaceExpression.EyesLookDownL,
            OVRFaceExpressions.FaceExpression.EyesLookDownR,
            OVRFaceExpressions.FaceExpression.EyesLookLeftL,
            OVRFaceExpressions.FaceExpression.EyesLookLeftR,
            OVRFaceExpressions.FaceExpression.EyesLookRightL,
            OVRFaceExpressions.FaceExpression.EyesLookRightR,
            OVRFaceExpressions.FaceExpression.EyesLookUpL,
            OVRFaceExpressions.FaceExpression.EyesLookUpR,
        };

        /// <summary>
        /// List of supported eye closed blendshapes.
        /// </summary>
        public static readonly OVRFaceExpressions.FaceExpression[] EyesClosedBlendshapes =
        {
            OVRFaceExpressions.FaceExpression.EyesClosedL,
            OVRFaceExpressions.FaceExpression.EyesClosedR,
        };

        /// <summary>
        /// List of supported upper face blendshapes.
        /// </summary>
        public static readonly OVRFaceExpressions.FaceExpression[] UpperFaceBlendshapes =
        {
            OVRFaceExpressions.FaceExpression.BrowLowererL,
            OVRFaceExpressions.FaceExpression.BrowLowererR,
            OVRFaceExpressions.FaceExpression.CheekRaiserL,
            OVRFaceExpressions.FaceExpression.CheekRaiserR,
            OVRFaceExpressions.FaceExpression.EyesLookDownL,
            OVRFaceExpressions.FaceExpression.EyesLookDownR,
            OVRFaceExpressions.FaceExpression.EyesLookLeftL,
            OVRFaceExpressions.FaceExpression.EyesLookLeftR,
            OVRFaceExpressions.FaceExpression.EyesLookRightL,
            OVRFaceExpressions.FaceExpression.EyesLookRightR,
            OVRFaceExpressions.FaceExpression.EyesLookUpL,
            OVRFaceExpressions.FaceExpression.EyesLookUpR,
            OVRFaceExpressions.FaceExpression.InnerBrowRaiserL,
            OVRFaceExpressions.FaceExpression.InnerBrowRaiserR,
            OVRFaceExpressions.FaceExpression.NoseWrinklerL,
            OVRFaceExpressions.FaceExpression.NoseWrinklerR,
            OVRFaceExpressions.FaceExpression.OuterBrowRaiserL,
            OVRFaceExpressions.FaceExpression.OuterBrowRaiserR,
            OVRFaceExpressions.FaceExpression.UpperLidRaiserL,
            OVRFaceExpressions.FaceExpression.UpperLidRaiserR,
        };

        /// <summary>
        /// List of supported lower face blendshapes.
        /// </summary>
        public static readonly OVRFaceExpressions.FaceExpression[] LowerFaceBlendshapes =
        {
            OVRFaceExpressions.FaceExpression.CheekPuffL,
            OVRFaceExpressions.FaceExpression.CheekPuffR,
            OVRFaceExpressions.FaceExpression.CheekSuckL,
            OVRFaceExpressions.FaceExpression.CheekSuckR,
            OVRFaceExpressions.FaceExpression.ChinRaiserB,
            OVRFaceExpressions.FaceExpression.ChinRaiserT,
            OVRFaceExpressions.FaceExpression.DimplerL,
            OVRFaceExpressions.FaceExpression.DimplerR,
            OVRFaceExpressions.FaceExpression.JawDrop,
            OVRFaceExpressions.FaceExpression.JawSidewaysLeft,
            OVRFaceExpressions.FaceExpression.JawSidewaysRight,
            OVRFaceExpressions.FaceExpression.JawThrust,
            OVRFaceExpressions.FaceExpression.LidTightenerL,
            OVRFaceExpressions.FaceExpression.LidTightenerR,
            OVRFaceExpressions.FaceExpression.LipCornerDepressorL,
            OVRFaceExpressions.FaceExpression.LipCornerDepressorR,
            OVRFaceExpressions.FaceExpression.LipCornerPullerL,
            OVRFaceExpressions.FaceExpression.LipCornerPullerR,
            OVRFaceExpressions.FaceExpression.LipFunnelerLB,
            OVRFaceExpressions.FaceExpression.LipFunnelerLT,
            OVRFaceExpressions.FaceExpression.LipFunnelerRB,
            OVRFaceExpressions.FaceExpression.LipFunnelerRT,
            OVRFaceExpressions.FaceExpression.LipPressorL,
            OVRFaceExpressions.FaceExpression.LipPressorR,
            OVRFaceExpressions.FaceExpression.LipPuckerL,
            OVRFaceExpressions.FaceExpression.LipPuckerR,
            OVRFaceExpressions.FaceExpression.LipsToward,
            OVRFaceExpressions.FaceExpression.LipStretcherL,
            OVRFaceExpressions.FaceExpression.LipStretcherR,
            OVRFaceExpressions.FaceExpression.LipSuckLB,
            OVRFaceExpressions.FaceExpression.LipSuckLT,
            OVRFaceExpressions.FaceExpression.LipSuckRB,
            OVRFaceExpressions.FaceExpression.LipSuckRT,
            OVRFaceExpressions.FaceExpression.LipTightenerL,
            OVRFaceExpressions.FaceExpression.LipTightenerR,
            OVRFaceExpressions.FaceExpression.LowerLipDepressorL,
            OVRFaceExpressions.FaceExpression.LowerLipDepressorR,
            OVRFaceExpressions.FaceExpression.MouthLeft,
            OVRFaceExpressions.FaceExpression.MouthRight,
            OVRFaceExpressions.FaceExpression.UpperLipRaiserL,
            OVRFaceExpressions.FaceExpression.UpperLipRaiserR,
        };

        /// <summary>
        /// Defines possible blendshapes on a skinned mesh renderer.
        /// </summary>
        [System.Serializable]
        public class MeshMapping
        {
            /// <summary>
            /// The skinned mesh renderer that has blendshapes.
            /// </summary>
            [Tooltip(BlendshapeMappingTooltips.MeshMappingTooltips.Mesh)]
            public SkinnedMeshRenderer Mesh;

            /// <summary>
            /// List of all supported blendshapes on this skinned mesh renderer.
            /// </summary>
            [Tooltip(BlendshapeMappingTooltips.MeshMappingTooltips.Blendshapes)]
            public List<OVRFaceExpressions.FaceExpression> Blendshapes =
                new List<OVRFaceExpressions.FaceExpression>();
        }

        /// <summary>
        /// List of all mesh mappings - supported blendshapes on meshes on this character.
        /// </summary>
        [Tooltip(BlendshapeMappingTooltips.Meshes)]
        public List<MeshMapping> Meshes = new List<MeshMapping>();
    }
}
