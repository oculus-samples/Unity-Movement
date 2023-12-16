// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Moves other transforms to this object constantly during LateUpdate
    /// </summary>
    public class TransformsFollowMe : MonoBehaviour
    {
        /// <summary>
        /// Used by objects being moved to mark who is moving them.
        /// </summary>
        public class Follower : MonoBehaviour
        {
            public TransformsFollowMe following;
        }

        /// <summary>
        /// Other Transforms that will follow this Transform
        /// </summary>
        [SerializeField]
        [Tooltip(TransformsFollowTooltips.FollowingTransforms)]
        private Transform[] _transformsFollowingMe;

        /// <summary>
        /// cached transform, for access performance
        /// </summary>
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void Start()
        {
            foreach (Transform who in _transformsFollowingMe)
            {
                Follower follower = who.gameObject.AddComponent<Follower>();
                follower.following = this;
            }
        }

        protected void LateUpdate()
        {
            DoFollowMe();
        }

        /// <summary>
        /// Moves the target transforms positions and rotations to this transform
        /// </summary>
        public void DoFollowMe()
        {
            if (_transformsFollowingMe == null || _transformsFollowingMe.Length == 0)
            {
                return;
            }
            foreach (Transform follower in _transformsFollowingMe)
            {
                follower.position = _transform.position;
                follower.rotation = _transform.rotation;
            }
        }

        /// <summary>
        /// Used to reset transforms that are following this transform
        /// </summary>
        public void SetTransformsToZero()
        {
            foreach (Transform follower in _transformsFollowingMe)
            {
                follower.localPosition = Vector3.zero;
                follower.localRotation = Quaternion.identity;
            }
        }
    }
}
