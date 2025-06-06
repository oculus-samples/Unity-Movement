// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Movement.Retargeting;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Disables processors if dependent anchors become invalid.
    /// </summary>
    public class DisableProcessorsInvalidAnchor : MonoBehaviour
    {
        /// <summary>
        /// Disables a target processor if its anchor becomes invalid.
        /// </summary>
        [Serializable]
        private class ProcessorAnchorInfo
        {
            /// <summary>
            /// Target processor index on the character retargeter.
            /// </summary>
            public int TargetProcessorIndex;

            /// <summary>
            /// Anchor tranfsorm.
            /// </summary>
            public Transform Anchor;

            private bool _wasProcessorDisabled = false;
            private float _oldWeight;

            /// <summary>
            /// Validates fields.
            /// </summary>
            public void Validate()
            {
                Assert.IsNotNull(Anchor);
            }

            /// <summary>
            /// Updates all anchor objects.
            /// </summary>
            /// <param name="retargeter">Retargeter instance.</param>
            public void Update(CharacterRetargeter retargeter)
            {
                if (TargetProcessorIndex >= retargeter.TargetProcessorContainers.Length)
                {
                    Debug.LogError($"Processor index {TargetProcessorIndex} is too large for the number " +
                        $"available, which is {retargeter.TargetProcessorContainers.Length}.");
                    return;
                }

                var currProcessor = retargeter.TargetProcessorContainers[TargetProcessorIndex].GetCurrentProcessor();
                if (Anchor.position.sqrMagnitude > Mathf.Epsilon)
                {
                    // Re-enable the processor if it was disabled.
                    if (_wasProcessorDisabled)
                    {
                        _wasProcessorDisabled = false;
                        currProcessor.Weight = _oldWeight;
                    }

                    return;
                }

                // If anchor is close to origin, then keep the processor weight low.
                // Cache the old weight if it has not been cached before.
                if (!_wasProcessorDisabled)
                {
                    _oldWeight = currProcessor.Weight;
                    _wasProcessorDisabled = true;
                }

                currProcessor.Weight = 0.0f;
            }
        }

        /// <summary>
        /// Retargeter instance.
        /// </summary>
        [SerializeField]
        [Tooltip("Retargeter instance.")]
        private CharacterRetargeter _retargeter;

        /// <summary>
        /// Processor anchor info objects.
        /// </summary>
        [SerializeField]
        [Tooltip("Processor anchor info objects.")]
        private ProcessorAnchorInfo[] _processorAnchors;

        private void Awake()
        {
            Assert.IsNotNull(_retargeter);
            foreach (var anchorInfo in _processorAnchors)
            {
                anchorInfo.Validate();
            }
        }

        private void Update()
        {
            foreach (var anchorInfo in _processorAnchors)
            {
                anchorInfo.Update(_retargeter);
            }
        }
    }
}
