// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Modifies source joints. Use this influence the source joints before they are
    /// retargeted to a target character.
    /// </summary>
    [System.Serializable]
    public abstract class SourceProcessor
    {
        /// <summary>
        /// Defines the source processor type. This can be
        /// extended in the future to support more custom versions.
        /// </summary>
        public enum ProcessorType
        {
            None = 0,
            ISDK,
        }

        /// <summary>
        /// The weight of this processor.
        /// </summary>
        public float Weight
        {
            get => _weight;
            set => _weight = value;
        }

        /// <summary>
        /// The weight of this processor, controlling how much influence it has on the source skeleton.
        /// </summary>
        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float _weight = 1.0f;

        /// <summary>
        /// Editor-only property that determines whether the processor is expanded in the inspector.
        /// </summary>
        [SerializeField]
        protected bool _isFoldoutExpanded = true;

        /// <summary>
        /// Initializes the processor.
        /// </summary>
        public abstract void Initialize(CharacterRetargeter characterRetargeter);

        /// <summary>
        /// Processes skeletal pose values before they are applied via retargeting.
        /// </summary>
        /// <param name="sourcePoses">The skeletal source poses to be affected.</param>
        public abstract void ProcessSkeleton(NativeArray<NativeTransform> sourcePoses);
    }
}
