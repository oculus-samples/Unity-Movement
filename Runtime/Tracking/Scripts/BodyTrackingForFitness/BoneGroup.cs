// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
#if INTERACTION_OVR_DEFINED
using Oculus.Interaction.Body.Input;
#endif
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// List bones by logical body group. Also has static methods to mirror body pose.
    /// </summary>
    public static class BoneGroup
    {
#if INTERACTION_OVR_DEFINED
        /// <summary>
        /// Bones that can be mostly derived from positioning of other bones
        /// </summary>
        public static readonly BodyJointId[] CommonIgnored = new BodyJointId[]
        {
            BodyJointId.Body_LeftShoulder,
            BodyJointId.Body_LeftScapula,
            BodyJointId.Body_LeftHandWristTwist,
            BodyJointId.Body_RightShoulder,
            BodyJointId.Body_RightScapula,
            BodyJointId.Body_RightHandWristTwist,
            BodyJointId.Body_LeftHandPalm,
            BodyJointId.Body_LeftHandThumbTip,
            BodyJointId.Body_LeftHandIndexMetacarpal,
            BodyJointId.Body_LeftHandIndexTip,
            BodyJointId.Body_LeftHandMiddleMetacarpal,
            BodyJointId.Body_LeftHandMiddleTip,
            BodyJointId.Body_LeftHandRingMetacarpal,
            BodyJointId.Body_LeftHandRingTip,
            BodyJointId.Body_LeftHandLittleMetacarpal,
            BodyJointId.Body_LeftHandLittleTip,
            BodyJointId.Body_RightHandPalm,
            BodyJointId.Body_RightHandThumbTip,
            BodyJointId.Body_RightHandIndexMetacarpal,
            BodyJointId.Body_RightHandIndexTip,
            BodyJointId.Body_RightHandMiddleMetacarpal,
            BodyJointId.Body_RightHandMiddleTip,
            BodyJointId.Body_RightHandRingMetacarpal,
            BodyJointId.Body_RightHandRingTip,
            BodyJointId.Body_RightHandLittleMetacarpal,
            BodyJointId.Body_RightHandLittleTip,
            BodyJointId.Body_LeftFootAnkleTwist,
            BodyJointId.Body_RightFootAnkleTwist,
        };

        /// <summary>
        /// Spinal column bones, from hips to head.
        /// </summary>
        public static readonly BodyJointId[] SpinalColumn = new BodyJointId[]
        {
            BodyJointId.Body_Hips,
            BodyJointId.Body_SpineLower,
            BodyJointId.Body_SpineMiddle,
            BodyJointId.Body_SpineUpper,
            BodyJointId.Body_Chest,
            BodyJointId.Body_Neck,
            BodyJointId.Body_Head
        };

        /// <summary>
        /// Left arm bones, including commonly ignored shoulder bones
        /// </summary>
        public static readonly BodyJointId[] LeftArm = new BodyJointId[]
        {
            BodyJointId.Body_LeftArmLower,
            BodyJointId.Body_LeftArmUpper,
            BodyJointId.Body_LeftScapula,
            BodyJointId.Body_LeftShoulder
        };

        /// <summary>
        /// Right arm bones, including commonly ignored shoulder bones
        /// </summary>
        public static readonly BodyJointId[] RightArm = new BodyJointId[]
        {
            BodyJointId.Body_RightArmLower,
            BodyJointId.Body_RightArmUpper,
            BodyJointId.Body_RightScapula,
            BodyJointId.Body_RightShoulder
        };

        /// <summary>
        /// Left leg bones, including commonly ignored ankle twist
        /// </summary>
        public static readonly BodyJointId[] LeftLeg = new BodyJointId[]
        {
            BodyJointId.Body_LeftFootAnkleTwist,
            BodyJointId.Body_LeftLegLower,
            BodyJointId.Body_LeftLegUpper,
        };

        /// <summary>
        /// Right leg bones, including commonly ignored ankle twist
        /// </summary>
        public static readonly BodyJointId[] RightLeg = new BodyJointId[]
        {
            BodyJointId.Body_RightFootAnkleTwist,
            BodyJointId.Body_RightLegLower,
            BodyJointId.Body_RightLegUpper,
        };

        /// <summary>
        /// Left foot bones
        /// </summary>
        public static readonly BodyJointId[] LeftFoot = new BodyJointId[]
        {
            BodyJointId.Body_LeftFootAnkle,
            BodyJointId.Body_LeftFootSubtalar,
            BodyJointId.Body_LeftFootTransverse,
            BodyJointId.Body_LeftFootBall,
        };

        /// <summary>
        /// Right foot bones
        /// </summary>
        public static readonly BodyJointId[] RightFoot = new BodyJointId[]
        {
            BodyJointId.Body_RightFootAnkle,
            BodyJointId.Body_RightFootSubtalar,
            BodyJointId.Body_RightFootTransverse,
            BodyJointId.Body_RightFootBall,
        };

        /// <summary>
        /// Left hand bones, including commonly ignored metacarpals and wrist twist
        /// </summary>
        public static readonly BodyJointId[] LeftHand = new BodyJointId[]
        {
            BodyJointId.Body_LeftHandWristTwist,
            BodyJointId.Body_LeftHandWrist,
            BodyJointId.Body_LeftHandPalm,
            BodyJointId.Body_LeftHandThumbMetacarpal,
            BodyJointId.Body_LeftHandThumbProximal,
            BodyJointId.Body_LeftHandThumbDistal,
            BodyJointId.Body_LeftHandThumbTip,
            BodyJointId.Body_LeftHandIndexMetacarpal,
            BodyJointId.Body_LeftHandIndexProximal,
            BodyJointId.Body_LeftHandIndexIntermediate,
            BodyJointId.Body_LeftHandIndexDistal,
            BodyJointId.Body_LeftHandIndexTip,
            BodyJointId.Body_LeftHandMiddleMetacarpal,
            BodyJointId.Body_LeftHandMiddleProximal,
            BodyJointId.Body_LeftHandMiddleIntermediate,
            BodyJointId.Body_LeftHandMiddleDistal,
            BodyJointId.Body_LeftHandMiddleTip,
            BodyJointId.Body_LeftHandRingMetacarpal,
            BodyJointId.Body_LeftHandRingProximal,
            BodyJointId.Body_LeftHandRingIntermediate,
            BodyJointId.Body_LeftHandRingDistal,
            BodyJointId.Body_LeftHandRingTip,
            BodyJointId.Body_LeftHandLittleMetacarpal,
            BodyJointId.Body_LeftHandLittleProximal,
            BodyJointId.Body_LeftHandLittleIntermediate,
            BodyJointId.Body_LeftHandLittleDistal,
            BodyJointId.Body_LeftHandLittleTip,
        };

        /// <summary>
        /// Right hand bones, including commonly ignored metacarpals and wrist twist
        /// </summary>
        public static readonly BodyJointId[] RightHand = new BodyJointId[]
        {
            BodyJointId.Body_RightHandWristTwist,
            BodyJointId.Body_RightHandWrist,
            BodyJointId.Body_RightHandPalm,
            BodyJointId.Body_RightHandThumbMetacarpal,
            BodyJointId.Body_RightHandThumbProximal,
            BodyJointId.Body_RightHandThumbDistal,
            BodyJointId.Body_RightHandThumbTip,
            BodyJointId.Body_RightHandIndexMetacarpal,
            BodyJointId.Body_RightHandIndexProximal,
            BodyJointId.Body_RightHandIndexIntermediate,
            BodyJointId.Body_RightHandIndexDistal,
            BodyJointId.Body_RightHandIndexTip,
            BodyJointId.Body_RightHandMiddleMetacarpal,
            BodyJointId.Body_RightHandMiddleProximal,
            BodyJointId.Body_RightHandMiddleIntermediate,
            BodyJointId.Body_RightHandMiddleDistal,
            BodyJointId.Body_RightHandMiddleTip,
            BodyJointId.Body_RightHandRingMetacarpal,
            BodyJointId.Body_RightHandRingProximal,
            BodyJointId.Body_RightHandRingIntermediate,
            BodyJointId.Body_RightHandRingDistal,
            BodyJointId.Body_RightHandRingTip,
            BodyJointId.Body_RightHandLittleMetacarpal,
            BodyJointId.Body_RightHandLittleProximal,
            BodyJointId.Body_RightHandLittleIntermediate,
            BodyJointId.Body_RightHandLittleDistal,
            BodyJointId.Body_RightHandLittleTip,
        };

        /// <summary>
        /// Body bones that create a human silhouette
        /// </summary>
        public static readonly BodyJointId[] CommonBody = new BodyJointId[]
        {
            BodyJointId.Body_Hips,
            BodyJointId.Body_SpineLower,
            BodyJointId.Body_SpineMiddle,
            BodyJointId.Body_SpineUpper,
            BodyJointId.Body_Chest,
            BodyJointId.Body_Neck,
            BodyJointId.Body_Head,
            BodyJointId.Body_LeftArmUpper,
            BodyJointId.Body_LeftArmLower,
            BodyJointId.Body_RightArmUpper,
            BodyJointId.Body_RightArmLower,
            BodyJointId.Body_LeftLegUpper,
            BodyJointId.Body_LeftLegLower,
            BodyJointId.Body_RightLegUpper,
            BodyJointId.Body_RightLegLower,
        };

        /// <summary>
        /// All bones in an ordered list
        /// </summary>
        public static readonly BodyJointId[] All = new BodyJointId[]
        {
            BodyJointId.Body_Root,
            BodyJointId.Body_Hips,
            BodyJointId.Body_SpineLower,
            BodyJointId.Body_SpineMiddle,
            BodyJointId.Body_SpineUpper,
            BodyJointId.Body_SpineUpper,
            BodyJointId.Body_Chest,
            BodyJointId.Body_Neck,
            BodyJointId.Body_Head,
            BodyJointId.Body_LeftShoulder,
            BodyJointId.Body_LeftScapula,
            BodyJointId.Body_LeftArmUpper,
            BodyJointId.Body_LeftArmLower,
            BodyJointId.Body_LeftHandWristTwist,
            BodyJointId.Body_RightShoulder,
            BodyJointId.Body_RightScapula,
            BodyJointId.Body_RightArmUpper,
            BodyJointId.Body_RightArmLower,
            BodyJointId.Body_RightHandWristTwist,
            BodyJointId.Body_LeftHandPalm,
            BodyJointId.Body_LeftHandWrist,
            BodyJointId.Body_LeftHandThumbMetacarpal,
            BodyJointId.Body_LeftHandThumbProximal,
            BodyJointId.Body_LeftHandThumbDistal,
            BodyJointId.Body_LeftHandThumbTip,
            BodyJointId.Body_LeftHandIndexMetacarpal,
            BodyJointId.Body_LeftHandIndexProximal,
            BodyJointId.Body_LeftHandIndexIntermediate,
            BodyJointId.Body_LeftHandIndexDistal,
            BodyJointId.Body_LeftHandIndexTip,
            BodyJointId.Body_LeftHandMiddleMetacarpal,
            BodyJointId.Body_LeftHandMiddleProximal,
            BodyJointId.Body_LeftHandMiddleIntermediate,
            BodyJointId.Body_LeftHandMiddleDistal,
            BodyJointId.Body_LeftHandMiddleTip,
            BodyJointId.Body_LeftHandRingMetacarpal,
            BodyJointId.Body_LeftHandRingProximal,
            BodyJointId.Body_LeftHandRingIntermediate,
            BodyJointId.Body_LeftHandRingDistal,
            BodyJointId.Body_LeftHandRingTip,
            BodyJointId.Body_LeftHandLittleMetacarpal,
            BodyJointId.Body_LeftHandLittleProximal,
            BodyJointId.Body_LeftHandLittleIntermediate,
            BodyJointId.Body_LeftHandLittleDistal,
            BodyJointId.Body_LeftHandLittleTip,
            BodyJointId.Body_RightHandPalm,
            BodyJointId.Body_RightHandWrist,
            BodyJointId.Body_RightHandThumbMetacarpal,
            BodyJointId.Body_RightHandThumbProximal,
            BodyJointId.Body_RightHandThumbDistal,
            BodyJointId.Body_RightHandThumbTip,
            BodyJointId.Body_RightHandIndexMetacarpal,
            BodyJointId.Body_RightHandIndexProximal,
            BodyJointId.Body_RightHandIndexIntermediate,
            BodyJointId.Body_RightHandIndexDistal,
            BodyJointId.Body_RightHandIndexTip,
            BodyJointId.Body_RightHandMiddleMetacarpal,
            BodyJointId.Body_RightHandMiddleProximal,
            BodyJointId.Body_RightHandMiddleIntermediate,
            BodyJointId.Body_RightHandMiddleDistal,
            BodyJointId.Body_RightHandMiddleTip,
            BodyJointId.Body_RightHandRingMetacarpal,
            BodyJointId.Body_RightHandRingProximal,
            BodyJointId.Body_RightHandRingIntermediate,
            BodyJointId.Body_RightHandRingDistal,
            BodyJointId.Body_RightHandRingTip,
            BodyJointId.Body_RightHandLittleMetacarpal,
            BodyJointId.Body_RightHandLittleProximal,
            BodyJointId.Body_RightHandLittleIntermediate,
            BodyJointId.Body_RightHandLittleDistal,
            BodyJointId.Body_RightHandLittleTip,
            BodyJointId.Body_LeftLegUpper,
            BodyJointId.Body_LeftLegLower,
            BodyJointId.Body_LeftFootAnkleTwist,
            BodyJointId.Body_LeftFootAnkle,
            BodyJointId.Body_LeftFootSubtalar,
            BodyJointId.Body_LeftFootTransverse,
            BodyJointId.Body_LeftFootBall,
            BodyJointId.Body_RightLegUpper,
            BodyJointId.Body_RightLegLower,
            BodyJointId.Body_RightFootAnkleTwist,
            BodyJointId.Body_RightFootAnkle,
            BodyJointId.Body_RightFootSubtalar,
            BodyJointId.Body_RightFootTransverse,
            BodyJointId.Body_RightFootBall,
        };

        /// <summary>
        /// Which bones should swap when reflecting the skeleton horizontally
        /// </summary>
        public readonly static (BodyJointId, BodyJointId)[] HorizontalReflection =
        {
            (BodyJointId.Body_LeftShoulder, BodyJointId.Body_RightShoulder),
            (BodyJointId.Body_LeftScapula, BodyJointId.Body_RightScapula),
            (BodyJointId.Body_LeftArmUpper, BodyJointId.Body_RightArmUpper),
            (BodyJointId.Body_LeftArmLower, BodyJointId.Body_RightArmLower),
            (BodyJointId.Body_LeftHandWristTwist, BodyJointId.Body_RightHandWristTwist),
            (BodyJointId.Body_LeftHandWrist, BodyJointId.Body_RightHandWrist),
            (BodyJointId.Body_LeftHandPalm, BodyJointId.Body_RightHandPalm),
            (BodyJointId.Body_LeftHandThumbMetacarpal, BodyJointId.Body_RightHandThumbMetacarpal),
            (BodyJointId.Body_LeftHandThumbProximal, BodyJointId.Body_RightHandThumbProximal),
            (BodyJointId.Body_LeftHandThumbDistal, BodyJointId.Body_RightHandThumbDistal),
            (BodyJointId.Body_LeftHandThumbTip, BodyJointId.Body_RightHandThumbTip),
            (BodyJointId.Body_LeftHandIndexMetacarpal, BodyJointId.Body_RightHandIndexMetacarpal),
            (BodyJointId.Body_LeftHandIndexProximal, BodyJointId.Body_RightHandIndexProximal),
            (BodyJointId.Body_LeftHandIndexIntermediate, BodyJointId.Body_RightHandIndexIntermediate),
            (BodyJointId.Body_LeftHandIndexDistal, BodyJointId.Body_RightHandIndexDistal),
            (BodyJointId.Body_LeftHandIndexTip, BodyJointId.Body_RightHandIndexTip),
            (BodyJointId.Body_LeftHandMiddleMetacarpal, BodyJointId.Body_RightHandMiddleMetacarpal),
            (BodyJointId.Body_LeftHandMiddleProximal, BodyJointId.Body_RightHandMiddleProximal),
            (BodyJointId.Body_LeftHandMiddleIntermediate, BodyJointId.Body_RightHandMiddleIntermediate),
            (BodyJointId.Body_LeftHandMiddleDistal, BodyJointId.Body_RightHandMiddleDistal),
            (BodyJointId.Body_LeftHandMiddleTip, BodyJointId.Body_RightHandMiddleTip),
            (BodyJointId.Body_LeftHandRingMetacarpal, BodyJointId.Body_RightHandRingMetacarpal),
            (BodyJointId.Body_LeftHandRingProximal, BodyJointId.Body_RightHandRingProximal),
            (BodyJointId.Body_LeftHandRingIntermediate, BodyJointId.Body_RightHandRingIntermediate),
            (BodyJointId.Body_LeftHandRingDistal, BodyJointId.Body_RightHandRingDistal),
            (BodyJointId.Body_LeftHandRingTip, BodyJointId.Body_RightHandRingTip),
            (BodyJointId.Body_LeftHandLittleMetacarpal, BodyJointId.Body_RightHandLittleMetacarpal),
            (BodyJointId.Body_LeftHandLittleProximal, BodyJointId.Body_RightHandLittleProximal),
            (BodyJointId.Body_LeftHandLittleIntermediate, BodyJointId.Body_RightHandLittleIntermediate),
            (BodyJointId.Body_LeftHandLittleDistal, BodyJointId.Body_RightHandLittleDistal),
            (BodyJointId.Body_LeftHandLittleTip, BodyJointId.Body_RightHandLittleTip),
            (BodyJointId.Body_LeftLegUpper, BodyJointId.Body_RightLegUpper),
            (BodyJointId.Body_LeftLegLower, BodyJointId.Body_RightLegLower),
            (BodyJointId.Body_LeftFootAnkle, BodyJointId.Body_RightFootAnkle),
            (BodyJointId.Body_LeftFootAnkleTwist, BodyJointId.Body_RightFootAnkleTwist),
            (BodyJointId.Body_LeftFootSubtalar, BodyJointId.Body_RightFootSubtalar),
            (BodyJointId.Body_LeftFootTransverse, BodyJointId.Body_RightFootTransverse),
            (BodyJointId.Body_LeftFootBall, BodyJointId.Body_RightFootBall),
        };

        /// <summary>
        /// Used when mirroring bone poses
        /// </summary>
        private static readonly Vector3 CoefficientMirrorPositionX = new Vector3(-1, 1, 1);

        /// <summary>
        /// Used when mirroring bone poses
        /// </summary>
        private static readonly Quaternion CoefficientMirrorRotationX =
            Quaternion.AngleAxis(180, Vector3.up) *
            Quaternion.AngleAxis(180, Vector3.forward);

        private static Pose MirroredPoseX(Pose pose) =>
            new Pose(MirrorPositionX(pose.position), MirrorRotationX(pose.rotation));

        /// <summary>
        /// Mirrors the given body pose horizontally, modifying the given bone pose list.
        /// </summary>
        /// <param name="bones">each index matches a <see cref="BodyJointId"/></param>
        public static void MirrorX(IList<Pose> bones)
        {
            for (int i = 0; i < bones.Count; ++i)
            {
                bones[i] = MirroredPoseX(bones[i]);
            }
            for (int i = 0; i < HorizontalReflection.Length; ++i)
            {
                int leftBodyJointId = (int)HorizontalReflection[i].Item1;
                int rightBodyJointId = (int)HorizontalReflection[i].Item2;
                SwapBones(bones, leftBodyJointId, rightBodyJointId);
            }
        }

        private static void SwapBones(IList<Pose> bones, int left, int right)
        {
            Pose swap = bones[left];
            bones[left] = bones[right];
            bones[right] = swap;
        }

        private static Quaternion MirrorRotationX(Quaternion q)
        {
            return new Quaternion(q.x * -1.0f, q.y, q.z, q.w * -1.0f) * CoefficientMirrorRotationX;
        }

        private static Vector3 MirrorPositionX(Vector3 p)
        {
            p.Scale(CoefficientMirrorPositionX);
            return p;
        }
#endif
    }
}
