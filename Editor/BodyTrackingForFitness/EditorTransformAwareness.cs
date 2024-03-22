// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// This class attaches itself to the Unity Editor at editor time to enable
    /// transform-dependent logic. It watches the editor's Selection, and if a
    /// transform that this class cares about is modified, it notifies a
    /// function associated with that transform's owner object. This solves the
    /// problem of how to propagate transform changes at editor time.
    /// </summary>
    public class EditorTransformAwareness
    {
        /// <summary>
        /// What is a large enough difference to trigger non-match by <see cref="IsMatch"/>
        /// </summary>
        public const float Epsilon = 1f / 1024;

        /// <summary>
        /// Private singleton so some editor time functionality can be static.
        /// </summary>
        private static EditorTransformAwareness _instance;
        
        /// <summary>
        /// Set of currently selected tracked transforms
        /// </summary>
        private HashSet<Transform> _activeTransforms = new HashSet<Transform>();

        /// <summary>
        /// Cached owners of active transforms
        /// </summary>
        private Dictionary<Transform, IEnumerable<Object>> _ownersOfActiveTransform =
            new Dictionary<Transform, IEnumerable<Object>>();

        /// <summary>
        /// Last known position of tracked transforms
        /// </summary>
        private Dictionary<Object, Dictionary<Transform, Pose>> _lastKnownPose =
            new Dictionary<Object, Dictionary<Transform, Pose>>();

        /// <summary>
        /// Objects that own lists of transforms (eg: bone managers).
        /// Allows clusters of transforms to use the same callback.
        /// </summary>
        private Dictionary<Object, Func<Transform,bool>> _transformsFromOwner =
            new Dictionary<Object, Func<Transform,bool>>();

        /// <summary>
        /// Callback methods to notify when a tracked transform has moved
        /// </summary>
        private Dictionary<Object, Action<Transform>> _transformOwnerListener =
            new Dictionary<Object, Action<Transform>>();

        /// <summary>
        /// Intermediary list, because activeTransforms could change while iterating callbacks
        /// </summary>
        private List<Transform> _toNotify = new List<Transform>();

        /// <code>[InitializeOnLoadMethod]</code> will trigger the static constructor
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor() { }

        /// <summary>
        /// static constructor called Once during editor runtime. Adds a
        /// listener that looks for special transforms that are being
        /// explicitly watched by <see cref="SetTransformGroupListener"/>
        /// </summary>
        static EditorTransformAwareness()
        {
            RefreshSystem();
        }

        /// <summary>
        /// Re-initializes the <see cref="EditorTransformAwareness"/> system
        /// </summary>
        public static void RefreshSystem()
        {
            _instance = new EditorTransformAwareness();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.delayCall += _instance.AddActiveTransformsToActiveListener;
        }

        private static void OnUpdate() => _instance.Update();
        private static void OnSelectionChanged() => _instance.AddActiveTransformsToActiveListener();

        private void Update()
        {
            if (!IsActive())
            {
                return;
            }
            UpdateActiveTransforms();
        }

        private bool IsActive() => UnityEditorInternal.InternalEditorUtility.isApplicationActive &&
                                   _activeTransforms.Count > 0;

        /// <summary>
        /// Identifies that some group of transforms managed by an owner should
        /// be passed into a specific callback whenever the transform changes
        /// position or rotation while selected in the Unity editor.
        /// </summary>
        /// <param name="owner">key object referencing the transforms</param>
        /// <param name="isTransformOwned">function identifies transform group</param>
        /// <param name="notifyMovedCallback">callback for members of this group</param>
        public static void SetBoneListener(Object owner, Func<Transform, bool> isTransformOwned,
            Action<Transform> notifyMovedCallback) =>
            _instance.SetTransformGroupListener(owner, isTransformOwned, notifyMovedCallback);

        /// <summary>
        /// Removes the special logic for the group of transforms managed by the given key
        /// </summary>
        /// <param name="owner"></param>
        public static void Remove(Object owner) => _instance.ClearTransformGroup(owner);

        /// <summary>
        /// Call this if a key object may have been deleted
        /// </summary>
        public static void RefreshCallbacks() => _instance.RemoveDeletedOwners();

        /// <summary>
        /// Check if the given object is a key owner of logic related to a group of transforms
        /// </summary>
        public static bool IsWatching(Object owner) =>
            _instance._transformsFromOwner.ContainsKey(owner);

        /// <summary>
        /// Check if the given transform is a managed transform, and it is selected.
        /// </summary>
        public static bool IsSelected(Transform transform) =>
            _instance._activeTransforms.Contains(transform);

        /// <summary>
        /// Returns the list of key objects that manage the given transform
        /// </summary>
        public static IEnumerable<Object> GetOwners(Transform transform) =>
            _instance.GetOwnersList(transform);

        /// <summary>
        /// Adds a group of transforms to monitor, bound to a specific key object
        /// </summary>
        /// <param name="owner">the key object</param>
        /// <param name="isTransformOwned">method to get if transform is associated with key</param>
        /// <param name="notifyMovedCallback">callback to notify when transforms move</param>
        private void SetTransformGroupListener(Object owner, Func<Transform, bool> isTransformOwned,
            Action<Transform> notifyMovedCallback)
        {
            _transformsFromOwner[owner] = isTransformOwned;
            _lastKnownPose[owner] = new Dictionary<Transform, Pose>();
            if (notifyMovedCallback != null)
            {
                _transformOwnerListener[owner] = notifyMovedCallback;
            }
        }

        /// <summary>
        /// Stops listening to transforms keyed to the given owner object
        /// </summary>
        private void ClearTransformGroup(Object owner)
        {
            _transformsFromOwner.Remove(owner);
            _lastKnownPose.Remove(owner);
            _transformOwnerListener.Remove(owner);
        }

        private void RemoveDeletedOwners()
        {
            bool ownerWasDeleted = false;
            foreach (var kvp in _transformsFromOwner)
            {
                if (kvp.Key == null)
                {
                    ownerWasDeleted = true;
                    break;
                }
            }
            if (ownerWasDeleted)
            {
                _transformsFromOwner = SameDictionaryWithoutNullKey(_transformsFromOwner);
                _lastKnownPose = SameDictionaryWithoutNullKey(_lastKnownPose);
                _transformOwnerListener = SameDictionaryWithoutNullKey(_transformOwnerListener);
            }
        }

        /// <summary>
        /// Because Unity can create dictionaries with objects that can be
        /// deleted, a function should be able to clean them up.
        /// </summary>
        public static Dictionary<K, V> SameDictionaryWithoutNullKey<K, V>(Dictionary<K, V> original)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            foreach (var kvp in original)
            {
                if (kvp.Key == null)
                {
                    continue;
                }
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private void AddActiveTransformsToActiveListener()
        {
            Transform[] selectedTransforms = Selection.GetTransforms(SelectionMode.Editable);
            ClearActiveTransforms();
            for (int i = 0; i < selectedTransforms.Length; ++i)
            {
                AddActiveTransformSelection(selectedTransforms[i]);
            }
        }

        private void ClearActiveTransforms()
        {
            _activeTransforms.Clear();
            _ownersOfActiveTransform.Clear();
        }

        private void AddActiveTransformSelection(Transform selectedTransform)
        {
            IEnumerable<Object> owners = GetKnownOwnersOfTransform(selectedTransform, true);
            if (owners == null)
            {
                return;
            }
            _activeTransforms.Add(selectedTransform);
            _ownersOfActiveTransform[selectedTransform] = owners;
        }
        
        private IEnumerable<Object> GetKnownOwnersOfTransform(Transform transform, bool getAll)
        {
            List<Object> owners = null;
            foreach (var kvp in _transformsFromOwner)
            {
                if (kvp.Value == null)
                {
                    continue;
                }
                Func<Transform,bool> isTransformOwned = kvp.Value;
                if (isTransformOwned == null)
                {
                    continue;
                }
                if (isTransformOwned.Invoke(transform))
                {
                    if (owners == null)
                    {
                        owners = new List<Object>();
                    }
                    owners.Add(kvp.Key);
                    if (getAll == false)
                    {
                        return owners;
                    }
                }
            }
            return owners;
        }

        private void UpdateActiveTransforms()
        {
            _toNotify.Clear();
            foreach (Transform transform in _activeTransforms)
            {
                if (!TryGetLastKnownPose(transform, out Pose pose) || !IsMatch(transform, pose))
                {
                    _toNotify.Add(transform);
                }
            }
            if (_toNotify.Count == 0)
            {
                return;
            }
            for (int i = 0; i < _toNotify.Count; ++i)
            {
                Transform transform = _toNotify[i];
                NotifyTransformChange(transform);
            }
        }

        private bool TryGetLastKnownPose(Transform transform, out Pose pose)
        {
            IEnumerable<Object> owners = GetOwnersList(transform);
            foreach (Object owner in owners)
            {
                if (_lastKnownPose[owner].TryGetValue(transform, out pose))
                {
                    return true;
                }
            }
            pose = default;
            return false;
        }

        private IEnumerable<Object> GetOwnersList(Transform transform)
        {
            if (!_ownersOfActiveTransform.TryGetValue(transform,
                    out IEnumerable<Object> owners))
            {
                owners = GetKnownOwnersOfTransform(transform, true);
                if (owners != null)
                {
                    _ownersOfActiveTransform[transform] = owners;
                }
            }
            return owners;
        }

        private void NotifyTransformChange(Transform activeTransform)
        {
            IEnumerable<Object> owners = GetOwnersList(activeTransform);
            if (owners != null)
            {
                foreach (Object owner in owners)
                {
                    if (_transformOwnerListener.TryGetValue(owner, out Action<Transform> callback))
                    {
                        callback.Invoke(activeTransform);
                    }
                    _lastKnownPose[owner][activeTransform] = ConvertToPose(activeTransform);
                }
            }
        }

        /// <summary>
        /// Converts a <see cref="Transform"/>'s postion/rotation to a <see cref="Pose"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static Pose ConvertToPose(Transform transform)
        {
            return new Pose(transform.position, transform.rotation);
        }
        
        /// <summary>
        /// Determines if <see cref="Transform"/> is reasonably close to a <see cref="Pose"/>
        /// </summary>
        public static bool IsMatch(Transform transform, Pose pose)
        {
            return IsMatch(transform.position, pose.position) &&
                   IsMatch(transform.rotation, pose.rotation);
        }

        /// <summary>
        /// Determines if vectors are reasonably close (within <see cref="Epsilon"/> in a dimension)
        /// </summary>
        public static bool IsMatch(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) <= Epsilon &&
                   Mathf.Abs(a.y - b.y) <= Epsilon &&
                   Mathf.Abs(a.z - b.z) <= Epsilon;
        }
        
        /// <summary>
        /// Determines if vectors are reasonably close (within <see cref="Epsilon"/> in a dimension)
        /// </summary>
        public static bool IsMatch(Quaternion a, Quaternion b)
        {
            return Mathf.Abs(a.x - b.x) <= Epsilon &&
                   Mathf.Abs(a.y - b.y) <= Epsilon &&
                   Mathf.Abs(a.z - b.z) <= Epsilon &&
                   Mathf.Abs(a.w - b.w) <= Epsilon;
        }
    }
}
