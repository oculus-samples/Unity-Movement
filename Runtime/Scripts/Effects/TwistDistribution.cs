// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Attributes;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Twist distribution from a segment to a list of destination twist joints.
    /// </summary>
    public class TwistDistribution : MonoBehaviour
    {
        /// <summary>
        /// The global weight of the twist joints.
        /// </summary>
        [SerializeField, Range(0, 1)]
        [Tooltip(TwistDistributionTooltips.GlobalWeight)]
        protected float _weight = 1f;

        /// <summary>
        /// The start transform on the opposite side of the twist source (like an elbow).
        /// </summary>
        [SerializeField]
        [Tooltip(TwistDistributionTooltips.SegmentStart)]
        protected Transform _segmentStart;

        /// <summary>
        /// The target transform containing the twist (like a wrist).
        /// </summary>
        [SerializeField]
        [Tooltip(TwistDistributionTooltips.SegmentEnd)]
        protected Transform _segmentEnd;

        /// <summary>
        /// Optional. Assign a different transform to be used for the Segment End up vector.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(TwistDistributionTooltips.SegmentEndUpTransform)]
        protected Transform _segmentEndUpTransform;

        /// <summary>
        /// Class structure for the twist joint.
        /// </summary>
        [System.Serializable]
        public class TwistJoint
        {
            /// <summary>
            /// The twist joint transform.
            /// </summary>
            [Tooltip(TwistDistributionTooltips.TwistJointTooltips.Joint)]
            public Transform Joint;

            /// <summary>
            /// The weight of the source transform's rotation on the twist joint.
            /// </summary>
            [Range(0, 1)]
            [Tooltip(TwistDistributionTooltips.TwistJointTooltips.Weight)]
            public float Weight;

            /// <summary>
            /// The rotation of the twist joint at rest.
            /// </summary>
            [HideInInspector]
            public Quaternion RestQuaternion;

            /// <summary>
            /// The segment's up axis.
            /// </summary>
            [HideInInspector]
            public Vector3 SegmentEndUpAxis;
        }

        /// <summary>
        /// The list of twist joints to affect by the source transform's rotation.
        /// </summary>
        [SerializeField, Space]
        [Tooltip(TwistDistributionTooltips.TwistJoints)]
        protected TwistJoint[] _twistJoints;

        /// <summary>
        /// The forward axis for the twist joints, one that points along the twist axis toward segment end.
        /// </summary>
        [SerializeField, Space]
        [Tooltip(TwistDistributionTooltips.TwistForwardAxis)]
        protected Vector3 _twistForwardAxis = new Vector3(1, 0, 0);

        /// <summary>
        /// The up axis for the twist joints, one that matches the segment end up axis.
        /// </summary>
        [SerializeField]
        [Tooltip(TwistDistributionTooltips.TwistUpAxis)]
        protected Vector3 _twistUpAxis = new Vector3(0, 1, 0);

        private Transform _currentEndUpTransform;
        private Quaternion _twistLocalRotationAxisOffset;
        private float[] _spacing;
        private Vector3 _currentLookAtDir;

        private void Start()
        {
            // Use custom segment up transform if needed.
            _currentEndUpTransform = _segmentEndUpTransform != null ? _segmentEndUpTransform : _segmentEnd;

            _spacing = new float[_twistJoints.Length];
            for (int i = 0; i < _twistJoints.Length; i++)
            {
                // Get normalized distance from twist joint to segment end.
                _spacing[i] =
                    (_segmentEnd.position - _twistJoints[i].Joint.position).magnitude /
                    (_segmentEnd.position - _segmentStart.position).magnitude;

                // Determine a segment up vector using this twist's up vector.
                _twistJoints[i].SegmentEndUpAxis = _currentEndUpTransform.InverseTransformVector(_twistJoints[i].Joint.TransformVector(_twistUpAxis));
            }

            // Offset to joint's local rotation axis.
            _twistLocalRotationAxisOffset = Quaternion.Inverse(Quaternion.LookRotation(_twistForwardAxis, _twistUpAxis));

            Initialize();
        }

        /// <summary>
        /// Initialize input and twist joint data.
        /// </summary>
        private void Initialize()
        {
            Assert.IsNotNull(_segmentStart);
            Assert.IsNotNull(_segmentEnd);

            for (int i = 0; i < _twistJoints.Length; i++)
            {
                _twistJoints[i].RestQuaternion = _twistJoints[i].Joint.localRotation;
            }
        }

        /// <summary>
        /// Apply twist distribution.
        /// </summary>
        public void ApplyTwist()
        {
            if (!enabled)
            {
                return;
            }

            SpaceJoints();
            TwistJoints();
        }

        /// <summary>
        /// Distribute bone positions between start and start & end segment
        /// </summary>
        private void SpaceJoints()
        {
            for (int i = 0; i < _twistJoints.Length; i++)
            {
                _twistJoints[i].Joint.position =
                    _segmentStart.position +
                    (_segmentEnd.position - _segmentStart.position) * (1f - _spacing[i]);
            }
        }

        /// <summary>
        /// Rotate twist joints around forward axis according to their weight.
        /// </summary>
        private void TwistJoints()
        {
            _currentLookAtDir = (_segmentEnd.position - _segmentStart.position) * 2f;

            for (int i = 0; i < _twistJoints.Length; i++)
            {
                // Reset joint.
                _twistJoints[i].Joint.localRotation = _twistJoints[i].RestQuaternion;

                // Blend twist between rest rotation and fully twisted using weight.
                _twistJoints[i].Joint.rotation =
                    Quaternion.Slerp(
                        _twistJoints[i].Joint.parent.rotation * _twistJoints[i].RestQuaternion,
                        Quaternion.LookRotation(
                            _currentLookAtDir,
                            _currentEndUpTransform.TransformVector(_twistJoints[i].SegmentEndUpAxis)) *
                            _twistLocalRotationAxisOffset,
                        _twistJoints[i].Weight * _weight);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_segmentStart == null || _segmentEnd == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            if (_segmentEndUpTransform != null)
            {
                Gizmos.DrawSphere(_segmentEndUpTransform.position, 0.01f);
            }
            Gizmos.DrawSphere(_segmentEnd.position, 0.01f);
            Gizmos.DrawSphere(_segmentStart.position, 0.01f);

            Gizmos.color = Color.yellow;
            foreach (var twistJoint in _twistJoints)
            {
                Gizmos.DrawSphere(twistJoint.Joint.position, 0.01f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_segmentStart.position, _segmentStart.position + _segmentStart.rotation * _twistForwardAxis * 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_segmentStart.position, _segmentStart.position + _segmentEnd.rotation * _twistUpAxis * 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_segmentStart.position, _segmentStart.position + _segmentEnd.up * 0.1f);
        }
    }
}
