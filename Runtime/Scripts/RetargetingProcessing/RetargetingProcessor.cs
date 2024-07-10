// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Jobs;
using UnityEngine;
using static Oculus.Movement.AnimationRigging.IRetargetingProcessor;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Base class for retargeting processors, meant to be used as a scriptable object.
    /// </summary>
    [Serializable]
    public class RetargetingProcessor : ScriptableObject, IRetargetingProcessor
    {
        /// <summary>
        /// The weight of this processor.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Weight = 1.0f;

        /// <summary>
        /// Deep copy data from another processor to this processor.
        /// </summary>
        /// <param name="source">The source processor to copy from.</param>
        public virtual void CopyData(RetargetingProcessor source)
        {
            Weight = source.Weight;
        }

        /// <inheritdoc />
        public virtual void CleanUp()
        {
        }

        /// <inheritdoc />
        public virtual void RespondToCalibration(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public virtual void SetupRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
        }

        /// <inheritdoc />
        public virtual void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public virtual void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public virtual JobHandle ProcessRetargetingLayerJob(JobHandle? previousJob, RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            return new JobHandle();
        }

        /// <inheritdoc />
        public virtual void DrawGizmos()
        {
        }


        /// <inheritdoc />
        public virtual void ReadJSONConfigFromFile(string filePath)
        {
            string text = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(text, this);
            Debug.Log($"Read JSON config from {filePath}.");
        }

        /// <inheritdoc />
        public virtual RetargetingProcessorType ProcessorType { get; set; }
    }
}
