// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Use this script to pull the skeleton's bones to follow this transform.
    /// If a tracked skeleton's rigged-mesh transform is stationary, this script
    /// will translate the skeleton's apparent position/rotation to this object.
    ///
    /// More context:
    /// If a rigged mesh is stationary, but the OVRCameraRig is moving, put this
    /// script on an object moving with the OVRCameraRig, and have the
    /// SkeletonPostprocessing of the retargeting layer use this script's
    /// SkeletonPostprocess function
    /// </summary>
    public class SkeletonTranslateProcessor : MonoBehaviour, IOVRSkeletonProcessor
    {
        /// <summary>
        /// Module for updating objects that care if this processor is working
        /// </summary>
        [System.Serializable]
        public class NotifyOnChange
        {
            /// <summary>
            /// Notifies that <see cref="SkeletonHandAdjustment.HandsAreOffset"/>
            /// should change when this object adjusts bone positioning behavior.
            /// </summary>
            public SkeletonHandAdjustment[] hands;
            /// <summary>
            /// The bone-containing animator that follows the player transform
            /// or moves back to it's neutral position/rotation.
            /// </summary>
            public TransformsFollowMe AlternativeCharacterMover;
            public UnityEvent OnEnable;
            public UnityEvent OnDisable;

            public void Enable()
            {
                OnEnable.Invoke();
                System.Array.ForEach(hands, h => h.HandsAreOffset = true);
                if (AlternativeCharacterMover != null)
                {
                    AlternativeCharacterMover.SetTransformsToZero();
                    AlternativeCharacterMover.enabled = false;
                }
            }

            public void Disable()
            {
                OnDisable.Invoke();
                System.Array.ForEach(hands, h => h.HandsAreOffset = false);
                if (AlternativeCharacterMover != null)
                {
                    AlternativeCharacterMover.enabled = true;
                }
            }
        }

        /// <summary>
        /// Which transform will offset the skeleton passed into this skeleton processor
        /// </summary>
        [SerializeField] private Transform _transformOffsetForSkeleton;
        [SerializeField] private NotifyOnChange _notifyOnStateChange = new NotifyOnChange();

        public bool EnableSkeletonProcessing { get => enabled; set => enabled = value; }

        public string SkeletonProcessorLabel => name;

        private void OnEnable()
        {
            _notifyOnStateChange.Enable();
        }

        private void OnDisable()
        {
            _notifyOnStateChange.Disable();
        }

        private void Reset()
        {
            _transformOffsetForSkeleton = transform;
        }

        /// <summary>
        /// Start method ensures toggle in Unity editor for this script
        /// </summary>
        protected void Start()
        {
            if (_transformOffsetForSkeleton == null)
            {
                _transformOffsetForSkeleton = transform;
            }
        }

        /// <summary>
        /// Applies transform position and rotation to the given OVRSkeleton data
        /// </summary>
        public void ProcessSkeleton(OVRSkeleton skeleton)
        {
            IList<OVRBone> bones = skeleton != null ? skeleton.Bones : null;
            if (bones == null || bones.Count == 0 || !enabled)
            {
                return;
            }
            Vector3 p = _transformOffsetForSkeleton.position;
            Quaternion r = _transformOffsetForSkeleton.rotation;
            for (int i = 0; i < bones.Count; ++i)
            {
                Transform bone = bones[i].Transform;
                bone.position = p + r * bone.position;
                bone.rotation = r * bone.rotation;
            }
        }
    }
}
