// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// <see cref="IOVRSkeletonProcessorAggregator"/> Component with Editor UI
    /// </summary>
    public class SkeletonProcessAggregator : MonoBehaviour,
        IOVRSkeletonProcessor, IOVRSkeletonProcessorAggregator
    {
        /// <summary>
        /// Abstraction layer for the Unity Editor to manipulate
        /// <see cref="IOVRSkeletonProcessor"/>s
        /// </summary>
        [System.Serializable]
        public class Item
        {
#if UNITY_EDITOR
            [Tooltip("Editor only label, for cosmetic purposes while editing")]
            [SerializeField] private string label;

            [Tooltip("The " + nameof(EnableSkeletonProcessing) + " state of this processor")]
            public bool Enabled;

            /// <summary>
            /// Used to check if this element is being modified
            /// </summary>
            private bool _lastKnownEnabled;

            /// <summary>
            /// Used to check if this element is being modified
            /// </summary>
            private UnityEngine.Object _lastKownProcessor;
#endif
            [Interface(typeof(IOVRSkeletonProcessor))]
            [Tooltip("The "+nameof(IOVRSkeletonProcessor))]
            public UnityEngine.Object Processor;

            private IOVRSkeletonProcessor _iProcessor;

            public IOVRSkeletonProcessor IProcessor
            {
                get
                {
                    return Processor == null
                        ? null : (_iProcessor != null)
                        ? _iProcessor : _iProcessor = Processor as IOVRSkeletonProcessor;
                }
            }
#if UNITY_EDITOR
            public bool OnValidateUpdate()
            {
                if (_lastKownProcessor != Processor)
                {
                    _lastKownProcessor = Processor;
                    _iProcessor = null;
                    if (Processor != null && IProcessor != null)
                    {
                        _lastKnownEnabled = IProcessor.EnableSkeletonProcessing;
                        label = IProcessor.SkeletonProcessorLabel;
                    }
                    else
                    {
                        _lastKnownEnabled = false;
                    }
                    Enabled = _lastKnownEnabled;
                    return true;
                }
                else if (_lastKnownEnabled != Enabled)
                {
                    if (IProcessor != null)
                    {
                        IProcessor.EnableSkeletonProcessing = _lastKnownEnabled = Enabled;
                    }
                    else
                    {
                        _lastKnownEnabled = Enabled = false;
                    }
                    return true;
                }
                else if (Processor != null && IProcessor != null && _lastKnownEnabled != IProcessor.EnableSkeletonProcessing)
                {
                    _lastKnownEnabled = Enabled = IProcessor.EnableSkeletonProcessing;
                    return true;
                }
                else if (string.IsNullOrEmpty(label) && IProcessor != null)
                {
                    label = IProcessor.SkeletonProcessorLabel;
                    return true;
                }
                return false;
            }
#endif
            public Item(IOVRSkeletonProcessor processor)
            {
                _iProcessor = processor;
                Processor = processor as UnityEngine.Object;
#if UNITY_EDITOR
                label = IProcessor.SkeletonProcessorLabel;
                Enabled = _iProcessor.EnableSkeletonProcessing;
#endif
            }
        }

        /// <summary>
        /// The <see cref="IOVRSkeletonProcessorAggregator"/> to give self to
        /// </summary>
        [SerializeField]
        [ContextMenuItem(nameof(FindLocalProcessAggregator), nameof(FindLocalProcessAggregator))]
        [Interface(typeof(IOVRSkeletonProcessorAggregator))]
        protected UnityEngine.Object _autoAddTo;

#if UNITY_EDITOR
        [ContextMenuItem(nameof(AddEveryOVRSkeletonProcessor), nameof(AddEveryOVRSkeletonProcessor))]
#endif
        [Tooltip("Unity Editor references to the Ordered list of " +
            nameof(IOVRSkeletonProcessor) + " components that will modify the " +
            "skeleton after it is initially read from " + nameof(OVRPlugin))]
        [SerializeField]
        private List<Item> _skeletonProcessors = new List<Item>();

        public string SkeletonProcessorLabel => SkeletonProcessorBehaviour.DefaultLabel(this);

        public List<Item> SkeletonProcessors
        {
            get => _skeletonProcessors;
            set => _skeletonProcessors = value;
        }
        public bool EnableSkeletonProcessing { get => enabled; set => enabled = value; }

#if UNITY_EDITOR
        private void Reset()
        {
            FindLocalProcessAggregator();
            IOVRSkeletonProcessor[] processors = GetComponents<IOVRSkeletonProcessor>();
            IOVRSkeletonProcessor self = this as IOVRSkeletonProcessor;
            Array.ForEach(processors, p =>
            {
                if (p == self)
                {
                    return;
                }
                _skeletonProcessors.Add(new Item(p));
            });
        }

        private void OnValidate()
        {
            EditTimeItemValidateCheck();
        }

        public void EditTimeItemValidateCheck()
        {
            for (int i = 0; i < _skeletonProcessors.Count; ++i)
            {
                if (_skeletonProcessors[i].OnValidateUpdate())
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

        private void AddEveryOVRSkeletonProcessor()
        {
            UnityEngine.Object[] processors = FindObjectsOfType<UnityEngine.Object>(true);
            Array.ForEach(processors, o =>
            {
                if (o is IOVRSkeletonProcessor p)
                {
                    _skeletonProcessors.Add(new Item(p));
                }
            });
        }
#endif

        private void FindLocalProcessAggregator()
        {
            IOVRSkeletonProcessorAggregator[] components =
                GetComponentsInParent<IOVRSkeletonProcessorAggregator>();
            IOVRSkeletonProcessorAggregator self = this;
            _autoAddTo = Array.Find(components, c => c != self)
                as UnityEngine.Object;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// any Start method allows the script to be turned enabled/disabled in the unity editor
        /// </summary>
        private void Start()
        {
            if (_autoAddTo != null)
            {
                IOVRSkeletonProcessorAggregator aggregator = _autoAddTo
                    as IOVRSkeletonProcessorAggregator;
                aggregator.AddProcessor(this);
            }
        }

        public virtual void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (!enabled)
            {
                return;
            }
            for (int i = 0; i < _skeletonProcessors.Count; ++i)
            {
                IOVRSkeletonProcessor processor = _skeletonProcessors[i].IProcessor;
                if (processor == null)
                {
                    Debug.LogError($"Unable to process {name} processor {i}");
                    continue;
                }
                if (!processor.EnableSkeletonProcessing)
                {
                    continue;
                }
                processor.ProcessSkeleton(skeleton);
            }
        }

        /// <summary>
        /// Adds a <see cref="IOVRSkeletonProcessor"/> to the list
        /// </summary>
        public void AddProcessor(IOVRSkeletonProcessor processor)
        {
            _skeletonProcessors.Add(new Item(processor));
        }

        /// <summary>
        /// Removes the given <see cref="IOVRSkeletonProcessor"/> from the list
        /// </summary>
        public void RemoveProcessor(IOVRSkeletonProcessor processor)
        {
            int index = _skeletonProcessors.FindIndex(item => item.IProcessor == processor);
            if (index >= 0)
            {
                _skeletonProcessors.RemoveAt(index);
            }
        }

        /// <summary>
        /// Removes the given <see cref="IOVRSkeletonProcessor"/> from the list, if it is valid
        /// </summary>
        public void RemoveProcessor(Component processorComponent)
        {
            IOVRSkeletonProcessor processor = processorComponent as IOVRSkeletonProcessor;
            if (processor != null)
            {
                RemoveProcessor(processor);
            }
        }

        /// <summary>
        /// Removes the <see cref="IOVRSkeletonProcessor"/> in this Transform from the list
        /// </summary>
        public void RemoveProcessorsInTransform(Transform processorObject)
        {
            IOVRSkeletonProcessor[] processors = processorObject.GetComponents<IOVRSkeletonProcessor>();
            Array.ForEach(processors, p => RemoveProcessor(p));
        }
    }
}
