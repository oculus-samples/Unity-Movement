// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Used to change hierarchy at runtime.
    /// Allows arrangement in a more intuitive manner during edit time.
    /// </summary>
    public class ReorderInHierarchy : MonoBehaviour
    {
        public enum HowToReorder
        {
            /// <summary>
            /// Do not touch the hierarchy
            /// </summary>
            None,
            /// <summary>
            /// Move this to be a child of the given target
            /// </summary>
            BecomeChildOf,
            /// <summary>
            /// Move the given target to be a child of this
            /// </summary>
            BecomeParentOf,
            /// <summary>
            /// Move this to be a child of the given target's parent, after the given target
            /// </summary>
            BecomeSiblingAfter,
            /// <summary>
            /// Move this to be a child of the given target's parent, before the given target
            /// </summary>
            BecomeSiblingBefore,
            /// <summary>
            /// Move this to be a child of the given target, and set position/rotation to zero
            /// </summary>
            BecomeChildOfAndLoseOldPosition,
            /// <summary>
            /// Move the given target to be a child of this, set target's position/rotation to zero
            /// </summary>
            BecomeParentOfAndClearOldPosition,
        }

        /// <summary>
        /// How to reorder this transform in the hierarchy
        /// </summary>
        [Tooltip(ReorderInHierarchyTooltips.HowToReorder)]
        [SerializeField]
        private HowToReorder _howToReorder;

        /// <summary>
        /// Which object to reorder in relation to
        /// </summary>
        [Tooltip(ReorderInHierarchyTooltips.Target)]
        [SerializeField]
        private Transform _target;

        /// <summary>
        /// Which object to reorder in relation to
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <inheritdoc/>
        private void Start()
        {
            DoActivateTrigger();
        }

        /// <summary>
        /// Do the reordering defined by this component
        /// </summary>
        public void DoActivateTrigger()
        {
            switch (_howToReorder)
            {
                case HowToReorder.BecomeChildOf:
                    BecomeChildOf(transform, _target);
                    break;
                case HowToReorder.BecomeParentOf:
                    BecomeParentOf(transform, _target);
                    break;
                case HowToReorder.BecomeSiblingAfter:
                    BecomeSiblingAfter(transform, _target);
                    break;
                case HowToReorder.BecomeSiblingBefore:
                    transform.parent = _target.parent;
                    transform.SetSiblingIndex(_target.GetSiblingIndex());
                    break;
                case HowToReorder.BecomeChildOfAndLoseOldPosition:
                    BecomeChildOfLoseOldPosition(transform, _target);
                    break;
                case HowToReorder.BecomeParentOfAndClearOldPosition:
                    BecomeParentOfLoseOldPosition(transform, _target);
                    break;
            }
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeChildOf"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        public static void BecomeChildOf(Transform transform, Transform newParent)
        {
            transform.SetParent(newParent);
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeParentOf"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newChild"></param>
        public static void BecomeParentOf(Transform transform, Transform newChild)
        {
            newChild.SetParent(transform);
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeChildOfAndLoseOldPosition"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newParent"></param>
        public static void BecomeChildOfLoseOldPosition(Transform transform, Transform newParent)
        {
            transform.SetParent(newParent, false);
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeParentOfAndClearOldPosition"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newChild"></param>
        public static void BecomeParentOfLoseOldPosition(Transform transform, Transform newChild)
        {
            newChild.SetParent(transform, false);
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeSiblingAfter"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="sibling"></param>
        public static void BecomeSiblingAfter(Transform transform, Transform sibling)
        {
            BecomeChildOf(transform, sibling.parent);
            transform.SetSiblingIndex(sibling.GetSiblingIndex() + 1);
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeSiblingBefore"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="sibling"></param>
        public static void BecomeSiblingBefore(Transform transform, Transform sibling)
        {
            BecomeChildOf(transform, sibling.parent);
            transform.SetSiblingIndex(sibling.GetSiblingIndex());
        }

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeChildOf"/>
        /// </summary>
        /// <param name="newParent"></param>
        public void BecomeChildOf(Transform newParent) => BecomeChildOf(transform, newParent);

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeParentOf"/>
        /// </summary>
        /// <param name="newChild"></param>
        public void BecomeParentOf(Transform newChild) => BecomeParentOf(transform, newChild);

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeChildOfAndLoseOldPosition"/>
        /// </summary>
        /// <param name="newParent"></param>
        public void BecomeChildOfLoseOldPosition(Transform newParent) =>
            BecomeChildOfLoseOldPosition(transform, newParent);

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeSiblingAfter"/>
        /// </summary>
        /// <param name="sibling"></param>
        public void BecomeSiblingAfter(Transform sibling) =>
            BecomeSiblingAfter(transform, sibling);

        /// <summary>
        /// <inheritdoc cref="HowToReorder.BecomeSiblingBefore"/>
        /// </summary>
        /// <param name="sibling"></param>
        public void BecomeSiblingBefore(Transform sibling) =>
            BecomeSiblingBefore(transform, sibling);
    }
}
