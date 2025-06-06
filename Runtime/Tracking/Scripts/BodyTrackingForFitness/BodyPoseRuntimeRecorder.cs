// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections;
#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Collections;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Sample implementation of a body pose recorder
    /// </summary>
    ///
    public class BodyPoseRuntimeRecorder : MonoBehaviour
#if ISDK_DEFINED
        , IBodyPose, ISkeletonMapping
#endif
    {
#if ISDK_DEFINED
        private static class BodyPoseRuntimeRecorderTooltips
        {
            public const string Source = "What body pose to record from";

            public const string BodyPose = "The recorded body pose data";

            public const string OnRecord = "Callback to execute when recording is finished";
        }

        /// <summary>
        /// What body pose to record from.
        /// </summary>
        [Tooltip(BodyPoseRuntimeRecorderTooltips.Source)]
        [Interface(typeof(IBodyPose))]
        [SerializeField]
        protected UnityEngine.Object _source;

        /// <summary>
        /// The recorded body pose data.
        /// </summary>
        [Tooltip(BodyPoseRuntimeRecorderTooltips.BodyPose)]
        public Pose[] bodyPose = Array.Empty<Pose>();

        /// <inheritdoc cref="IBodyPose.WhenBodyPoseUpdated"/>
        public event Action WhenBodyPoseUpdated = delegate { };

        /// <summary>
        /// Callback to execute when recording is finished.
        /// </summary>
        [Tooltip(BodyPoseRuntimeRecorderTooltips.OnRecord)]
        public UnityEvent _onRecord = new UnityEvent();

        /// <inheritdoc cref="IBodyPose.SkeletonMapping"/>
        public ISkeletonMapping SkeletonMapping => this;

        /// <inheritdoc cref="ISkeletonMapping.Joints"/>
        public IEnumerableHashSet<BodyJointId> Joints => FullBodySkeletonTPose.TPose.Joints;

        /// <inheritdoc cref="ISkeletonMapping.TryGetParentJointId"/>
        public bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent) =>
            FullBodySkeletonTPose.TPose.TryGetParentJointId(jointId, out parent);

        /// <inheritdoc cref="IBodyPose.GetJointPoseLocal"/>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
        {
            return FullBodySkeletonTPose.GetJointPoseLocalIfFromRootIsKnown(
                this, bodyJointId, out pose);
        }

        /// <inheritdoc cref="IBodyPose.GetJointPoseFromRoot"/>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
        {
            int id = (int)bodyJointId;
            if (Application.isPlaying)
            {
                if (id >= 0 && id < bodyPose.Length)
                {
                    pose = bodyPose[id];
                    return true;
                }
            }
            else
            {
                pose = FullBodySkeletonTPose.TPose.GetTPose(id);
                return true;
            }
            pose = default;
            return false;
        }

        /// <summary>
        /// Used by UI to record the current input body pose in the given number of seconds
        /// </summary>
        public void PressRecordButton(float timerTillRecordStarts) =>
            StartCoroutine(RecordInSeconds(timerTillRecordStarts));

        private IEnumerator RecordInSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Copy(_source as IBodyPose);
            _onRecord.Invoke();
        }

        /// <summary>
        /// Copies the data from a given body pose into this recorder
        /// </summary>
        public void Copy(IBodyPose bodyPoseInput)
        {
            Array.Resize(ref bodyPose, FullBodySkeletonTPose.TPose.ExpectedBoneCount);
            for (int i = 0; i < bodyPose.Length; ++i)
            {
                bodyPoseInput.GetJointPoseFromRoot((BodyJointId)i, out Pose bonePose);
                bodyPose[i] = bonePose;
            }
            WhenBodyPoseUpdated.Invoke();
        }
#endif
    }
}
