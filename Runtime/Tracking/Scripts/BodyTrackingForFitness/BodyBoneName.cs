// Copyright (c) Meta Platforms, Inc. and affiliates.

#if INTERACTION_OVR_DEFINED
using Oculus.Interaction.Body.Input;
#endif
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// A convenient enumeration of all full body bones, without overlapping enum values (that
    /// can confuse Unity UI), using a naming convention that more closely matches Unity's
    /// <see cref="HumanBodyBones"/> than the core SDK. Based on <see cref="BodyJointId"/>.
    /// </summary>
    public enum BodyBoneName
    {
#if INTERACTION_OVR_DEFINED
        None = BodyJointId.Invalid,
        [InspectorName("Root")] Root = BodyJointId.Body_Root,
        [InspectorName("Hips")] Hips = BodyJointId.Body_Hips,
        [InspectorName("Spine/Lower")] SpineLower = BodyJointId.Body_SpineLower,
        [InspectorName("Spine/Middle")] SpineMiddle = BodyJointId.Body_SpineMiddle,
        [InspectorName("Spine/Upper")] SpineUpper = BodyJointId.Body_SpineUpper,
        [InspectorName("Spine/Chest")] Chest = BodyJointId.Body_Chest,
        [InspectorName("Spine/Neck")] Neck = BodyJointId.Body_Neck,
        [InspectorName("Head")] Head = BodyJointId.Body_Head,
        [InspectorName("Left Arm/Shoulder")] LeftShoulder = BodyJointId.Body_LeftShoulder,
        [InspectorName("Left Arm/Scapula")] LeftScapula = BodyJointId.Body_LeftScapula,
        [InspectorName("Left Arm/Upper")] LeftUpperArm = BodyJointId.Body_LeftArmUpper,
        [InspectorName("Left Arm/Lower")] LeftLowerArm = BodyJointId.Body_LeftArmLower,
        [InspectorName("Left Arm/Wrist Twist")] LeftWristTwist = BodyJointId.Body_LeftHandWristTwist,
        [InspectorName("Right Arm/Shoulder")] RightShoulder = BodyJointId.Body_RightShoulder,
        [InspectorName("Right Arm/Scapula")] RightScapula = BodyJointId.Body_RightScapula,
        [InspectorName("Right Arm/Upper")] RightUpperArm = BodyJointId.Body_RightArmUpper,
        [InspectorName("Right Arm/Lower")] RightLowerArm = BodyJointId.Body_RightArmLower,
        [InspectorName("Right Arm/Wrist Twist")] RightWristTwist = BodyJointId.Body_RightHandWristTwist,
        [InspectorName("Left Hand/Palm")] LeftPalm = BodyJointId.Body_LeftHandPalm,
        [InspectorName("Left Hand/Wrist")] LeftWrist = BodyJointId.Body_LeftHandWrist,
        [InspectorName("Left Hand/Thumb Metacarpal")] LeftThumbMetacarpal = BodyJointId.Body_LeftHandThumbMetacarpal,
        [InspectorName("Left Hand/Thumb Proximal")] LeftThumbProximal = BodyJointId.Body_LeftHandThumbProximal,
        [InspectorName("Left Hand/Thumb Distal")] LeftThumbDistal = BodyJointId.Body_LeftHandThumbDistal,
        [InspectorName("Left Hand/Thumb Tip")] LeftThumbTip = BodyJointId.Body_LeftHandThumbTip,
        [InspectorName("Left Hand/Index Metacarpal")] LeftIndexMetacarpal = BodyJointId.Body_LeftHandIndexMetacarpal,
        [InspectorName("Left Hand/Index Proximal")] LeftIndexProximal = BodyJointId.Body_LeftHandIndexProximal,
        [InspectorName("Left Hand/Index Intermediate")] LeftIndexIntermediate = BodyJointId.Body_LeftHandIndexIntermediate,
        [InspectorName("Left Hand/Index Distal")] LeftIndexDistal = BodyJointId.Body_LeftHandIndexDistal,
        [InspectorName("Left Hand/Index Tip")] LeftIndexTip = BodyJointId.Body_LeftHandIndexTip,
        [InspectorName("Left Hand/Middle Metacarpal")] LeftMiddleMetacarpal = BodyJointId.Body_LeftHandMiddleMetacarpal,
        [InspectorName("Left Hand/Middle Proximal")] LeftMiddleProximal = BodyJointId.Body_LeftHandMiddleProximal,
        [InspectorName("Left Hand/Middle Intermediate")] LeftMiddleIntermediate = BodyJointId.Body_LeftHandMiddleIntermediate,
        [InspectorName("Left Hand/Middle Distal")] LeftMiddleDistal = BodyJointId.Body_LeftHandMiddleDistal,
        [InspectorName("Left Hand/Middle Tip")] LeftMiddleTip = BodyJointId.Body_LeftHandMiddleTip,
        [InspectorName("Left Hand/Ring Metacarpal")] LeftRingMetacarpal = BodyJointId.Body_LeftHandRingMetacarpal,
        [InspectorName("Left Hand/Ring Proximal")] LeftRingProximal = BodyJointId.Body_LeftHandRingProximal,
        [InspectorName("Left Hand/Ring Intermediate")] LeftRingIntermediate = BodyJointId.Body_LeftHandRingIntermediate,
        [InspectorName("Left Hand/Ring Distal")] LeftRingDistal = BodyJointId.Body_LeftHandRingDistal,
        [InspectorName("Left Hand/Ring Tip")] LeftRingTip = BodyJointId.Body_LeftHandRingTip,
        [InspectorName("Left Hand/Little Metacarpal")] LeftLittleMetacarpal = BodyJointId.Body_LeftHandLittleMetacarpal,
        [InspectorName("Left Hand/Little Proximal")] LeftLittleProximal = BodyJointId.Body_LeftHandLittleProximal,
        [InspectorName("Left Hand/Little Intermediate")] LeftLittleIntermediate = BodyJointId.Body_LeftHandLittleIntermediate,
        [InspectorName("Left Hand/Little Distal")] LeftLittleDistal = BodyJointId.Body_LeftHandLittleDistal,
        [InspectorName("Left Hand/Little Tip")] LeftLittleTip = BodyJointId.Body_LeftHandLittleTip,
        [InspectorName("Right Hand/Palm")] RightPalm = BodyJointId.Body_RightHandPalm,
        [InspectorName("Right Hand/Wrist")] RightWrist = BodyJointId.Body_RightHandWrist,
        [InspectorName("Right Hand/Thumb Metacarpal")] RightThumbMetacarpal = BodyJointId.Body_RightHandThumbMetacarpal,
        [InspectorName("Right Hand/Thumb Proximal")] RightThumbProximal = BodyJointId.Body_RightHandThumbProximal,
        [InspectorName("Right Hand/Thumb Distal")] RightThumbDistal = BodyJointId.Body_RightHandThumbDistal,
        [InspectorName("Right Hand/Thumb Tip")] RightThumbTip = BodyJointId.Body_RightHandThumbTip,
        [InspectorName("Right Hand/Index Metacarpal")] RightIndexMetacarpal = BodyJointId.Body_RightHandIndexMetacarpal,
        [InspectorName("Right Hand/Index Proximal")] RightIndexProximal = BodyJointId.Body_RightHandIndexProximal,
        [InspectorName("Right Hand/Index Intermediate")] RightIndexIntermediate = BodyJointId.Body_RightHandIndexIntermediate,
        [InspectorName("Right Hand/Index Distal")] RightIndexDistal = BodyJointId.Body_RightHandIndexDistal,
        [InspectorName("Right Hand/Index Tip")] RightIndexTip = BodyJointId.Body_RightHandIndexTip,
        [InspectorName("Right Hand/Middle Metacarpal")] RightMiddleMetacarpal = BodyJointId.Body_RightHandMiddleMetacarpal,
        [InspectorName("Right Hand/Middle Proximal")] RightMiddleProximal = BodyJointId.Body_RightHandMiddleProximal,
        [InspectorName("Right Hand/Middle Intermediate")] RightMiddleIntermediate = BodyJointId.Body_RightHandMiddleIntermediate,
        [InspectorName("Right Hand/Middle Distal")] RightMiddleDistal = BodyJointId.Body_RightHandMiddleDistal,
        [InspectorName("Right Hand/Middle Tip")] RightMiddleTip = BodyJointId.Body_RightHandMiddleTip,
        [InspectorName("Right Hand/Ring Metacarpal")] RightRingMetacarpal = BodyJointId.Body_RightHandRingMetacarpal,
        [InspectorName("Right Hand/Ring Proximal")] RightRingProximal = BodyJointId.Body_RightHandRingProximal,
        [InspectorName("Right Hand/Ring Intermediate")] RightRingIntermediate = BodyJointId.Body_RightHandRingIntermediate,
        [InspectorName("Right Hand/Ring Distal")] RightRingDistal = BodyJointId.Body_RightHandRingDistal,
        [InspectorName("Right Hand/Ring Tip")] RightRingTip = BodyJointId.Body_RightHandRingTip,
        [InspectorName("Right Hand/Little Metacarpal")] RightLittleMetacarpal = BodyJointId.Body_RightHandLittleMetacarpal,
        [InspectorName("Right Hand/Little Proximal")] RightLittleProximal = BodyJointId.Body_RightHandLittleProximal,
        [InspectorName("Right Hand/Little Intermediate")] RightLittleIntermediate = BodyJointId.Body_RightHandLittleIntermediate,
        [InspectorName("Right Hand/Little Distal")] RightLittleDistal = BodyJointId.Body_RightHandLittleDistal,
        [InspectorName("Right Hand/Little Tip")] RightLittleTip = BodyJointId.Body_RightHandLittleTip,
        [InspectorName("Left Leg/Upper")] LeftLegUpper = BodyJointId.Body_LeftLegUpper,
        [InspectorName("Left Leg/Lower")] LeftLegLower = BodyJointId.Body_LeftLegLower,
        [InspectorName("Left Leg/Ankle Twist")] LeftFootAnkleTwist = BodyJointId.Body_LeftFootAnkleTwist,
        [InspectorName("Left Foot/Ankle")] LeftFootAnkle = BodyJointId.Body_LeftFootAnkle,
        [InspectorName("Left Foot/Subtalar")] LeftFootSubtalar = BodyJointId.Body_LeftFootSubtalar,
        [InspectorName("Left Foot/Transverse")] LeftFootTransverse = BodyJointId.Body_LeftFootTransverse,
        [InspectorName("Left Foot/Toes")] LeftFootToes = BodyJointId.Body_LeftFootBall,
        [InspectorName("Right Leg/Upper")] RightLegUpper = BodyJointId.Body_RightLegUpper,
        [InspectorName("Right Leg/Lower")] RightLegLower = BodyJointId.Body_RightLegLower,
        [InspectorName("Right Leg/Ankle Twist")] RightFootAnkleTwist = BodyJointId.Body_RightFootAnkleTwist,
        [InspectorName("Right Foot/Ankle")] RightFootAnkle = BodyJointId.Body_RightFootAnkle,
        [InspectorName("Right Foot/Subtalar")] RightFootSubtalar = BodyJointId.Body_RightFootSubtalar,
        [InspectorName("Right Foot/Transverse")] RightFootTransverse = BodyJointId.Body_RightFootTransverse,
        [InspectorName("Right Foot/Toes")] RightFootToes = BodyJointId.Body_RightFootBall,
        /// <summary>
        /// Not a bone --this is the count of bones.
        /// </summary>
        End = BodyJointId.Body_End
#endif
    }
}
