// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// Moves the given sphere collider to be appropriate to the feet of the user
    /// </summary>
    public class SphereColliderStaysBelowHips : MonoBehaviour
    {
        /// <summary>
        /// The sphere collider at the foot of the body tracked user
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.Collider)]
        [SerializeField]
        protected SphereCollider _collider;

        /// <summary>
        /// Capsule volume where the body is expected, should be a trigger
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.ExpectedBodyCapsule)]
        [SerializeField]
        protected CapsuleCollider _expectedBodyCapsule;

        /// <summary>
        /// Root of the character object, which has specially named body tracked bones as children somewhere in it's hierarchy
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.CharacterRoot)]
        [SerializeField]
        protected Transform _characterRoot;

        /// <summary>
        /// Collision layers to avoid merging foot collision area into (due to animation or body tracking)
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.FloorLayerMask)]
        [SerializeField]
        protected LayerMask _floorLayerMask = 1;

        /// <summary>
        /// Partial names (in the hierarchy) of Transforms that track the hips
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.TrackingHipsNames)]
        [SerializeField]
        protected string[] _trackingHipsNames = { "Hips" };

        /// <summary>
        /// Partial names (in the hierarchy) of Transforms that track the toes or bottom of feet
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.TrackingToesNames)]
        [SerializeField]
        protected string[] _trackingToesNames = { "_ToesEnd", "FootTip" };

        /// <summary>
        /// If true, the sphere collider will be influenced by the y-position of toes
        /// </summary>
        [Tooltip(SphereColliderStaysBelowHipsTooltips.ColliderFollowsToes)]
        [SerializeField]
        private bool _colliderFollowsToes = false;

        /// <summary>
        /// Last position of the target sphere collider
        /// </summary>
        private Vector3 _lastPosition;

        /// <summary>
        /// Transform that move when the player moves IRL.
        /// </summary>
        protected Transform _trackingHips;

        /// <summary>
        /// Transforms belonging to feet, at the ground, that move when the animated character moves.
        /// </summary>
        protected Transform[] _trackingToes;

        /// <summary>
        /// Statically allocated memory for collision detection, for performance reasons
        /// </summary>
        private RaycastHit[] _nonallocSphereCastHits = new RaycastHit[10];

        /// <inheritdoc cref="_characterRoot"/>
        public Transform CharacterRoot
        {
            get => _characterRoot;
            set
            {
                _characterRoot = value;
                RefreshBodytrackedRoot();
            }
        }

        /// <inheritdoc cref="_colliderFollowsToes"/>
        public bool TrackToes
        {
            get => _colliderFollowsToes;
            set => _colliderFollowsToes = value;
        }

        /// <summary>
        /// <inheritdoc cref="_trackingHips"/>
        /// Automatically set when CharacterRoot is set. Can be manually reset later.
        /// </summary>
        public Transform TrackedToDetermineGroundPosition
        {
            get => _trackingHips;
            set => _trackingHips = value;
        }

        /// <summary>
        /// Discovers an active transform named by <see cref="_trackingHipsNames"/>
        /// </summary>
        public void RefreshBodytrackedRoot()
        {
            Transform[] trackingHips = FindTransformsWithNamesContaining(CharacterRoot, _trackingHipsNames);
            _trackingHips = trackingHips[0];
            _trackingToes = FindTransformsWithNamesContaining(CharacterRoot, _trackingToesNames);
        }

        private static Transform[] FindTransformsWithNamesContaining(Transform root, IEnumerable<string> partialNames)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>();
            List<Transform> result = new List<Transform>();
            foreach (string partialName in partialNames)
            {
                for (int i = 0; i < transforms.Length; ++i)
                {
                    Transform t = transforms[i];
                    if (t.name.Contains(partialName))
                    {
                        result.Add(t);
                    }
                }
            }
            return result.ToArray();
        }

        protected Vector3 GetPlausibleFeetAbsolutePosition()
        {
            Vector3 plausibleFeetPosition = _trackingHips.position;
            if (!_colliderFollowsToes)
            {
                plausibleFeetPosition.y = transform.position.y;
            }
            else
            {
                plausibleFeetPosition.y = _trackingToes[0].position.y;
                GetBestYValueForToes(ref plausibleFeetPosition.y);
            }
            return plausibleFeetPosition;
        }

        private void GetBestYValueForToes(ref float toesY)
        {
            for (int i = 1; i < _trackingToes.Length; ++i)
            {
                Transform toe = _trackingToes[i];
                float toePositionY = toe.position.y;
                if (toePositionY >= toesY)
                {
                    toesY = toePositionY;
                }
            }
        }

        private void Start()
        {
            RefreshBodytrackedRoot();
        }

        private void Update()
        {
            Transform colliderTransform = _collider.transform;
            Vector3 feetAbsolutePosition = GetPlausibleFeetAbsolutePosition();
            Vector3 targetSpherePosition = feetAbsolutePosition + Vector3.up * _collider.radius;
            Vector3 targetSphereLocalPosition = colliderTransform.InverseTransformPoint(targetSpherePosition);
            Vector3 previousSphereLocalPosition = _collider.center;
            bool collisionSphereMovedUnexpectedly = previousSphereLocalPosition != targetSphereLocalPosition;
            if (collisionSphereMovedUnexpectedly)
            {
                // this will cause OnCollisionEnter for the _collider
                _collider.center = targetSphereLocalPosition;
                CapsuleColliderFollowsSphereCollider();
                MoveOutOfPenetratedFloor(colliderTransform, targetSpherePosition);
            }
            _lastPosition = targetSpherePosition;

            void CapsuleColliderFollowsSphereCollider()
            {
                if (_expectedBodyCapsule == null)
                {
                    return;
                }
                if (_expectedBodyCapsule.gameObject == _collider.gameObject)
                {
                    _expectedBodyCapsule.center = _collider.center + (Vector3.up * (_expectedBodyCapsule.height / 2 - _collider.radius));
                }
                else
                {
                    Vector3 targetCapsulePosition = feetAbsolutePosition + Vector3.up * (_expectedBodyCapsule.height / 2);
                    Vector3 targetCapsuleLocalPosition = _expectedBodyCapsule.transform.InverseTransformPoint(targetCapsulePosition);
                    _expectedBodyCapsule.center = targetCapsuleLocalPosition;
                }
            }
        }

        private void MoveOutOfPenetratedFloor(Transform colliderT, Vector3 targetSpherePosition)
        {
            Vector3 movedDelta = targetSpherePosition - _lastPosition;
            float targetDistance = movedDelta.magnitude;
            Vector3 direction = movedDelta / targetDistance;
            _collider.enabled = false;
            int hitCount = Physics.SphereCastNonAlloc(_lastPosition, _collider.radius, direction,
                _nonallocSphereCastHits, targetDistance, _floorLayerMask);
            _collider.enabled = true;
            int mostFloorLikeCollision = -1;
            float bestFloorLikeness = float.NegativeInfinity;
            float floorLikeness;
            const int DefaultLayerMask = 1;
            bool assumeLayerMaskAvoidsSelf = _floorLayerMask != DefaultLayerMask;
            for (int i = 0; i < hitCount; ++i)
            {
                RaycastHit hitInfo = _nonallocSphereCastHits[i];
                bool immediateContactCollision = hitInfo.distance == 0;
                if (immediateContactCollision || hitInfo.collider.isTrigger
                || (!assumeLayerMaskAvoidsSelf && DoesColliderBelongToController(hitInfo.collider)))
                {
                    continue;
                }
                else if ((floorLikeness = Vector3.Dot(Vector3.up, hitInfo.normal)) > bestFloorLikeness)
                {
                    mostFloorLikeCollision = i;
                    bestFloorLikeness = floorLikeness;
                }
            }
            if (mostFloorLikeCollision >= 0)
            {
                RaycastHit hitInfo = _nonallocSphereCastHits[mostFloorLikeCollision];
                Vector3 stablePositionAfterOffset = hitInfo.point + hitInfo.normal * _collider.radius;
                Vector3 currentTrackingSpaceCenter = colliderT.position;
                Vector3 offset = targetSpherePosition - currentTrackingSpaceCenter;
                Vector3 stablePosition = stablePositionAfterOffset - offset;
                colliderT.position = stablePosition;
            }
        }

        private bool DoesColliderBelongToController(Collider collider)
        {
            Transform selfRoot = transform;
            Transform t = collider.transform;
            while (t != null)
            {
                if (t == selfRoot || t == _characterRoot)
                {
                    return true;
                }
                t = t.parent;
            }
            return false;
        }
    }
}
