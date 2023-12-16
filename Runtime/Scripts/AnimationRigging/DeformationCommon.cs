// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Data types common to all deformation classes.
    /// </summary>
    public static class DeformationCommon
    {
        /// <summary>
        /// Information about the distance between two bone transforms.
        /// </summary>
        [Serializable]
        public struct BonePairData
        {
            /// <summary>
            /// The start bone transform.
            /// </summary>
            [SyncSceneToStream]
            public Transform StartBone;

            /// <summary>
            /// The end bone transform.
            /// </summary>
            [SyncSceneToStream]
            public Transform EndBone;

            /// <summary>
            /// The distance between the start and end bones.
            /// </summary>
            public float Distance;

            /// <summary>
            /// The proportion of this bone relative to the height.
            /// </summary>
            public float HeightProportion;

            /// <summary>
            /// The proportion of this bone relative to its limb.
            /// </summary>
            public float LimbProportion;
        }

        /// <summary>
        /// Information about the positioning of an arm.
        /// </summary>
        [Serializable]
        public struct ArmPosData
        {
            /// <summary>
            /// The shoulder transform.
            /// </summary>
            public Transform ShoulderBone;

            /// <summary>
            /// The upper arm transform.
            /// </summary>
            public Transform UpperArmBone;

            /// <summary>
            /// The lower arm transform.
            /// </summary>
            public Transform LowerArmBone;

            /// <summary>
            /// The hand transform.
            /// </summary>
            public Transform HandBone;

            /// <summary>
            /// The local position of the shoulder.
            /// </summary>
            public Vector3 ShoulderLocalPos;

            /// <summary>
            /// The axis of the lower arm to the hand.
            /// </summary>
            public Vector3 LowerArmToHandAxis;

            /// <summary>
            /// Indicates if initialized or not.
            /// </summary>
            /// <returns></returns>
            public bool IsInitialized =>
                UpperArmBone != null &&
                LowerArmBone != null &&
                HandBone != null &&
                LowerArmToHandAxis != Vector3.zero;

            /// <summary>
            /// Resets all tracked transforms to null.
            /// </summary>
            public void ClearTransformData()
            {
                ShoulderBone = null;
                UpperArmBone = null;
                LowerArmBone = null;
                HandBone = null;
            }
        }
    }
}
