// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.AI
{
    /// <summary>
    /// Controls how the blend factor is determined.
    /// </summary>
    public enum BlendMode
    {
        /// <summary>Use <see cref="AIMotionSynthesizerConfig.BlendFactor"/> directly.</summary>
        [Tooltip("Manually control blend factor")]
        Manual,

        /// <summary>Automatically blend based on input provider activity.</summary>
        [Tooltip("Automatically control blend factor based on input provider")]
        Input
    }

    /// <summary>
    /// Controls how root motion is applied to the character.
    /// </summary>
    public enum RootMotionMode
    {
        /// <summary>No root motion applied.</summary>
        [Tooltip("Do not apply root motion")]
        None,

        /// <summary>Copy position/rotation from <see cref="AIMotionSynthesizerConfig.ReferenceTransform"/>.</summary>
        [Tooltip("Apply root motion from reference")]
        ApplyFromReference,

        /// <summary>Apply root motion computed by the AI motion synthesizer system.</summary>
        [Tooltip("Apply root motion from AIMotionSynthesizer")]
        ApplyRootMotion
    }

    /// <summary>
    /// Configuration for AI motion synthesizer blending with body tracking.
    /// </summary>
    [System.Serializable]
    public class AIMotionSynthesizerConfig
    {
        /// <summary>JSON skeleton configuration file (AIMotionSynthesizerSkeletonData.json).</summary>
        [Tooltip("JSON configuration file for the AIMotionSynthesizer skeleton data")]
        public TextAsset Config;

        /// <summary>Binary neural network model file.</summary>
        [Tooltip("Binary model asset for the AIMotionSynthesizer")]
        public TextAsset ModelAsset;

        /// <summary>Optional guidance asset for animation style variations.</summary>
        [Tooltip("Binary guidance asset database file")]
        public TextAsset GuidanceAsset;

        /// <summary>Component providing velocity/direction input. Must implement <see cref="IAIMotionSynthesizerInputProvider"/>.</summary>
        [Tooltip("Input provider for velocity and direction (must implement IAIMotionSynthesizerInputProvider)")]
        public MonoBehaviour InputProvider;

        /// <summary>How root motion is applied to the character transform.</summary>
        [Tooltip("Root motion mode for AIMotionSynthesizer. Controls how root motion is applied to the transform.")]
        public RootMotionMode RootMotionMode = RootMotionMode.ApplyFromReference;

        /// <summary>Transform to copy position/rotation from when using <see cref="RootMotionMode.ApplyFromReference"/>.</summary>
        [Tooltip("Reference transform used when RootMotionMode is set to ApplyFromReferenceTransform")]
        public Transform ReferenceTransform;

        /// <summary>When true, uses a synthesized standing pose regardless of blend factor.</summary>
        [Tooltip("When enabled, uses synthesized standing pose with blend factor always set to 1")]
        public bool EnableSynthesizedStandingPose = false;

        /// <summary>Pose source for upper body (spine and above) at full blend.</summary>
        [Tooltip("Which pose to use for upper body when blend factor = 1 (spine and above)")]
        public MSDKAIMotionSynthesizer.PoseSource UpperBodySource = MSDKAIMotionSynthesizer.PoseSource.BodyTracking;

        /// <summary>Pose source for lower body (hips and legs) at full blend.</summary>
        [Tooltip("Which pose to use for lower body when blend factor = 1 (hips and legs)")]
        public MSDKAIMotionSynthesizer.PoseSource LowerBodySource = MSDKAIMotionSynthesizer.PoseSource.AIMotionSynthesizer;

        /// <summary>Whether blend factor is controlled manually or by input activity.</summary>
        [Tooltip("How to control the blend factor between body tracking and AIMotionSynthesizer")]
        public BlendMode BlendMode = BlendMode.Input;

        /// <summary>Manual blend factor when <see cref="BlendMode"/> is Manual. 0 = body tracking, 1 = AI motion synthesizer.</summary>
        [Tooltip("Blend factor: 0 = full body tracking, 1 = blended pose based on upper/lower body options")]
        [Range(0f, 1f)]
        public float BlendFactor;

        /// <summary>Velocity input when using manual blend mode (meters/second).</summary>
        [Tooltip("Manual velocity for AI Motion Synthesizer (used when BlendMode is Manual)")]
        public Vector3 ManualVelocity = Vector3.zero;

        /// <summary>Direction input when using manual blend mode.</summary>
        [Tooltip("Manual direction for AI Motion Synthesizer (used when BlendMode is Manual)")]
        public Vector3 ManualDirection = Vector3.forward;

        /// <summary>Seconds to transition from 0 to 1 blend when input becomes active.</summary>
        [Tooltip("Time in seconds to transition blend factor when input provider becomes active (0 -> 1)")]
        public float BlendInTime = 0.25f;

        /// <summary>Seconds to transition from 1 to 0 blend when input becomes inactive.</summary>
        [Tooltip("Time in seconds to transition blend factor when input provider becomes inactive (1 -> 0)")]
        public float BlendOutTime = 1.0f;

        /// <summary>Enable debug skeleton visualization.</summary>
        [Tooltip("Enable debug drawing of AIMotionSynthesizer skeleton")]
        public bool DebugDrawAIMotionSynthesizer = false;

        /// <summary>Color for debug skeleton lines.</summary>
        [Tooltip("Color for AIMotionSynthesizer skeleton visualization")]
        public Color DebugAIMotionSynthesizerColor = Color.blue;
    }
}
