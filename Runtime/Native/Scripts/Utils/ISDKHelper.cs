// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
#if INTERACTION_OVR_DEFINED
using Oculus.Interaction.Input;
#endif
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Helper class for managing ISDK (Interaction SDK) components and processors.
    /// </summary>
    public class ISDKHelper : MonoBehaviour
    {
#if INTERACTION_OVR_DEFINED
        /// <summary>
        /// Structure containing references to hand components and processors.
        /// </summary>
        [Serializable]
        public struct ISDKData
        {
            /// <summary>
            /// The GameObject containing the hand component.
            /// </summary>
            [SerializeField]
            public GameObject Hand;

            /// <summary>
            /// Array of CCD (Cyclic Coordinate Descent) skeletal processors to control.
            /// </summary>
            [SerializeField]
            public CCDSkeletalProcessor[] CCDProcessors;

            /// <summary>
            /// Array of ISDK skeletal processors to control.
            /// </summary>
            [SerializeField]
            public ISDKSkeletalProcessor[] ISDKProcessors;

            /// <summary>
            /// Reference to the IHand component on the Hand GameObject.
            /// </summary>
            public IHand iHand;
        }

        [SerializeField]
        protected ISDKData[] _isdkData;

        /// <summary>
        /// Updates the weights of processors based on hand tracking state.
        /// Enables processors when hand tracking is valid and disables them otherwise.
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < _isdkData.Length; i++)
            {
                var data = _isdkData[i];
                if (data.iHand == null)
                {
                    data.iHand = data.Hand.GetComponent<IHand>();
                }
                bool enableHandProcessors =
                    data.iHand.IsConnected && data.iHand.IsHighConfidence &&
                    data.iHand.IsTrackedDataValid;

                if (data.CCDProcessors != null)
                {
                    foreach (var ccdProcessor in data.CCDProcessors)
                    {
                        ccdProcessor.Weight = enableHandProcessors ? 1.0f : 0.0f;
                    }
                }

                if (data.ISDKProcessors != null)
                {
                    foreach (var isdkProcessor in data.ISDKProcessors)
                    {
                        isdkProcessor.Weight = enableHandProcessors ? 1.0f : 0.0f;
                    }
                }
            }
        }
#endif
    }
}
