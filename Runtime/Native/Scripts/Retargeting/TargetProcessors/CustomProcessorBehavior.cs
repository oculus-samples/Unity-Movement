// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Behavior class referenced by <see cref="CustomProcessor"/>. Inherit to define your own.
    /// </summary>
    public class CustomProcessorBehavior : MonoBehaviour
    {
        /// <summary>
        /// Initializes the processor.
        /// </summary>
        /// <param name="retargeter">The native retargeter.</param>
        public virtual void Initialize(CharacterRetargeter retargeter)
        {
        }

        /// <summary>
        /// Destroys the processor.
        /// </summary>
        public virtual void Destroy()
        {
        }

        /// <summary>
        /// Processes the retargeted pose values in Update.
        /// </summary>
        /// <param name="pose">The pose that the constraint needs to affect.</param>
        /// <param name="weight">Weight.</param>
        public virtual void UpdatePose(
            ref NativeArray<NativeTransform> pose,
            float weight)
        {
        }

        /// <summary>
        /// Processes the target pose values in LateUpdate.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be updated by the constraint.</param>
        /// <param name="weight">Weight.</param>
        public virtual void LateUpdatePose(
            ref NativeArray<NativeTransform> currentPose,
            ref NativeArray<NativeTransform> targetPose,
            float weight)
        {
        }
    }
}
