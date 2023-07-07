// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for twist distribution data.
    /// </summary>
    public interface ITwistDistributionData
    {
        /// <summary>
        /// The OVR Skeleton component for the character.
        /// </summary>
        public OVRCustomSkeleton ConstraintSkeleton { get; }

        /// <summary>
        /// The Animator component for the character.
        /// </summary>

        public Animator ConstraintAnimator { get; }

        /// <summary>
        /// The start transform on the opposite side of the twist source (like an elbow).
        /// </summary>
        public Transform SegmentStart { get; }

        /// <summary>
        /// The target transform containing the twist (like a wrist).
        /// </summary>
        public Transform SegmentEnd { get; }

        /// <summary>
        /// Optional. Assign a different transform to be used for the Segment End up vector.
        /// </summary>
        public Transform SegmentUp { get; }

        /// <summary>
        /// The array of twist joints and their weights to be affected by the source transform's rotation.
        /// </summary>
        public WeightedTransformArray TwistNodes { get; }

        /// <summary>
        /// The array of twist node up directions.
        /// </summary>
        public Vector3[] TwistNodeUps { get; }

        /// <summary>
        /// The proportional space between each twist node.
        /// </summary>
        public float[] TwistNodeSpacings { get; }

        /// <summary>
        /// The twist forward axis.
        /// </summary>
        public Vector3 TwistForwardAxis { get; }

        /// <summary>
        /// The twist up axis.
        /// </summary>
        public Vector3 TwistUpAxis { get; }

        /// <summary>
        /// The name of the twist nodes weighted transform array property.
        /// </summary>
        public string TwistNodesProperty { get; }

        /// <summary>
        /// Indicates if bone transforms are valid or not.
        /// </summary>
        /// <returns>True if bone transforms are valid, false if not.</returns>
        public bool IsBoneTransformsDataValid();
    }

    /// <summary>
    /// Twist distribution data used by the twist distribution job.
    /// Implements the twist distribution data interface.
    /// </summary>
    [System.Serializable]
    public struct TwistDistributionData : IAnimationJobData, ITwistDistributionData
    {
        /// <summary>
        /// Axis type for TwistDistribution.
        /// </summary>
        public enum Axis
        {
            /// <summary>X Axis.</summary>
            X,
            /// <summary>Y Axis.</summary>
            Y,
            /// <summary>Z Axis.</summary>
            Z
        }

        // Interface implementation
        /// <inheritdoc />
        OVRCustomSkeleton ITwistDistributionData.ConstraintSkeleton => _skeleton;

        /// <inheritdoc />
        Animator ITwistDistributionData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        Transform ITwistDistributionData.SegmentStart => _segmentStart;

        /// <inheritdoc />
        Transform ITwistDistributionData.SegmentUp => _segmentUp;

        /// <inheritdoc />
        Transform ITwistDistributionData.SegmentEnd => _segmentEnd;

        /// <inheritdoc />
        Vector3 ITwistDistributionData.TwistForwardAxis => Convert(_twistForwardAxis, _invertForwardAxis);

        /// <inheritdoc />
        Vector3 ITwistDistributionData.TwistUpAxis => Convert(_twistUpAxis, _invertUpAxis);

        /// <inheritdoc />
        WeightedTransformArray ITwistDistributionData.TwistNodes => _twistNodes;

        /// <inheritdoc />
        Vector3[] ITwistDistributionData.TwistNodeUps => _twistNodeUps;

        /// <inheritdoc />
        float[] ITwistDistributionData.TwistNodeSpacings => _twistNodeSpacings;

        /// <inheritdoc />
        string ITwistDistributionData.TwistNodesProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_twistNodes));

        /// <inheritdoc cref="ITwistDistributionData.ConstraintSkeleton"/>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.Skeleton)]
        private OVRCustomSkeleton _skeleton;

        /// <inheritdoc cref="ITwistDistributionData.ConstraintAnimator"/>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.Animator)]
        private Animator _animator;

        /// <inheritdoc cref="ITwistDistributionData.SegmentStart"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.SegmentStart)]
        private Transform _segmentStart;

        /// <inheritdoc cref="ITwistDistributionData.SegmentUp"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.SegmentUp)]
        private Transform _segmentUp;

        /// <inheritdoc cref="ITwistDistributionData.SegmentEnd"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.SegmentEnd)]
        private Transform _segmentEnd;

        /// <inheritdoc cref="ITwistDistributionData.TwistNodes"/>
        [SyncSceneToStream, SerializeField, WeightRange(0f, 1f)]
        [Tooltip(TwistDistributionDataTooltips.TwistNodes)]
        private WeightedTransformArray _twistNodes;

        /// <inheritdoc cref="ITwistDistributionData.TwistForwardAxis"/>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.TwistForwardAxis)]
        private Axis _twistForwardAxis;

        /// <inheritdoc cref="ITwistDistributionData.TwistUpAxis"/>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.TwistUpAxis)]
        private Axis _twistUpAxis;

        /// <summary>
        /// If true, invert the forward axis.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.InvertForwardAxis)]
        private bool _invertForwardAxis;

        /// <summary>
        /// If true, invert the up axis.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(TwistDistributionDataTooltips.InvertUpAxis)]
        private bool _invertUpAxis;

        [NotKeyable, SerializeField, HideInInspector]
        private Vector3[] _twistNodeUps;

        [NotKeyable, SerializeField, HideInInspector]
        private float[] _twistNodeSpacings;

        /// <summary>
        /// Assign the OVR Skeleton component.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton to be assigned.</param>
        public void AssignOVRSkeleton(OVRCustomSkeleton skeleton)
        {
            _skeleton = skeleton;
        }

        /// <summary>
        /// Assign the Animator component.
        /// </summary>
        /// <param name="skeleton">The Animator to be assigned.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Assign twist nodes.
        /// </summary>
        /// <param name="twistNodes">Twist node transforms.</param>
        public void AssignTwistNodes(Transform[] twistNodes)
        {
            _twistNodes.Clear();
            foreach (var twistNode in twistNodes)
            {
                _twistNodes.Add(new WeightedTransform(twistNode, 1.0f));
            }
        }

        /// <summary>
        /// Computes twist node data required for constraint to work.
        /// </summary>
        public void ComputeTwistNodeData()
        {
            _twistNodeUps = new Vector3[_twistNodes.Count];
            _twistNodeSpacings = new float[_twistNodes.Count];
            Vector3 upAxis = Convert(_twistUpAxis, _invertUpAxis);
            for (int i = 0; i < _twistNodeUps.Length; i++)
            {
                var sourceTransform = _twistNodes[i].transform;
                _twistNodeUps[i] = _segmentUp.InverseTransformVector(sourceTransform.TransformVector(upAxis));
                _twistNodeSpacings[i] = 1f - (_segmentEnd.position - sourceTransform.position).magnitude /
                                             (_segmentEnd.position - _segmentStart.position).magnitude;
            }
        }

        /// <summary>
        /// Assign segments
        /// </summary>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <param name="segmentUp"></param>
        public void AssignSegments(Transform segmentStart, Transform segmentEnd, Transform segmentUp)
        {
            _segmentStart = segmentStart;
            _segmentEnd = segmentEnd;
            _segmentUp = segmentUp;
        }

        private static Vector3 Convert(Axis axis, bool inverted)
        {
            int sign = inverted ? -1 : 1;
            if (axis == Axis.X)
            {
                return sign * Vector3.right;
            }

            if (axis == Axis.Y)
            {
                return sign * Vector3.up;
            }

            return sign * Vector3.forward;
        }

        /// <inheritdoc />
        public bool IsBoneTransformsDataValid()
        {
            return (_skeleton != null && _skeleton.IsDataValid) ||
                (_animator != null);
        }

        bool IAnimationJobData.IsValid()
        {
            if (_skeleton == null && _animator == null)
            {
                Debug.LogError("Skeleton or animator not set up.");
                return false;
            }

            if (_segmentStart == null || _segmentEnd == null)
            {
                Debug.LogError("Segments not set up.");
                return false;
            }

            if (_twistNodeSpacings == null || _twistNodeUps == null ||
                _twistNodeSpacings.Length == 0 || _twistNodeUps.Length == 0)
            {
                Debug.LogError("Twist node data arrays not set up.");
                return false;
            }

            for (int i = 0; i < _twistNodes.Count; ++i)
            {
                if (_twistNodes[i].transform == null)
                {
                    Debug.LogError("Twist node transform reference is null.");
                    return false;
                }
            }

            if (_twistNodeSpacings.Length != _twistNodes.Count ||
                _twistNodeSpacings.Length != _twistNodes.Count)
            {
                Debug.LogError("Twist node data arrays do not match the length of twist node array.");
                return false;
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _skeleton = null;
            _animator = null;
            _segmentStart = null;
            _segmentEnd = null;
            _twistForwardAxis = Axis.Z;
            _twistUpAxis = Axis.Y;
            _twistNodes.Clear();
            _twistNodeUps = null;
            _twistNodeSpacings = null;
        }
    }

    /// <summary>
    /// Twist Distribution constraint. This should be enabled to
    /// begin with, so that it can compute metadata before the
    /// character can begin animating.
    /// </summary>
    [DisallowMultipleComponent]
    public class TwistDistributionConstraint : RigConstraint<
        TwistDistributionJob,
        TwistDistributionData,
        TwistDistributionJobBinder<TwistDistributionData>>,
        IOVRSkeletonConstraint
    {
        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
