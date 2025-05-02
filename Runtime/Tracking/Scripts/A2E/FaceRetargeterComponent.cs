// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Oculus.Movement.FaceTrackingTooltips;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Inherits from <see cref="WeightsProvider"/> to map source tracking weights
    /// to a set of traget weights based on a JSON configuration file.
    /// </summary>
    public class FaceRetargeterComponent : WeightsProvider
    {
        /// <summary>
        /// Retargeter config JSON.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargeterComponentTooltips.RetargeterConfig)]
        protected TextAsset _retargeterConfig;
        /// <inheritdoc cref="_retargeterConfig"/>
        public TextAsset RetargeterConfig
        {
            get => _retargeterConfig;
            set => _retargeterConfig = value;
        }

        /// <summary>
        /// Override config filename loaded from application's persistent path.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargeterComponentTooltips.RetargeterConfigOverride)]
        protected string _retargeterConfigOverride;
        /// <inheritdoc cref="_retargeterConfigOverride"/>
        public string RetargeterConfigOverride
        {
            get => _retargeterConfigOverride;
            set => _retargeterConfigOverride = value;
        }

        /// <summary>
        /// The source weights provide to provide to the input mapper.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargeterComponentTooltips.WeightsProvider)]
        protected WeightsProvider _weightsProvider;
        /// <inheritdoc cref="_weightsProvider"/>
        public WeightsProvider WeightsProvider
        {
            get => _weightsProvider;
            set => _weightsProvider = value;
        }

        private Mapper _inputMapper;
        private Retargeter _retargeter;

        private float[] _input;
        private float[] _output;
        private string[] _inputSignals;
        private string[] _outputSignals;

        // <inheritdoc />
        public override bool IsValid => _retargeter != null;

        private void Awake()
        {
            EnsureInitialized();
        }

        // <inheritdoc />
        public override string[] GetWeightNames()
        {
            EnsureInitialized();

            return _outputSignals ??= _retargeter.OutputSignals.ToArray();
        }

        // <inheritdoc />
        public override float[] GetWeights()
        {
            EnsureInitialized();

            _inputMapper.Map(_weightsProvider.GetWeights(), _input);
            _retargeter.Eval(_input, _output);

            return _output;
        }

        /// <summary>
        /// Retrieves input signals.
        /// </summary>
        /// <returns>Input signal names.</returns>
        public IReadOnlyList<string> GetInputNames()
        {
            EnsureInitialized();

            return _inputSignals ??= _retargeter.InputSignals.ToArray();
        }

        private void EnsureInitialized()
        {
            if (_retargeter != null) return;

            // Setup weights provider
            Debug.Assert(_weightsProvider != null);

            var retargeterConfigContent = "";

            // Load config from override preferentially
            var overridePath = Path.Join(Application.persistentDataPath, _retargeterConfigOverride);
            if (File.Exists(overridePath) && _retargeterConfigOverride.Length > 0)
            {
                Debug.Log($"Loading retargeter config from override: {overridePath}");
                retargeterConfigContent = File.ReadAllText(overridePath);
            }

            // Otherwise use the text asset hardwired in the app (which can be null if we decide we want to rely on the
            // override asset only to guarantee a valid config, like in UXR studies)
            else if (_retargeterConfig != null)
            {
                Debug.Log($"Loading retargeter config from text asset: {_retargeterConfig.name}");
                retargeterConfigContent = _retargeterConfig.text;
            }

            if (retargeterConfigContent.Length == 0)
            {
                Debug.LogError($"A valid retargeter configuration not found! (config={_retargeterConfig.name}, override={_retargeterConfigOverride})");
                return;
            }

            // And instantiate the retargeter
            _retargeter = new Retargeter(retargeterConfigContent);

            _inputMapper = new Mapper(_weightsProvider.GetWeightNames().ToList(), _retargeter.InputSignals, (
                missedInputs =>
                {
                    Debug.LogWarning($"RetargeterComponent {name}: Input signals {string.Join(", ", missedInputs)} are not set up in the retargeter config, and will not be used!");
                }),
                missedDrivers =>
                {
                    Debug.LogWarning($"RetargeterComponent {name}: Output signals {string.Join(", ", missedDrivers)} are set up in the retargeter config but have no driving signals, and will not be used!");
                }
            );

            _input = Enumerable.Repeat(0.0f, _retargeter.InputSignals.Length).ToArray();
            _output = Enumerable.Repeat(0.0f, _retargeter.OutputSignals.Length).ToArray();
        }
    }
}
