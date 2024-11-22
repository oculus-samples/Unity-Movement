// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Oculus.Movement.FaceTrackingTooltips;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Implements a rig concept based on a naming convention, and drives the deformation.
    /// </summary>
    public class FaceDriver : MonoBehaviour
    {
        /// <summary>
        /// Meshes to animate.
        /// </summary>
        [SerializeField]
        [Tooltip(FaceDriverTooltips.Meshes)]
        protected List<SkinnedMeshRenderer> _meshes;
        /// <inheritdoc cref="_meshes"/>
        public List<SkinnedMeshRenderer> Meshes
        {
            get => _meshes;
            set => _meshes = value;
        }

        /// <summary>
        /// The weights provider that drives the deformation.
        /// </summary>
        [SerializeField]
        [Tooltip(FaceDriverTooltips.WeightsProvider)]
        protected WeightsProvider _weightsProvider;
        /// <inheritdoc cref="_weightsProvider"/>
        public WeightsProvider WeightsProvider
        {
            get => _weightsProvider;
            set => _weightsProvider = value;
        }

        /// <summary>
        /// Character's rig type.
        /// </summary>
        [SerializeField]
        [Tooltip(FaceDriverTooltips.RigType)]
        protected RigType _rigType = RigType.XRTech;
        /// <inheritdoc cref="_rigType"/>
        public RigType RigTypeValue
        {
            get => _rigType;
            set => _rigType = value;
        }

        private List<Mapper> _mappers = new List<Mapper>();
        private List<IRigLogic> _rigs = new List<IRigLogic>();

        private float[] _weights;
        private List<float[]> _drivers = new List<float[]>();
        private List<float[]> _outputSignals = new List<float[]>();

        /// <summary>
        /// Character's rig type.
        /// </summary>
        public enum RigType
        {
            Simple,
            XRTech
        }

        /// <summary>
        /// Indicates if this component is initialized or not.
        /// </summary>
        public bool Initialized => _meshes != null && _meshes.Count > 0 && _rigs.Count == _meshes.Count;

        private static IRigLogic MakeRig(RigType rt, List<string> names)
        {
            switch (rt)
            {
                case RigType.Simple:
                    return new SimpleRigLogic(names);
                case RigType.XRTech:
                    return new RigLogic(names);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rt), rt, null);
            }
        }

        private void Start()
        {
            // Setup weights provider
            Debug.Assert(_weightsProvider != null);
            Debug.Assert(_weightsProvider.GetWeightNames() != null);

            var missedInputs = new Dictionary<string, int>();
            var missedMeshBlendshapes = new SortedSet<string>();

            // Instantiate all rig logic instances
            foreach (var mesh in _meshes)
            {
                List<string> blendshapeNames = new List<string>();
                for (var i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
                {
                    var bsName = mesh.sharedMesh.GetBlendShapeName(i);
                    bsName = bsName.Substring(bsName.LastIndexOf(".", StringComparison.Ordinal) + 1);
                    blendshapeNames.Add(bsName);
                }

                var rig = MakeRig(_rigType, blendshapeNames);
                _rigs.Add(rig);
                var drivers = rig.Drivers;

                var mapper = new Mapper(_weightsProvider.GetWeightNames().ToList(), drivers, (inputs) =>
                {
                    foreach (var i in inputs)
                    {
                        if (!missedInputs.TryAdd(i, 1))
                        {
                            missedInputs[i] += 1;
                        }
                    }
                }, (drivers) =>
                {
                    foreach (var d in drivers)
                    {
                        missedMeshBlendshapes.Add($"{mesh.name}.{d}");
                    }
                });
                _mappers.Add(mapper);

                _drivers.Add(Enumerable.Repeat(0.0f, drivers.Count).ToArray());
                _outputSignals.Add(Enumerable.Repeat(0.0f, blendshapeNames.Count).ToArray());
            }

            _weights = Enumerable.Repeat(0.0f, _weightsProvider.GetWeightNames().Count).ToArray();

            // Print all inputs that have not been found in any of the meshes (i.e., end up unused).
            // Please note that meshes can use different sets of inputs - e.g., the tongue shapes are only used by the
            // mouth mesh, while e.g. the eyebrow meshes only by the face mesh. Finding at least one mesh that uses a
            // signal means that signal is in use.
            var filteredMissedInputs = missedInputs.Where(i => i.Value == _meshes.Count).ToList();
            if (filteredMissedInputs.Count > 0)
            {
                Debug.LogWarning($"FaceDriver {name}: Some input signals are not driving any blendshapes: {string.Join(", ", filteredMissedInputs.Select((i) => i.Key))}");
            }

            // Print all blendshapes that have not been matched with an input.
            if (missedMeshBlendshapes.Count > 0)
            {
                Debug.LogWarning($"FaceDriver {name}: Blendshapes are not driven by any signals: {string.Join(", ", missedMeshBlendshapes)}");
            }
        }

        private void DriveAllMeshesWithRetargeting(IReadOnlyList<float> inputSignals)
        {
            if (!Initialized)
            {
                Debug.LogError("FaceDriver is not initialized properly.");
                return;
            }

            for (var i = 0; i < _meshes.Count; i++)
            {
                var mesh = _meshes[i];
                var mapper = _mappers[i];
                var rig = _rigs[i];
                var outputs = _outputSignals[i];

                mapper.Map(inputSignals, _drivers[i]);
                rig.Eval(_drivers[i], outputs);
                for (var j = 0; j < outputs.Length; j++)
                {
                    mesh.SetBlendShapeWeight(j, outputs[j] * 100.0f);
                }
            }
        }

        private void Update()
        {
            if (_weightsProvider == null || !_weightsProvider.IsValid)
            {
                return;
            }

            WeightsProvider.CopyWeights(_weightsProvider.GetWeights(), ref _weights);
            DriveAllMeshesWithRetargeting(_weights);
        }
    }
}
