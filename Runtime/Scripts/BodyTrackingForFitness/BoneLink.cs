// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Stores information about a bone connections in a skeleton/armature
    /// </summary>
    /// <typeparam name="TBoneId">Generic to disambiguate kind of skeleton</typeparam>
    public class BoneLink<TBoneId>
    {
        /// <summary>
        /// The bone that this data describes
        /// </summary>
        public TBoneId id;

        /// <summary>
        /// Which bone could be considered a parent bone (that this could be connected
        /// to in a hierarchy). Root bones should have a parent that equates to -1.
        /// </summary>
        public TBoneId parent;

        /// <summary>
        /// Which bone this should point to. For example, the spine points to the neck, the neck
        ///  points to the head. The head is a terminus, and should point at a bone id equal to -1.
        /// </summary>
        public TBoneId next;

        /// <summary>
        /// Bones should be thought of as capsules/cylinders. This defines the bone's length axis.
        /// </summary>
        public Quaternion alignment;

        /// <summary>
        /// The expected position and orientation during a T-pose
        /// </summary>
        public Pose tPose;

        /// <summary>
        /// The length of the bone in the T-pose. Calculated if not supplied in the constructor.
        /// </summary>
        public float length;

        /// <summary>
        /// Bones that are parented to this bone, likely including the <see cref="next"/> bone
        /// </summary>
        public int[] children;

        /// <param name="id"><see cref="id"/></param>
        /// <param name="parent"><see cref="parent"/></param>
        /// <param name="next"><see cref="next"/></param>
        /// <param name="alignment"><see cref="alignment"/></param>
        /// <param name="tPose"><see cref="tPose"/></param>
        public BoneLink(TBoneId id, TBoneId parent, TBoneId next, Quaternion alignment,
        PoseTuple tPose)
        {
            this.id = id;
            this.parent = parent;
            this.next = next;
            this.alignment = alignment;
            this.tPose = tPose;
            length = 0;
        }

        /// <summary>
        /// Enables syntax sugar, for defining the bone link with a tuple
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public static implicit operator BoneLink<TBoneId>(
        (TBoneId id, TBoneId parent, TBoneId next, Vector3 eulerAlignment, PoseTuple tPose) link)
        {
            return new BoneLink<TBoneId>(link.id, link.parent, link.next,
                Quaternion.Euler(link.eulerAlignment), link.tPose);
        }

        /// <summary>
        /// Used to calculate the <see cref="length"/>
        /// </summary>
        /// <param name="poses">the full T-pose of a skeleton</param>
        public void UpdateLength(IList<BoneLink<TBoneId>> poses)
        {
            int endIndex = (int)(object)next;
            if (endIndex < 0)
            {
                length = 0;
                return;
            }
            Vector3 start = poses[(int)(object)id].tPose.position;
            Vector3 end = poses[endIndex].tPose.position;
            Vector3 delta = end - start;
            length = delta.magnitude;
        }
    }

    /// <summary>
    /// This class is designed for simple data entry when describing a T-pose. It is
    /// functionally identical to the <see cref="UnityEngine.Pose"/> class.
    /// </summary>
    public struct PoseTuple
    {
        /// <inheritdoc cref="Pose.position"/>
        public Vector3 position;
        /// <inheritdoc cref="Pose.rotation"/>
        public Quaternion rotation;

        public PoseTuple(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        /// <summary>
        /// Automatically converts from a tuple
        /// </summary>
        public static implicit operator PoseTuple((Vector3 pos, Quaternion rot) data) =>
            new PoseTuple(data.pos, data.rot);

        /// <summary>
        /// Automatically converts to a <see cref="Pose"/>
        /// </summary>
        public static implicit operator Pose(PoseTuple pose) =>
            new Pose(pose.position, pose.rotation);

        /// <summary>
        /// Automatically converts from a <see cref="Pose"/>
        /// </summary>
        public static implicit operator PoseTuple(Pose pose) =>
            new PoseTuple(pose.position, pose.rotation);

        /// <summary>
        /// Automatically converts from two tuples, position and euler rotation
        /// </summary>
        public static implicit operator PoseTuple(
            ((float x, float y, float z) pos, (float pitch, float yaw, float roll) rot) data) =>
            new PoseTuple(new Vector3(data.pos.x, data.pos.y, data.pos.z),
                Quaternion.Euler(data.rot.pitch, data.rot.yaw, data.rot.roll));

        /// <summary>
        /// Automatically converts from two tuples, position and quaternion rotation
        /// </summary>
        public static implicit operator PoseTuple(
            ((float x, float y, float z) pos, (float x, float y, float z, float w) quat) data) =>
            new PoseTuple(new Vector3(data.pos.x, data.pos.y, data.pos.z),
                new Quaternion(data.quat.x, data.quat.y, data.quat.z, data.quat.w));
    }
}
