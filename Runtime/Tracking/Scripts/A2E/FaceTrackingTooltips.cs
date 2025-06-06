// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.


namespace Oculus.Movement
{
    public static class FaceTrackingTooltips
    {
        public static class OVRWeightsProviderTooltips
        {
            public const string OvrFaceExpressions =
                "The face expressions provider to source from.";
        }

        public static class RetargeterComponentTooltips
        {
            public const string RetargeterConfig =
                "Retargeter config JSON.";

            public const string RetargeterConfigOverride =
                "\"Override filename loaded from application's persistent path (i.e., " +
                "uploaded to headset separately from the app). " +
                "If not present, the TextAsset config will be used.\"";

            public const string WeightsProvider =
                "The source weights provide to provide to the input mapper.";
        }

        public static class FaceDriverTooltips
        {
            public const string Meshes =
                "Meshes to animate.";

            public const string WeightsProvider =
                "The weights provider that drives the deformation.";

            public const string RigType =
                "Character's rig type.";
        }
    }
}
