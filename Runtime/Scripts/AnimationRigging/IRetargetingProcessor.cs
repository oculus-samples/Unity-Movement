// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Unity.Jobs;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for retargeting processors.
    /// </summary>
    public interface IRetargetingProcessor
    {
        /// <summary>
        /// Allows clean up of resources used.
        /// </summary>
        public void CleanUp();

        /// <summary>
        /// Responds to calibration event.
        /// </summary>
        /// <param name="retargetingLayer">Retargeting layer component.</param>
        /// <param name="ovrBones">Body tracking bones.</param>
        public void RespondToCalibration(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones);

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
        /// Process retargeting layer, job version.
        /// </summary>
        /// <param name="previousJob">Previous job, if any.</param>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="ovrBones">The body tracking bones.</param>
        /// <returns>Handle of job created.</returns>
        public JobHandle ProcessRetargetingLayerJob(JobHandle? previousJob, RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones);

        /// <summary>
        /// Allow drawing debugging gizmos.
        /// </summary>
        public void DrawGizmos();

        /// <summary>
        /// Read JSON config from file.
        /// </summary>
        /// <param name="filePath">File path to read from.</param>
        public void ReadJSONConfigFromFile(string filePath);

        /// <summary>
        /// Processor type (normal or jobs).
        /// </summary>
        public enum RetargetingProcessorType { Normal = 0, Jobs }

        /// <summary>
        /// Processor type field.
        /// </summary>
        public RetargetingProcessorType ProcessorType { get; set; }
    }
}
