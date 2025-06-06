// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections;
#if ISDK_DEFINED
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Collections;
#endif
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Simpler than using <see cref="Oculus.Interaction.Body.PoseDetection.PoseFromBody"/>, and
    /// setting up it's associated <see cref="Oculus.Interaction.Input.DataSource{TData}"/>.
    /// </summary>
    public class OVRBodyPose : OVRBody
#if ISDK_DEFINED
        , IBodyPose, IBody, ISkeletonMapping
#endif
    {
        /// <summary>
        /// How many frames have been processed by this body pose reader
        /// </summary>
        private int _iterationNumber;

#if ISDK_DEFINED

        /// <inheritdoc cref="IBodyPose.SkeletonMapping"/>
        public ISkeletonMapping SkeletonMapping => this;
#endif

        /// <inheritdoc cref="IBody.IsConnected"/>
        public bool IsConnected => BodyState != null;

        /// <inheritdoc cref="IBody.IsHighConfidence"/>
        public bool IsHighConfidence => BodyState != null && BodyState.Value.Confidence > 0.5f;

        /// <inheritdoc cref="IBody.IsTrackedDataValid"/>
        public bool IsTrackedDataValid => BodyState != null &&
                                          BodyState.Value.CalibrationStatus ==
                                          OVRPlugin.BodyTrackingCalibrationState.Valid;

        /// <inheritdoc cref="IBody.Scale"/>
        public float Scale => 1;

        /// <inheritdoc cref="IBody.CurrentDataVersion"/>
        public int CurrentDataVersion => _iterationNumber;

#if ISDK_DEFINED
        /// <inheritdoc cref="ISkeletonMapping.Joints"/>
        public IEnumerableHashSet<BodyJointId> Joints => FullBodySkeletonTPose.TPose.Joints;
#endif

        /// <inheritdoc cref="IBody.WhenBodyUpdated"/>
        public event Action WhenBodyUpdated = delegate { };

        /// <inheritdoc cref="IBodyPose.WhenBodyPoseUpdated"/>
        public event Action WhenBodyPoseUpdated = delegate { };

        /// <inheritdoc cref="IBody.GetRootPose"/>
        public bool GetRootPose(out Pose pose)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            return true;
        }


#if ISDK_DEFINED
        /// <inheritdoc cref="ISkeletonMapping.TryGetParentJointId"/>
        public bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent) =>
            FullBodySkeletonTPose.TPose.TryGetParentJointId(jointId, out parent);

        /// <inheritdoc cref="IBody.GetJointPose"/>
        public bool GetJointPose(BodyJointId bodyJointId, out Pose pose)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                pose = default;
                return false;
            }
#endif
            OVRPlugin.BodyJointLocation[] jointList;
            int i = (int)bodyJointId;
            if (BodyState != null && (jointList = BodyState.Value.JointLocations) != null &&
               i >= 0 && i < jointList.Length)
            {
                pose = new Pose(
                    jointList[i].Pose.Position.FromFlippedZVector3f(),
                    jointList[i].Pose.Orientation.FromFlippedZQuatf());
                return true;
            }
            pose = default;
            return false;
        }
#endif

#if ISDK_DEFINED
        /// <inheritdoc cref="IBodyPose.GetJointPoseLocal"/>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            FullBodySkeletonTPose.GetJointPoseLocalIfFromRootIsKnown(this, bodyJointId, out pose);

        /// <inheritdoc cref="IBodyPose.GetJointPoseFromRoot"/>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            GetJointPose(bodyJointId, out pose);
#endif

        /// <summary>
        /// OVRBody doesn't implement a Start method, so this doesn't interfere with it's work
        /// </summary>
        protected virtual void Start()
        {
            StartCoroutine(UpdateThatDoesntOverwriteOvrBodyUpdate());
        }

        private void Reset()
        {
            ProvidedSkeletonType = OVRPlugin.BodyJointSet.FullBody;
        }

        private IEnumerator UpdateThatDoesntOverwriteOvrBodyUpdate()
        {
            while (gameObject != null)
            {
                yield return null;
                if (!enabled || BodyState == null)
                {
                    continue;
                }
                IterateBodyTracking();
            }
        }

        private void IterateBodyTracking()
        {
            WhenBodyUpdated.Invoke();
            WhenBodyPoseUpdated.Invoke();
            ++_iterationNumber;
        }
    }
}
