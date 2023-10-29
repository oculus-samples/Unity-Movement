// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    [Serializable]
    public class RetargetingProcessor : ScriptableObject, IRetargetingProcessor
    {
        /// <summary>
        /// The weight of this processor.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Weight = 1.0f;

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
    }
}
