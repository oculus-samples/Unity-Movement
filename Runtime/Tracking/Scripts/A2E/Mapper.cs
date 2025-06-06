// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Mapper class. Used by <see cref="FaceDriver"/> to map from
    /// weights provider names to drivers (or blendshapes on character).
    /// </summary>
    public class Mapper
    {
        private class Rule
        {
            public int FirstIndex;
            public int SecondIndex;
            public Rule(int first, int second)
            {
                FirstIndex = first;
                SecondIndex = second;
            }
        }


        private readonly Rule[] _rules;
        private readonly int _expectedSrcSize, _expectedDstSize;

        /// <summary>
        /// Main constructor where source and destination names are mapped to each other and stored.
        /// </summary>
        /// <param name="src">Source names.</param>
        /// <param name="dest">Destination names.</param>
        /// <param name="warnIfSrcNotFound">Warning in case source name is not found in destination.</param>
        /// <param name="warnIfDestNotDriven">Warning if any names rename in destination after mapping has completed.</param>
        public Mapper(IList<string> src, IList<string> dest, Action<IReadOnlyCollection<string>> warnIfSrcNotFound, Action<IReadOnlyCollection<string>> warnIfDestNotDriven)
        {
            _expectedSrcSize = src.Count;
            _expectedDstSize = dest.Count;

            var notFound = new SortedSet<string>();
            var remainderInDest = dest.ToHashSet();

            if (src.Count == 0)
            {
                foreach (var s in src)
                {
                    notFound.Add(s);
                }
            }

            if (dest.Count > 0 && src.Count > 0)
            {
                List<Rule> rules = new List<Rule>();
                for (var i = 0; i < src.Count; ++i)
                {
                    var index = dest.IndexOf(src[i]);
                    if (index < 0)
                    {
                        notFound.Add(src[i]);
                    }
                    else
                    {
                        rules.Add(new Rule(i, index));
                        remainderInDest.Remove(dest[index]);
                    }
                }
                _rules = rules.ToArray();
            }

            if (notFound.Count > 0 && warnIfSrcNotFound != null)
            {
                warnIfSrcNotFound(notFound);
            }

            if (remainderInDest.Count > 0 && warnIfDestNotDriven != null)
            {
                warnIfDestNotDriven(remainderInDest);
            }
        }

        /// <summary>
        /// Uses stored mapping to map from source to destination.
        /// </summary>
        /// <param name="src">Source values.</param>
        /// <param name="dest">Destination values.</param>
        /// <exception cref="ArgumentException">Exception thrown if a problem is encountered.</exception>
        public void Map(float[] src, float[] dest)
        {
            if (src.Length != _expectedSrcSize)
            {
                throw new ArgumentException($"Expected source to be of length {_expectedSrcSize}, got {src.Length}");
            }

            if (dest.Length != _expectedDstSize)
            {
                throw new ArgumentException($"Expected destination to be of length {_expectedDstSize}, got {dest.Length}");
            }

            foreach (var t in _rules)
            {
                dest[t.SecondIndex] = src[t.FirstIndex];
            }
        }
    }
}
