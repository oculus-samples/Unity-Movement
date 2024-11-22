// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DeformationRig.Utils.Deprecated;

namespace DeformationRig.Deprecated
{
    /// <summary>
    /// A naming scheme describes how to parse the various corrective shapes' ICorrectiveShape
    /// representations from a list of shape names.
    /// </summary>
    public interface INamingScheme
    {
        /// <summary>
        /// Parses all provided shape names looking for known corrective shapes.
        ///
        /// This list is returned IN ORDER. Meaning, if all correctives are driven in the order they
        /// are returned, all dependent blendshapes will have their driver values available when
        /// needed.
        /// </summary>
        List<ICorrectiveShape> ParseCorrectives(List<string> shapeNames);

        /// <summary>The human-readable name of this parsing version.</summary>
        string VersionName { get; }
    }

    /// <summary>
    /// Available naming schemes to use when parsing the names of corrective shapes.
    /// </summary>
    public static class NamingSchemes
    {
        private class NamingSchemeV0 : INamingScheme
        {
            /// <summary>
            /// Regex describing the name of a directly driven shape.
            ///
            /// Group 0, Capture 0: Full matching shape name (unused)
            /// Group 1, Capture 0: Shape name
            /// Group 2, Capture 0: Suffix (caps expected), prefixed with underscore (unused)
            /// Group 3, Capture 0: Suffix (caps expected)
            /// </summary>
            private static readonly Regex DRIVER_PATTERN = new Regex(
                @"^([a-zA-Z]{4,}|pc_[0-9]{1,2})(_([A-Z]{1,2}))?$");

            /// <summary>
            /// Regex describing the name of a directly driven shape (alternate pattern, used by
            /// Mobile Genesis).
            ///
            /// Group 0, Capture 0: Full matching shape name (unused)
            /// Group 1, Capture 0: Shape name
            /// </summary>
            private static readonly Regex ALT_DRIVER_PATTERN = new Regex(
                @"^(Blendshape_[0-9]+_0)$");

            /// <summary>
            /// Regex describing the name of an inbetween shape.
            ///
            /// Group 0, Capture 0: Full matching shape name (unused)
            /// Group 1, Capture 0: Shape name without value
            /// Group 2, Capture 0: Value
            /// Group 3, Capture 0: Suffix, prefixed with underscore (unused)
            /// Group 4, Capture 0: Suffix
            /// </summary>
            private static readonly Regex IN_BETWEEN_PATTERN = new Regex(
                @"^([a-zA-Z]+)([0-9]{2})(_([A-Z]{1,2}))?$");

            /// <summary>
            /// Regex describing the name of a combination shape.
            ///
            /// Group 0, Capture 0: Full matching shape name (unused)
            /// Group 1, Capture 0: All non-suffix elements, still joined
            /// Group 2, Capture N: Combo element N+1 prefixed with underscore
            ///   Example: full shape name: mesh.foo_bar_baz, N = 0, returns "_bar"
            /// Group 3, Capture 0: Suffix, prefixed with underscore (unused)
            /// Group 4, Capture 0: Suffix
            /// </summary>
            private static readonly Regex COMBINATION_PATTERN = new Regex(
                @"^([a-zA-Z0-9]{4,25}(_[a-zA-Z0-9]{4,25})+)(_([A-Z]{1,2}))?$");

            public string VersionName => "V0";

            /// <inheritdoc />
            public List<ICorrectiveShape> ParseCorrectives(List<string> shapeNames)
            {
                var drivers = new List<DriverShape>();
                var combinations = new List<Combination>();
                var partialInBetweens = new List<InBetween.Partial>();

                for (int shapeIdx = 0; shapeIdx < shapeNames.Count; shapeIdx++)
                {
                    if (TryParseDriver(shapeNames, shapeIdx, out var driver))
                    {
                        drivers.Add(driver);
                    }
                    else if (TryParseCombination(shapeNames, shapeIdx, out var combination))
                    {
                        combinations.Add(combination);
                    }
                    else if (TryParseInBetween(shapeNames, shapeIdx, out var inBetween))
                    {
                        partialInBetweens.Add(inBetween);
                    }
                    else
                    {
                        Debug.LogWarning($"Found unparseable shape name [{shapeNames[shapeIdx]}].");
                    }
                }

                var inBetweens = InBetween.BuildFromPartials(partialInBetweens);
                var ret = new List<ICorrectiveShape>(
                    drivers.Count + combinations.Count + inBetweens.Count);
                ret.AddRange(drivers);
                ret.AddRange(inBetweens);
                ret.AddRange(combinations);
                return ret;
            }

            private static bool TryParseDriver(
                List<string> shapeNames, int index, out DriverShape driver)
            {
                var shapeName = shapeNames[index];
                var match = DRIVER_PATTERN.Match(shapeName);
                if (match.Success)
                {
                    if (match.Groups.Count < 4)
                    {
                        throw new MissingMemberException(
                            $"Matched a standard driver shape [{shapeName}], but was missing "
                            + "parsed data.");
                    }

                    driver = new DriverShape(index);
                    return true;
                }

                match = ALT_DRIVER_PATTERN.Match(shapeName);
                if (match.Success)
                {
                    if (match.Groups.Count < 2)
                    {
                        throw new MissingMemberException(
                            $"Matched an alt driver shape [{shapeName}], but was missing parsed "
                            + "data.");
                    }

                    driver = new DriverShape(index);
                    return true;
                }

                driver = null;
                return false;
            }

            private static bool TryParseCombination(
                List<string> shapeNames, int index, out Combination combination)
            {
                var shapeName = shapeNames[index];
                var match = COMBINATION_PATTERN.Match(shapeName);
                if (!match.Success)
                {
                    combination = null;
                    return false;
                }
                else if (match.Groups.Count < 5)
                {
                    throw new MissingMemberException(
                        $"Matched a combination shape [{shapeName}], but was missing parsed data.");
                }

                var elements = match.Groups[1].Captures[0].ToString().Split("_");

                var suffix = "";
                if (match.Groups[4].Captures.Count > 0)
                {
                    suffix = match.Groups[4].Captures[0].ToString();
                }

                var driverIndices = elements.Select(element =>
                    {
                        var idx = FindShape(shapeNames, element, suffix);
                        if (idx == -1)
                        {
                            throw new IndexOutOfRangeException(
                                $"Invalid named combo: {shapeName}. "
                                + $"Could not find driver shape {element} with suffix '{suffix}'");
                        }
                        return idx;
                    })
                    .ToArray();

                combination = new Combination()
                {
                    DrivenIndex = index,
                    DriverIndices = driverIndices,
                };
                return true;
            }

            private static bool TryParseInBetween(
                List<string> shapeNames, int index, out InBetween.Partial inBetween)
            {
                var shapeName = shapeNames[index];
                Match match = IN_BETWEEN_PATTERN.Match(shapeName);
                if (!match.Success)
                {
                    inBetween = null;
                    return false;
                }

                if (match.Groups.Count < 5)
                {
                    throw new MissingMemberException(
                        $"Matched an in between shape [{shapeName}], but was missing parsed data.");
                }

                var driverName = match.Groups[1].Captures[0].ToString();
                if (!Int32.TryParse(match.Groups[2].Captures[0].ToString(), out int peakValue))
                {
                    throw new ArgumentException(
                        $"Could not parse value from in between {shapeName}");
                }
                else if (peakValue <= 0 || peakValue >= 100)
                {
                    throw new ArgumentException(
                        $"Parsed value in {shapeName} is outside of (0, 100): {peakValue}");

                }

                var suffix = "";
                if (match.Groups[4].Captures.Count > 0)
                {
                    suffix = match.Groups[4].Captures[0].ToString();
                }

                var driverIndex = FindShape(shapeNames, driverName, suffix);
                if (driverIndex == -1)
                {
                    throw new IndexOutOfRangeException(
                        $"Invalid named in between: {shapeName}. "
                        + $"Could not find driver shape {driverName} with suffix '{suffix}'");
                }

                inBetween = new InBetween.Partial()
                {
                    DrivenIndex = index,
                    DriverIndex = driverIndex,
                    PeakValue = ConversionHelpers.PercentToDecimal(peakValue),
                };
                return true;
            }

            /// <summary>
            /// Searches a list of shape names for a given shape, trying different combinations of
            /// potential suffixes if the shape is not immediately found.
            ///
            /// TODO: This could be made more elegant to actually construct all possible suffixes
            /// from a suffix of arbitrary length, but when we expect a suffix to be 0-2 characters,
            /// manually checking each is probably fine. If we start having 3 character suffixes,
            /// this should be replaced with the elegant version.
            /// </summary>
            private static int FindShape(
                List<string> shapeNames, string shapeName, string fullSuffix)
            {
                var driverIndex = shapeNames.IndexOf(shapeName);
                if (driverIndex != -1)
                {
                    return driverIndex;
                }

                if (fullSuffix.Length > 2)
                {
                    throw new ArgumentException($"Expected suffix of length 0-2, got {fullSuffix}");
                }

                if (fullSuffix.Length > 0)
                {
                    driverIndex = shapeNames.IndexOf($"{shapeName}_{fullSuffix[0]}");
                    if (driverIndex != -1)
                    {
                        return driverIndex;
                    }
                }

                if (fullSuffix.Length > 1)
                {
                    driverIndex = shapeNames.IndexOf($"{shapeName}_{fullSuffix[1]}");
                    if (driverIndex != -1)
                    {
                        return driverIndex;
                    }

                    driverIndex = shapeNames.IndexOf($"{shapeName}_{fullSuffix}");
                    if (driverIndex != -1)
                    {
                        return driverIndex;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// The V0 corrective naming scheme.
        /// </summary>
        public static readonly INamingScheme V0 = new NamingSchemeV0();
    }
}
