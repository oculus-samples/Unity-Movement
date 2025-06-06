// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

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
        protected SkinnedMeshRenderer[] _meshes;
        /// <inheritdoc cref="_meshes"/>
        public SkinnedMeshRenderer[] Meshes
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

        private class WeightContainer
        {
            public float[] Weights;
            public WeightContainer(float[] weights)
            {
                Weights = weights;
            }
        }

        private Mapper[] _mappers;
        private IRigLogic[] _rigs;
        private float[] _weights;
        private WeightContainer[] _drivers;
        private WeightContainer[] _outputSignals;

        private WeightContainer[] _meshToCachedValues;

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
        public bool Initialized =>
            _meshes != null
            && _meshes.Length > 0 &&
            (_rigs.Length == _meshes.Length);

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
            List<IRigLogic> rigList = new List<IRigLogic>();
            List<Mapper> mapperList = new List<Mapper>();
            List<WeightContainer> driverList = new List<WeightContainer>();
            List<WeightContainer> outputSignalList = new List<WeightContainer>();
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
                rigList.Add(rig);
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
                mapperList.Add(mapper);

                driverList.Add(new WeightContainer(Enumerable.Repeat(0.0f, drivers.Length).ToArray()));
                outputSignalList.Add(new WeightContainer(Enumerable.Repeat(0.0f, blendshapeNames.Count).ToArray()));
            }
            _rigs = rigList.ToArray();
            _mappers = mapperList.ToArray();
            _drivers = driverList.ToArray();
            _outputSignals = outputSignalList.ToArray();
            _weights = Enumerable.Repeat(0.0f, _weightsProvider.GetWeightNames().Length).ToArray();

            // Print all inputs that have not been found in any of the meshes (i.e., end up unused).
            // Please note that meshes can use different sets of inputs - e.g., the tongue shapes are only used by the
            // mouth mesh, while e.g. the eyebrow meshes only by the face mesh. Finding at least one mesh that uses a
            // signal means that signal is in use.
            var filteredMissedInputs = missedInputs.Where(i => i.Value == _meshes.Length).ToList();
            if (filteredMissedInputs.Count > 0)
            {
                Debug.LogWarning($"FaceDriver {name}: Some input signals are not driving any blendshapes: {string.Join(", ", filteredMissedInputs.Select((i) => i.Key))}");
            }

            // Print all blendshapes that have not been matched with an input.
            if (missedMeshBlendshapes.Count > 0)
            {
                Debug.LogWarning($"FaceDriver {name}: Blendshapes are not driven by any signals: {string.Join(", ", missedMeshBlendshapes)}");
            }

            _meshToCachedValues = new WeightContainer[_meshes.Length];
            for (int i = 0; i < _meshes.Length; i++)
            {
                int blendshapeCount = _meshes[i].sharedMesh.blendShapeCount;
                // Force some invalid value, so that on the first frame, our cache is seen as invalid.
                float[] allWeights = Enumerable.Repeat(-1.0f, blendshapeCount).ToArray();
                _meshToCachedValues[i] = new WeightContainer(allWeights);
            }
        }

        private void DriveAllMeshesWithRetargeting(float[] inputSignals)
        {
            if (!Initialized)
            {
                Debug.LogError("FaceDriver is not initialized properly.");
                return;
            }

            for (var i = 0; i < _meshes.Length; i++)
            {
                var mesh = _meshes[i];
                var mapper = _mappers[i];
                var rig = _rigs[i];
                var outputs = _outputSignals[i].Weights;

                mapper.Map(inputSignals, _drivers[i].Weights);
                rig.Eval(_drivers[i].Weights, outputs);

                var currentCachedWeights = _meshToCachedValues[i].Weights;
                for (var j = 0; j < outputs.Length; j++)
                {
                    var finalValue = outputs[j] * 100.0f;
                    // Avoid updating the skinned mesh renderer if the last cached weight
                    // indicates that an update is not required.
                    if (Math.Abs(finalValue - currentCachedWeights[j]) < 1e-6)
                    {
                        continue;
                    }
                    currentCachedWeights[j] = finalValue;
                    mesh.SetBlendShapeWeight(j, finalValue);
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
