// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// A common base class for <see cref="ISkeletonProcessor"/>
    /// <see cref="MonoBehaviour"/>s. Allows common editor UI Drawer.
    /// </summary>
    public class SkeletonProcessorBehaviour : MonoBehaviour, IOVRSkeletonProcessor
    {
        public virtual string SkeletonProcessorLabel
        {
            get
            {
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
                return DefaultLabel(this);
            }
        }

        public bool EnableSkeletonProcessing { get => enabled; set => enabled = value; }

        public static string DefaultLabel(IOVRSkeletonProcessor processor)
        {
            string typeName = processor.GetType().Name;
            int lastDot = typeName.LastIndexOf('.');
            if (lastDot != -1)
            {
                typeName = typeName.Substring(lastDot + 1);
            }
            return typeName;
        }

        /// <summary>
        /// Applies hand joint position and rotation to the given OVRSkeleton data
        /// </summary>
        public virtual void ProcessSkeleton(OVRSkeleton skeleton)
        {
            throw new System.NotImplementedException($"{GetType().Name} is" +
                $" missing `{nameof(ProcessSkeleton)}` implementation");
        }
    }
}
