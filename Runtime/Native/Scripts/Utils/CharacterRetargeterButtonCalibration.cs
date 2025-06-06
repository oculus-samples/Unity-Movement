// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using UnityEngine;

namespace Meta.XR.Movement.Utils
{
    public class CharacterRetargeterButtonCalibration : MonoBehaviour
    {
        [SerializeField]
        private CharacterRetargeter[] _characterRetargeters;

        [SerializeField]
        private OVRInput.Button _calibrationButton = OVRInput.Button.One;

        public void Update()
        {
            if (!OVRInput.Get(_calibrationButton))
            {
                return;
            }

            foreach (var retargeter in _characterRetargeters)
            {
                retargeter.Calibrate();
            }
        }
    }
}
