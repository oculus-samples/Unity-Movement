// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;

namespace Oculus.Movement.AnimationRigging
{
    public interface IRetargetingProcessor
    {
        /// <summary>
        /// Setup the retargeting processor; this should only be run once.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        public void SetupRetargetingProcessor(RetargetingLayer retargetingLayer);

        /// <summary>
        /// Prepare the retargeting processor; this is run after retargeting, but before any processors have run.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="ovrBones">The tracked OVR bones.</param>
        public void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones);

        /// <summary>
        /// Processes a <see cref="RetargetingLayer"/>; this is run after retargeting and in order of processors.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="ovrBones">The tracked OVR bones.</param>
        public void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones);

        /// <summary>
        /// Allow drawing debugging gizmos.
        /// </summary>
        public void DrawGizmos();
    }
}
