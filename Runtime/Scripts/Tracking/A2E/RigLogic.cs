// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Implementation of <see cref="IRigLogic"/> used for
    /// more complex characters.
    /// </summary>
    public class RigLogic : IRigLogic
    {
        /// <summary>
        /// Driver struct, which is container for name and suffix.
        /// </summary>
        public readonly struct Driver
        {
            /// <summary>
            /// Driver constructor.
            /// </summary>
            /// <param name="n">Name.</param>
            /// <param name="s">Suffix.</param>
            public Driver(string n, string s)
            {
                Name = n;
                Suffix = s;
            }

            /// <summary>
            /// Readonly name field.
            /// </summary>
            public readonly string Name;
            /// <summary>
            /// Readonly suffix field.
            /// </summary>
            public readonly string Suffix;

            /// <summary>
            /// Returns string representation.
            /// </summary>
            /// <returns>String representation.</returns>
            public override string ToString()
            {
                return Suffix.Length > 0 ? $"{Name}_{Suffix}" : Name;
            }
        }

        /// <summary>
        /// Returns all drivers as an <see cref="IList{T}"/>.
        /// </summary>
        public IList<string> Drivers => _drivers.ConvertAll((driver => driver.ToString()));
        private readonly List<Driver> _drivers = new List<Driver>();

        private static readonly Regex DirectRegex = new Regex("^([a-z][a-zA-Z]+)(_([A-Z]{1,2}))?$");
        private static readonly Regex InbwRegex = new Regex("^([a-z][a-zA-Z]+)([0-9]{1,2})(_[A-Z]{1,2})?$");
        private static readonly Regex CorrRegex = new Regex("^([a-z][a-zA-Z]+([0-9]{1,2})?)(_([a-z][a-zA-Z]+([0-9]{1,2})?))+(_([A-Z]{1,2}))?$");

        private readonly List<int> _direct = new List<int>();
        private readonly Dictionary<int, List<KeyValuePair<int, float>>> _inbw = new();
        private int _inbwCount = 0;
        private readonly Dictionary<int, List<int>> _corr = new();

        /// <summary>
        /// Returns the number of output signals.
        /// </summary>
        public int OutputSignalsCount => _direct.Count + _inbwCount + _corr.Count;

        internal static Driver? MatchDirect(string name)
        {
            var match = DirectRegex.Match(name);
            if (!match.Success) return null;

            return new Driver(match.Groups[1].Value, match.Groups.Last().Value);
        }

        internal struct InbwMatch
        {
            /// <summary>
            /// <see cref="InbwMatch"/> constructor.
            /// </summary>
            /// <param name="val">value.</param>
            /// <param name="dr">Driver.</param>
            public InbwMatch(int val, string dr)
            {
                Value = val;
                Driver = dr;
            }

            /// <summary>
            /// Readonly value field.
            /// </summary>
            public readonly int Value;
            /// <summary>
            /// Readonly driver field.
            /// </summary>
            public readonly string Driver;
        }

        internal static InbwMatch? MatchInbw(string name, bool includeDirect = false)
        {
            var match = InbwRegex.Match(name);
            if (match.Success)
            {
                return new InbwMatch(int.Parse(match.Groups[2].Value), match.Groups[1].Value + match.Groups[3].Value);
            }

            if (includeDirect)
            {
                match = DirectRegex.Match(name);
                if (match.Success)
                {
                    return new InbwMatch(100, name);
                }
            }

            return null;
        }

        internal readonly struct CorrMatch : IEquatable<CorrMatch>
        {
            /// <summary>
            /// <see cref="CorrMatch"/> constructor.
            /// </summary>
            /// <param name="s">Suffix.</param>
            /// <param name="dr">Drivers.</param>
            public CorrMatch(string s, List<InbwMatch> dr = null)
            {
                Suffix = s;
                Drivers = dr ?? new List<InbwMatch>();
            }

            /// <summary>
            /// Checks equivalence with other <see cref="CorrMatch"/> object.
            /// </summary>
            /// <param name="obj">Object that is <see cref="CorrMatch"/> instance.</param>
            /// <returns>True if equal, false if not.</returns>
            public override bool Equals(object obj) => obj is CorrMatch other && this.Equals(other);

            /// <summary>
            /// Checks equivalence  with other <see cref="CorrMatch"/> object.
            /// </summary>
            /// <param name="o">Other <see cref="CorrMatch"/> object.</param>
            /// <returns>True if equals, false if not.</returns>
            public bool Equals(CorrMatch o)
            {
                return Suffix == o.Suffix && Drivers.SequenceEqual(o.Drivers);
            }

            /// <summary>
            /// Returns hash code corresponding to this object.
            /// </summary>
            /// <returns>Hash code as integer.</returns>
            public override int GetHashCode() => (Drivers, Suffix).GetHashCode();

            /// <summary>
            /// Readonly list of drivers.
            /// </summary>
            public readonly List<InbwMatch> Drivers;
            /// <summary>
            /// Readonly suffix.
            /// </summary>
            public readonly string Suffix;

            /// <summary>
            /// Returns string representation.
            /// </summary>
            /// <returns>String representation.</returns>
            public override string ToString()
            {
                return Drivers.Aggregate("", (current, d) => current + (current.Length > 0 ? "_" : "") +
                    $"{d.Driver}{(d.Value == 100 ? "" : d.Value.ToString())}") +
                    (Suffix == "" ? "" : $"_{Suffix}");
            }
        }

        internal static CorrMatch? MatchCorr(string name)
        {
            var match = CorrRegex.Match(name);
            if (!match.Success) return null;

            var result = new CorrMatch(match.Groups.Last().Value);

            result.Drivers.Add(MatchInbw(match.Groups[1].Value, true).Value);
            foreach (var i in match.Groups[4].Captures)
            {
                result.Drivers.Add(MatchInbw(i.ToString(), true).Value);
            }

            return result;
        }

        /// <summary>
        /// Constructor accepting a list of names.
        /// </summary>
        /// <param name="names">List of names.</param>
        public RigLogic(IList<string> names)
        {
            // First, collect all the pass-through signals
            for (var i = 0; i < names.Count; ++i)
            {
                if (MatchDirect(names[i]) is var dir && dir != null)
                {
                    _drivers.Add(dir.Value);
                    _direct.Add(i);
                }
            }

            // Collect all inbetween signals
            for (var i = 0; i < names.Count; ++i)
            {
                if (MatchInbw(names[i]) is var inbw && inbw != null)
                {
                    var driver = _drivers.FindIndex((Driver d) => inbw.Value.Driver == d.ToString());
                    if (driver < 0)
                    {
                        Debug.LogWarning($"Could not find driver {inbw.Value.Driver} for inbetween {names[i]}");
                        continue;
                    }
                    var driverIndex = _direct[driver];

                    if (!_inbw.ContainsKey(driverIndex))
                    {
                        _inbw[driverIndex] = new List<KeyValuePair<int, float>>() { new(-1, 0.0f), new(-1, 1.0f) };
                    }

                    _inbw[driverIndex].Add(new KeyValuePair<int, float>(i, inbw.Value.Value / 100.0f));
                    ++_inbwCount;
                }
            }

            foreach (var i in _inbw)
            {
                i.Value.Sort((a, b) => Math.Sign(a.Value - b.Value));
            }

            // Collect all correctives
            for (var i = 0; i < names.Count; ++i)
            {
                if (MatchCorr(names[i]) is var corr && corr != null)
                {
                    var allDrivers = new List<int>();
                    foreach (var source in corr.Value.Drivers)
                    {
                        var plainDriverIndex =
                            _drivers.FindIndex(d => d.Name == source.Driver && corr.Value.Suffix.Contains(d.Suffix));
                        if (plainDriverIndex < 0)
                        {
                            Debug.LogWarning($"Driver for {source.Driver.ToString()}{(source.Value < 100 ? source.Value.ToString() : "")} from {corr} not found!");
                            continue;
                        }

                        if (source.Value == 100)
                        {
                            allDrivers.Add(_direct[plainDriverIndex]);
                        }
                        else
                        {
                            var driver = _drivers[plainDriverIndex];
                            var inbwIndex = names.IndexOf($"{driver.Name}{source.Value}" + (driver.Suffix.Length == 0 ? "" : $"_{driver.Suffix}"));
                            allDrivers.Add(inbwIndex);
                        }
                    }

                    _corr.Add(i, allDrivers);
                }
            }

            var handledCount = _direct.Count + _inbwCount + _corr.Count;
            if (handledCount != names.Count)
            {
                Debug.LogWarning($"All shapes should be matched, each only once - expected {names.Count}, handling only {handledCount}");
            }
        }

        /// <summary>
        /// Produces output signals from a list of drivers, the latter of which can
        /// contain correctives. The output signals can be used to drive a skinned mesh.
        /// </summary>
        /// <param name="driverWeights">Driver weights.</param>
        /// <param name="outputSignals">Output signals.</param>
        public void Eval(IReadOnlyList<float> driverWeights, IList<float> outputSignals)
        {
            Debug.Assert(driverWeights.Count == _direct.Count);
            Debug.Assert(outputSignals.Count == OutputSignalsCount);

            for (var i = 0; i < outputSignals.Count; ++i)
            {
                outputSignals[i] = 0.0f;
            }

            // Pass-through signals
            for (var i = 0; i < _direct.Count; ++i)
            {
                outputSignals[_direct[i]] = driverWeights[i];
            }

            // Inbetween signals
            foreach (var (key, val) in _inbw)
            {
                var index = 0;
                while (index < val.Count)
                {
                    if (val[index].Value >= outputSignals[key]) break;
                    ++index;
                }
                if (index < 1) continue;

                var w = (outputSignals[key] - val[index - 1].Value) / (val[index].Value - val[index - 1].Value);
                Debug.Assert(w >= 0.0f && w <= 1.0f);

                if (val[index].Key >= 0) outputSignals[val[index].Key] = w;
                if (val[index - 1].Key >= 0) outputSignals[val[index - 1].Key] = 1.0f - w;
            }

            // Corrective signals
            foreach (var c in _corr)
            {
                var w = 1.0f;
                foreach (var i in c.Value)
                {
                    w *= outputSignals[i];
                }
                outputSignals[c.Key] = w;
            }
        }
    }
}
