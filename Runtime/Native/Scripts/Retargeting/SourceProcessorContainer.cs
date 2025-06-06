// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Contains all possible source processors, including the current one being used.
    /// </summary>
    [System.Serializable]
    public class SourceProcessorContainer
    {
        /// <summary>
        /// The currently selected processor type to be used for source skeleton processing.
        /// </summary>
        [SerializeField]
        protected SourceProcessor.ProcessorType _currentProcessorType;

        /// <summary>
        /// Reference to the ISDK skeletal processor implementation that can be used when the current processor type is set to ISDK.
        /// </summary>
        [SerializeField]
        protected ISDKSkeletalProcessor _isdkProcessor;

        /// <summary>
        /// Returns the current <see cref="SourceProcessor"/> based on the current type
        /// saved.
        /// </summary>
        /// <returns>The derived <see cref="SourceProcessor"/> type.</returns>
        public SourceProcessor GetCurrentProcessor()
        {
            switch (_currentProcessorType)
            {
                case SourceProcessor.ProcessorType.ISDK:
                    return _isdkProcessor;
                default:
                    return null;
            }
        }
    }
}
