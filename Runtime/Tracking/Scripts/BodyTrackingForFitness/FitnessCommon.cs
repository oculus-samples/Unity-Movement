// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
#if ISDK_DEFINED
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction;
#endif
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    public class FitnessCommon : MonoBehaviour
    {
        public static int GetParentIndex(SkeletonData sourceData, int boneIndex)
        {
            var allJoints = sourceData.Joints;
            var parentName = sourceData.ParentJoints[boneIndex];
            int parentIndex = -1;
            for (int i = 0; i < allJoints.Length; i++)
            {
                if (allJoints[i] == parentName)
                {
                    parentIndex = i;
                    break;
                }
            }

            return parentIndex;
        }

#if ISDK_DEFINED
        /// <summary>
        /// Gets joint pose local relative to its praent, assuming root is available.
        /// </summary>
        /// <param name="data">Body data asset, used to seek parent.</param>
        /// <param name="posesFromRoot">Body pose to evaluate.</param>
        /// <param name="bodyJointId">Joint index.</param>
        /// <param name="pose">Pose to return.</param>
        /// <returns>True if pose obtained; false if not.</returns>
        public static bool GetJointPoseLocalIfFromRootIsKnown(
            SkeletonData data,
            IBodyPose posesFromRoot,
            BodyJointId bodyJointId,
            out Pose pose)
        {
            int id = (int)bodyJointId;
            if (id < 0 || id >= data.TPoseArray.Length)
            {
                pose = default;
                return false;
            }

            bool obtainedBonePose = posesFromRoot.GetJointPoseFromRoot(bodyJointId, out pose);
            if (!obtainedBonePose)
            {
                return false;
            }

            int parent = (int)FitnessCommon.GetParentIndex(data, id);
            if (parent >= 0)
            {
                obtainedBonePose = posesFromRoot.GetJointPoseFromRoot(
                    (BodyJointId)parent, out Pose parentPose);
                if (!obtainedBonePose)
                {
                    return false;
                }

                Pose inverseParent = default;
                PoseUtils.Inverse(parentPose, ref inverseParent);
                pose = new Pose(
                    inverseParent.rotation * pose.position + inverseParent.position,
                    inverseParent.rotation * pose.rotation);
            }

            return true;
        }
#endif
    }
}
