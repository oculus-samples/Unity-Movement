// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Version of Correctives mapped to ARKit blend shapes,
    /// via "custom" mapping.
    /// </summary>
    public class ARKitFace : CorrectivesFace
    {
        private static (string, OVRFaceExpressions.FaceExpression)[] ARKitBlendshapesSorted =
            {
            ("EyeBlinkLeft", OVRFaceExpressions.FaceExpression.EyesClosedL),
            ("EyeLookDownLeft", OVRFaceExpressions.FaceExpression.EyesLookDownL),
            ("EyeLookInLeft", OVRFaceExpressions.FaceExpression.EyesLookRightL),
            ("EyeLookOutLeft", OVRFaceExpressions.FaceExpression.EyesLookLeftL),
            ("EyeLookUpLeft", OVRFaceExpressions.FaceExpression.EyesLookUpL),
            ("EyeSquintLeft", OVRFaceExpressions.FaceExpression.LidTightenerL),
            ("EyeWideLeft", OVRFaceExpressions.FaceExpression.UpperLidRaiserL),
            ("EyeBlinkRight", OVRFaceExpressions.FaceExpression.EyesClosedR),
            ("EyeLookDownRight", OVRFaceExpressions.FaceExpression.EyesLookDownR),
            ("EyeLookInRight", OVRFaceExpressions.FaceExpression.EyesLookLeftR),
            ("EyeLookOutRight", OVRFaceExpressions.FaceExpression.EyesLookRightR),
            ("EyeLookUpRight", OVRFaceExpressions.FaceExpression.EyesLookUpR),
            ("EyeSquintRight", OVRFaceExpressions.FaceExpression.LidTightenerR),
            ("EyeWideRight", OVRFaceExpressions.FaceExpression.UpperLidRaiserR),
            ("JawForward", OVRFaceExpressions.FaceExpression.JawThrust),
            ("JawLeft", OVRFaceExpressions.FaceExpression.JawSidewaysLeft),
            ("JawRight", OVRFaceExpressions.FaceExpression.JawSidewaysRight),
            ("JawOpen", OVRFaceExpressions.FaceExpression.JawDrop),
            ("MouthClose", OVRFaceExpressions.FaceExpression.LipsToward),
            ("MouthFunnel", OVRFaceExpressions.FaceExpression.LipFunnelerLB),
            ("MouthPucker", OVRFaceExpressions.FaceExpression.LipPuckerL),
            ("MouthLeft", OVRFaceExpressions.FaceExpression.MouthLeft),
            ("MouthRight", OVRFaceExpressions.FaceExpression.MouthRight),
            ("MouthSmileLeft", OVRFaceExpressions.FaceExpression.LipCornerPullerL),
            ("MouthSmileRight", OVRFaceExpressions.FaceExpression.LipCornerPullerR),
            ("MouthFrownLeft", OVRFaceExpressions.FaceExpression.LipCornerDepressorL),
            ("MouthFrownRight", OVRFaceExpressions.FaceExpression.LipCornerDepressorR),
            ("MouthDimpleLeft", OVRFaceExpressions.FaceExpression.DimplerL),
            ("MouthDimpleRight", OVRFaceExpressions.FaceExpression.DimplerR),
            ("MouthStretchLeft", OVRFaceExpressions.FaceExpression.LipStretcherL),
            ("MouthStretchRight", OVRFaceExpressions.FaceExpression.LipStretcherR),
            ("MouthRollLower", OVRFaceExpressions.FaceExpression.LipSuckLB),
            ("MouthRollUpper", OVRFaceExpressions.FaceExpression.LipSuckLT),
            ("MouthShrugLower", OVRFaceExpressions.FaceExpression.ChinRaiserB),
            ("MouthShrugUpper", OVRFaceExpressions.FaceExpression.ChinRaiserT),
            ("MouthPressLeft", OVRFaceExpressions.FaceExpression.LipPressorL),
            ("MouthPressRight", OVRFaceExpressions.FaceExpression.LipPressorR),
            ("MouthLowerDownLeft", OVRFaceExpressions.FaceExpression.LowerLipDepressorL),
            ("MouthLowerDownRight", OVRFaceExpressions.FaceExpression.LowerLipDepressorR),
            ("MouthUpperUpLeft", OVRFaceExpressions.FaceExpression.UpperLipRaiserL),
            ("MouthUpperUpRight", OVRFaceExpressions.FaceExpression.UpperLipRaiserR),
            ("BrowDownLeft", OVRFaceExpressions.FaceExpression.BrowLowererL),
            ("BrowDownRight", OVRFaceExpressions.FaceExpression.BrowLowererR),
            ("BrowInnerUp", OVRFaceExpressions.FaceExpression.InnerBrowRaiserL),
            ("BrowOuterUpLeft", OVRFaceExpressions.FaceExpression.OuterBrowRaiserL),
            ("BrowOuterUpRight", OVRFaceExpressions.FaceExpression.OuterBrowRaiserR),
            ("CheekPuff", OVRFaceExpressions.FaceExpression.CheekPuffL),
            ("CheekSquintLeft", OVRFaceExpressions.FaceExpression.CheekRaiserL),
            ("CheekSquintRight", OVRFaceExpressions.FaceExpression.CheekRaiserR),
            ("NoseSneerLeft", OVRFaceExpressions.FaceExpression.NoseWrinklerL),
            ("NoseSneerRight", OVRFaceExpressions.FaceExpression.NoseWrinklerR)
        };

        /// <summary>
        /// Defines ARKit blend shape name and face expression pair mappings.
        /// </summary>
        /// <returns>Two arrays, each relating a blend shape name with a face expression pair.</returns>
        protected override (string[], OVRFaceExpressions.FaceExpression[]) GetCustomBlendShapeNameAndExpressionPairs()
        {
            string[] arKitBlendShapeNames = new string[ARKitBlendshapesSorted.Length];
            OVRFaceExpressions.FaceExpression[] arKitFaceExpressions =
                new OVRFaceExpressions.FaceExpression[ARKitBlendshapesSorted.Length];
            for (int i = 0; i < ARKitBlendshapesSorted.Length; i++)
            {
                arKitBlendShapeNames[i] = ARKitBlendshapesSorted[i].Item1;
                arKitFaceExpressions[i] = ARKitBlendshapesSorted[i].Item2;
            }
            return (arKitBlendShapeNames, arKitFaceExpressions);
        }
    }
}
