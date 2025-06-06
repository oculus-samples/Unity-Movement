// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Maintains input and output signals; has <see cref="Eval"/>
    /// which allows producing output signals from supplied inputs.
    /// </summary>
    public class Retargeter
    {
        /// <summary>
        /// List of input signals.
        /// </summary>
        public string[] InputSignals { get; private set; }
        /// <summary>
        /// List of output signals.
        /// </summary>
        public string[] OutputSignals { get; private set; }

        private const float Eps = 1e-4f;

        private struct Item
        {
            /// <summary>
            /// Index field.
            /// </summary>
            public int Index;
            /// <summary>
            /// Weight field.
            /// </summary>
            public float Weight;

            /// <summary>
            /// Evaluate provided signal using weight.
            /// </summary>
            /// <param name="signal">Signal value.</param>
            /// <returns>Signal as a fraction of weight or (1 - signal)/(1 - weight).</returns>
            public float Eval(float signal)
            {
                if (Mathf.Abs(signal - Weight) < Eps) return 1.0f;
                return signal <= Weight ? signal / Weight : (1f - signal) / (1f - Weight);
            }
        }

        private struct Rule
        {
            /// <summary>
            /// List of drivers.
            /// </summary>
            public Item[] Drivers;
            /// <summary>
            /// List of targets.
            /// </summary>
            public Item[] Targets;

            /// <summary>
            /// Obtains signal and target weights.
            /// </summary>
            /// <param name="signals">Signals list.</param>
            /// <param name="targets">Targets list.</param>
            public void Peak(float[] signals, float[] targets)
            {
                foreach (var dr in Drivers)
                {
                    signals[dr.Index] = dr.Weight;
                }

                foreach (var t in Targets)
                {
                    targets[t.Index] = t.Weight;
                }

            }

            /// <summary>
            /// Returns computed weight from (<see cref="IReadOnlyList"/>) signals.
            /// </summary>
            /// <param name="signals">Signals list.</param>
            /// <returns>Computed weight.</returns>
            public float Eval(float[] signals)
            {
                var weight = 1.0f;
                foreach (var d in Drivers)
                {
                    weight *= d.Eval(signals[d.Index]) * d.Weight;
                }

                return weight;
            }

            /// <summary>
            /// Returns computed weight from (<see cref="IList"/>) signals.
            /// </summary>
            /// <param name="signals">Signals list.</param>
            /// <returns>Computed weight.</returns>
            public float Eval(IList<float> signals)
            {
                var weight = 1.0f;
                foreach (var d in Drivers)
                {
                    weight *= d.Eval(signals[d.Index]) * d.Weight;
                }

                return weight;
            }
        }

        private Rule[] _rules;
        private Matrix _deltas;
        private float[] _activations;

        private struct LoadedWeights
        {
            public List<Item> Items;
        }

        private static LoadedWeights LoadWeights(Dictionary<string, float> d, ref Dictionary<string, int> indices)
        {
            var items = new List<Item>();

            foreach (var t in d)
            {
                var index = indices.Count;
                if (indices.TryGetValue(t.Key, out var i))
                {
                    index = i;
                }
                else
                {
                    indices[t.Key] = index;
                }

                items.Add(new Item() { Index = index, Weight = t.Value });
            }

            return new LoadedWeights()
            {
                Items = items,
            };
        }

        private static List<Rule> LoadV1(string json, ref Dictionary<string, int> signals, ref Dictionary<string, int> rigDrivers)
        {
            var mapping = JSONRigParser.DeserializeV1Mapping(json);

            var rules = new List<Rule>();

            foreach (var s in mapping)
            {
                var signalIndex = signals.Count;
                if (signals.TryGetValue(s.Key, out var i))
                {
                    signalIndex = i;
                }
                else
                {
                    signals[s.Key] = signalIndex;
                }

                var targets = LoadWeights(s.Value, ref rigDrivers);
                if (targets.Items.Count == 0)
                {
                    continue;
                }

                var drivers = Enumerable.Repeat(new Item { Index = signalIndex, Weight = 1f }, 1).ToList();
                rules.Add(new Rule() { Drivers = drivers.ToArray(), Targets = targets.Items.ToArray() });
            }

            return rules;
        }

        private static List<Rule> LoadV2(string json, ref Dictionary<string, int> signals, ref Dictionary<string, int> rigDrivers)
        {
            var mapping = JSONRigParser.DeserializeV2Mapping(json);

            var rules = new List<Rule>();

            foreach (var dt in mapping)
            {
                var drivers = LoadWeights(dt["drivers"], ref signals);
                if (drivers.Items.Count == 0)
                {
                    continue;
                }

                var targets = LoadWeights(dt["targets"], ref rigDrivers);

                if (targets.Items.Count == 0)
                {
                    continue;
                }

                rules.Add(new Rule() { Drivers = drivers.Items.ToArray(), Targets = targets.Items.ToArray() });
            }

            return rules;
        }

        private static string PrintMatrix(Matrix m)
        {
            var sb = new StringBuilder();
            for (var r = 0; r < m.Rows; ++r)
            {
                for (var c = 0; c < m.Cols; ++c)
                {
                    sb.Append(m[r, c].ToString("0.###"));
                    sb.Append(" ");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string PrintVector(List<float> m)
        {
            var sb = new StringBuilder();
            foreach (var a in m)
            {
                sb.Append($"{a} ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Main retargeter constructor.
        /// </summary>
        /// <param name="json">JSON configuration.</param>
        /// <param name="useSparseDeltaMatrix">Whether to use sparse delta matrix or not.</param>
        public Retargeter(string json, bool useSparseDeltaMatrix = true)
        {
            var signals = new Dictionary<string, int>();
            var rigDrivers = new Dictionary<string, int>();

            // V1 setups root element is a dict, V2 setups root element is an array
            var rulesList = json[0] == '{' ? LoadV1(json, ref signals, ref rigDrivers) : LoadV2(json, ref signals, ref rigDrivers);

            var peaks = new DenseMatrix(rulesList.Count, signals.Count);
            var targets = new DenseMatrix(rulesList.Count, rigDrivers.Count);
            for (var i = 0; i < rulesList.Count; ++i)
            {
                rulesList[i].Peak(peaks.Row(i), targets.Row(i));
            }

            var m = new DenseMatrix(rulesList.Count, rulesList.Count);
            for (var r = 0; r < m.Rows; ++r)
            {
                var rule = rulesList[r];
                for (var c = 0; c < m.Cols; ++c)
                {
                    m[r, c] = rule.Eval(peaks.Row(c));
                }
            }
            _rules = rulesList.ToArray();

            m.Invert();
            m.Transpose();
            _deltas = useSparseDeltaMatrix ? new SparseMatrix(DenseMatrix.Mult(m, targets)) : DenseMatrix.Mult(m, targets);

            {
                var inputSignals = Enumerable.Repeat("", signals.Count()).ToList();
                foreach (var s in signals)
                {
                    inputSignals[s.Value] = s.Key;
                }

                Debug.Assert(!inputSignals.Contains(""));
                InputSignals = inputSignals.ToArray();
            }

            {
                var outputSignals = Enumerable.Repeat("", rigDrivers.Count()).ToList();
                foreach (var s in rigDrivers)
                {
                    outputSignals[s.Value] = s.Key;
                }

                Debug.Assert(!outputSignals.Contains(""));
                OutputSignals = outputSignals.ToArray();
            }

            _activations = Enumerable.Repeat(0.0f, _rules.Length).ToArray();
        }

        /// <summary>
        /// Runs eval on input and output signals.
        /// </summary>
        /// <param name="signals">Input signals.</param>
        /// <param name="outputs">Output signals.</param>
        /// <exception cref="ArgumentException">Exception thrown if a problem is encountered.</exception>
        public void Eval(float[] signals, float[] outputs)
        {
            if (signals.Length != InputSignals.Length)
            {
                throw new ArgumentException($"Expected {InputSignals.Length} input signals, got {signals.Length}");
            }

            if (outputs.Length != OutputSignals.Length)
            {
                throw new ArgumentException($"Expected {OutputSignals.Length} output signals, got {outputs.Length}");
            }

            for (var i = 0; i < _rules.Length; ++i)
            {
                _activations[i] = _rules[i].Eval(signals);
            }

            Matrix.Mult(_activations, _deltas, outputs);
            for (var i = 0; i < outputs.Length; ++i)
            {
                outputs[i] = Mathf.Clamp(outputs[i], 0.0f, 1.0f);
            }
        }
    }
}
