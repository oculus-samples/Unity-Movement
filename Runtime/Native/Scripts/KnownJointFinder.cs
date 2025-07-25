// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Utility class to find known joints in a skeleton hierarchy
    /// </summary>
    public static class KnownJointFinder
    {
        /// <summary>
        /// Find known joints in a skeleton based on joint names and parent-child relationships
        /// </summary>
        /// <param name="jointNames">Array of joint names in the skeleton</param>
        /// <param name="parentJointNames">Array of parent joint names for each joint</param>
        /// <returns>Array of known joint names</returns>
        public static string[] FindKnownJoints(string[] jointNames, string[] parentJointNames)
        {
            if (jointNames == null || parentJointNames == null || jointNames.Length != parentJointNames.Length)
            {
                return new string[(int)KnownJointType.KnownJointCount];
            }

            var knownJoints = new string[(int)KnownJointType.KnownJointCount];

            // Detect and trim common prefixes/suffixes for better pattern matching
            var commonPrefixSuffix = DetectCommonPrefixSuffix(jointNames);
            var normalizedJointNames =
                NormalizeJointNames(jointNames, commonPrefixSuffix.prefix, commonPrefixSuffix.suffix);
            var normalizedParentNames =
                NormalizeJointNames(parentJointNames, commonPrefixSuffix.prefix, commonPrefixSuffix.suffix);

            // Create mapping from normalized names back to original names
            var normalizedToOriginal = CreateNormalizedMapping(jointNames, normalizedJointNames);
            var hierarchy = BuildHierarchy(normalizedJointNames, normalizedParentNames);

            // Find root joint (has no parent)
            var rootJoint = FindRootJoint(normalizedJointNames, normalizedParentNames);
            if (!string.IsNullOrEmpty(rootJoint))
            {
                knownJoints[(int)KnownJointType.Root] = normalizedToOriginal[rootJoint];
            }

            // Find hips (child of root with spine + left/right upper legs)
            var hipsJoint = FindHipsJoint(rootJoint, hierarchy, normalizedJointNames);
            if (!string.IsNullOrEmpty(hipsJoint))
            {
                knownJoints[(int)KnownJointType.Hips] = normalizedToOriginal[hipsJoint];
            }

            // Find chest (highest spine joint with neck + shoulders)
            var chestJoint = FindChestJoint(hipsJoint, hierarchy, normalizedJointNames);
            if (!string.IsNullOrEmpty(chestJoint))
            {
                knownJoints[(int)KnownJointType.Chest] = normalizedToOriginal[chestJoint];
            }

            // Find neck (child of chest)
            var neckJoint = FindNeckJoint(chestJoint, hierarchy, normalizedJointNames);
            if (!string.IsNullOrEmpty(neckJoint))
            {
                knownJoints[(int)KnownJointType.Neck] = normalizedToOriginal[neckJoint];
            }

            // Find upper legs (children of hips)
            var upperLegs = FindUpperLegs(hipsJoint, hierarchy, normalizedJointNames);
            if (upperLegs.leftUpperLeg != null)
            {
                knownJoints[(int)KnownJointType.LeftUpperLeg] = normalizedToOriginal[upperLegs.leftUpperLeg];
            }

            if (upperLegs.rightUpperLeg != null)
            {
                knownJoints[(int)KnownJointType.RightUpperLeg] = normalizedToOriginal[upperLegs.rightUpperLeg];
            }

            // Find ankles (feet - descendants of upper legs)
            var ankles = FindAnkles(upperLegs.leftUpperLeg, upperLegs.rightUpperLeg, hierarchy, normalizedJointNames);
            if (ankles.leftAnkle != null)
            {
                knownJoints[(int)KnownJointType.LeftAnkle] = normalizedToOriginal[ankles.leftAnkle];
            }

            if (ankles.rightAnkle != null)
            {
                knownJoints[(int)KnownJointType.RightAnkle] = normalizedToOriginal[ankles.rightAnkle];
            }

            // Find shoulders and upper arms
            var shoulders = FindShoulders(chestJoint, hierarchy, normalizedJointNames);
            var upperArms = FindUpperArms(shoulders.leftShoulder, shoulders.rightShoulder, chestJoint, hierarchy,
                normalizedJointNames);
            if (upperArms.leftUpperArm != null)
            {
                knownJoints[(int)KnownJointType.LeftUpperArm] = normalizedToOriginal[upperArms.leftUpperArm];
            }

            if (upperArms.rightUpperArm != null)
            {
                knownJoints[(int)KnownJointType.RightUpperArm] = normalizedToOriginal[upperArms.rightUpperArm];
            }

            // Find wrists (hands)
            var wrists = FindWrists(upperArms.leftUpperArm, upperArms.rightUpperArm, hierarchy, normalizedJointNames);
            if (wrists.leftWrist != null)
            {
                knownJoints[(int)KnownJointType.LeftWrist] = normalizedToOriginal[wrists.leftWrist];
            }

            if (wrists.rightWrist != null)
            {
                knownJoints[(int)KnownJointType.RightWrist] = normalizedToOriginal[wrists.rightWrist];
            }

            return knownJoints;
        }

        private static Dictionary<string, List<string>> BuildHierarchy(string[] jointNames, string[] parentJointNames)
        {
            var hierarchy = new Dictionary<string, List<string>>();

            for (int i = 0; i < jointNames.Length; i++)
            {
                var joint = jointNames[i];
                var parent = parentJointNames[i];

                if (!string.IsNullOrEmpty(parent))
                {
                    if (!hierarchy.ContainsKey(parent))
                    {
                        hierarchy[parent] = new List<string>();
                    }

                    hierarchy[parent].Add(joint);
                }
            }

            return hierarchy;
        }

        private static (string prefix, string suffix) DetectCommonPrefixSuffix(string[] jointNames)
        {
            if (jointNames == null || jointNames.Length < 2)
            {
                return ("", "");
            }

            var validNames = jointNames.Where(name => !string.IsNullOrEmpty(name)).ToArray();
            if (validNames.Length < 2)
            {
                return ("", "");
            }

            const double threshold = 0.7; // 70% threshold for both prefix and suffix detection

            // Find common prefix using dynamic detection
            string commonPrefix = DetectCommonPrefix(validNames, threshold);

            // Find common suffix using dynamic detection
            string commonSuffix = DetectCommonSuffix(validNames, threshold);

            return (commonPrefix, commonSuffix);
        }

        private static string DetectCommonPrefix(string[] validNames, double threshold)
        {
            var minThreshold = (int)(validNames.Length * threshold);

            // Try different approaches to find the best common prefix
            string bestPrefix = "";
            int bestCount = 0;

            // Dynamic detection by analyzing character-by-character
            if (bestCount < minThreshold)
            {
                // Find the longest common prefix across all names
                var sortedNames = validNames.OrderBy(name => name.Length).ToArray();
                var shortestName = sortedNames[0];

                for (int prefixLength = 1; prefixLength <= shortestName.Length; prefixLength++)
                {
                    var potentialPrefix = shortestName.Substring(0, prefixLength);
                    var count = validNames.Count(name =>
                        name.StartsWith(potentialPrefix, StringComparison.OrdinalIgnoreCase));

                    if (count >= minThreshold && count > bestCount)
                    {
                        bestPrefix = potentialPrefix;
                        bestCount = count;
                    }
                    else if (count < minThreshold)
                    {
                        // Stop if we've gone too far and lost too many matches
                        break;
                    }
                }
            }

            // Pattern-based detection (look for common separators)
            if (bestCount < minThreshold)
            {
                var separators = new[] { "_", ":", ".", "-" };
                foreach (var separator in separators)
                {
                    var prefixCandidates = validNames
                        .Where(name => name.Contains(separator))
                        .Select(name => name.Substring(0, name.IndexOf(separator, StringComparison.Ordinal) + 1))
                        .GroupBy(prefix => prefix, StringComparer.OrdinalIgnoreCase)
                        .Where(group => group.Count() >= minThreshold)
                        .OrderByDescending(group => group.Count())
                        .FirstOrDefault();

                    if (prefixCandidates != null && prefixCandidates.Count() > bestCount)
                    {
                        bestPrefix = prefixCandidates.Key;
                        bestCount = prefixCandidates.Count();
                    }
                }
            }

            return bestCount >= minThreshold ? bestPrefix : "";
        }

        private static string DetectCommonSuffix(string[] validNames, double threshold)
        {
            var minThreshold = (int)(validNames.Length * threshold);

            string bestSuffix = "";
            int bestCount = 0;

            // 1: Check predefined common suffixes first
            var commonSuffixes = new[] { ".x", ".y", ".z", "_end", "_tip", ".001", ".L", ".R", "_L", "_R" };
            foreach (var suffix in commonSuffixes)
            {
                var count = validNames.Count(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                if (count >= minThreshold && count > bestCount)
                {
                    bestSuffix = suffix;
                    bestCount = count;
                }
            }

            // 2: Dynamic detection by analyzing character-by-character from the end
            if (bestCount < minThreshold)
            {
                var sortedNames = validNames.OrderBy(name => name.Length).ToArray();
                var shortestName = sortedNames[0];

                for (int suffixLength = 1; suffixLength <= shortestName.Length; suffixLength++)
                {
                    var potentialSuffix = shortestName.Substring(shortestName.Length - suffixLength);
                    var count = validNames.Count(name =>
                        name.EndsWith(potentialSuffix, StringComparison.OrdinalIgnoreCase));

                    if (count >= minThreshold && count > bestCount)
                    {
                        bestSuffix = potentialSuffix;
                        bestCount = count;
                    }
                    else if (count < minThreshold)
                    {
                        // Stop if we've gone too far and lost too many matches
                        break;
                    }
                }
            }

            // 3: Pattern-based detection (look for common separators from the end)
            if (bestCount < minThreshold)
            {
                var separators = new[] { "_", ".", "-" };
                foreach (var separator in separators)
                {
                    var suffixCandidates = validNames
                        .Where(name => name.Contains(separator))
                        .Select(name =>
                        {
                            var lastIndex = name.LastIndexOf(separator, StringComparison.Ordinal);
                            return lastIndex >= 0 ? name.Substring(lastIndex) : "";
                        })
                        .Where(suffix => !string.IsNullOrEmpty(suffix))
                        .GroupBy(suffix => suffix, StringComparer.OrdinalIgnoreCase)
                        .Where(group => group.Count() >= minThreshold)
                        .OrderByDescending(group => group.Count())
                        .FirstOrDefault();

                    if (suffixCandidates != null && suffixCandidates.Count() > bestCount)
                    {
                        bestSuffix = suffixCandidates.Key;
                        bestCount = suffixCandidates.Count();
                    }
                }
            }

            return bestCount >= minThreshold ? bestSuffix : "";
        }

        private static string[] NormalizeJointNames(string[] jointNames, string prefix, string suffix)
        {
            if (jointNames == null)
            {
                return null;
            }

            var normalizedNames = new string[jointNames.Length];

            for (int i = 0; i < jointNames.Length; i++)
            {
                var name = jointNames[i];
                if (string.IsNullOrEmpty(name))
                {
                    normalizedNames[i] = name;
                    continue;
                }

                // Remove prefix
                if (!string.IsNullOrEmpty(prefix) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                }

                // Remove suffix
                if (!string.IsNullOrEmpty(suffix) && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                }

                normalizedNames[i] = name;
            }

            return normalizedNames;
        }

        private static Dictionary<string, string> CreateNormalizedMapping(string[] originalNames,
            string[] normalizedNames)
        {
            var mapping = new Dictionary<string, string>();

            for (int i = 0; i < originalNames.Length && i < normalizedNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(normalizedNames[i]))
                {
                    mapping[normalizedNames[i]] = originalNames[i];
                }
            }

            return mapping;
        }

        private static string FindRootJoint(string[] jointNames, string[] parentJointNames)
        {
            // Create sets for efficient lookup
            var jointNameSet = new HashSet<string>(jointNames.Where(name => !string.IsNullOrEmpty(name)));
            var parentNameSet = new HashSet<string>(parentJointNames.Where(name => !string.IsNullOrEmpty(name)));

            // Method 1: Find joints that are referenced as parents but don't exist in jointNames
            var rootCandidates = parentNameSet.Except(jointNameSet).ToList();
            if (rootCandidates.Count == 1)
            {
                return rootCandidates[0];
            }

            // Method 2: Find joint with empty parent (true root)
            for (int i = 0; i < jointNames.Length; i++)
            {
                if (string.IsNullOrEmpty(parentJointNames[i]) || parentJointNames[i].Length == 0)
                {
                    return jointNames[i];
                }
            }

            // Method 3: If multiple root candidates, try to find the best one
            if (rootCandidates.Count > 1)
            {
                // Prefer names that contain "root"
                var rootNamedCandidates = rootCandidates.Where(name =>
                    name.ToLower().Contains("root")).ToList();
                if (rootNamedCandidates.Count == 1)
                {
                    return rootNamedCandidates[0];
                }

                // Return the first candidate as fallback
                return rootCandidates[0];
            }

            // Method 4: Fallback - look for joints that reference themselves or common root names
            for (int i = 0; i < jointNames.Length; i++)
            {
                var parentName = parentJointNames[i];
                if (parentName == jointNames[i] ||
                    parentName.ToLower() == "root" ||
                    parentName.ToLower() == "armature" ||
                    parentName.ToLower() == "skeleton")
                {
                    return jointNames[i];
                }
            }

            return jointNames[0];
        }

        private static string FindHipsJoint(string rootJoint, Dictionary<string, List<string>> hierarchy,
            string[] jointNames)
        {
            if (string.IsNullOrEmpty(rootJoint))
            {
                return null;
            }

            // First try direct name matching for hips
            var hipsCandidate = FindJointByNamePatterns(jointNames, new[]
            {
                "hips", "pelvis", "hip", "root"
            });

            if (!string.IsNullOrEmpty(hipsCandidate) && IsValidHipsWithDepth(hipsCandidate, rootJoint, hierarchy))
            {
                return hipsCandidate;
            }

            // If root has children, look for structural hips
            if (!hierarchy.TryGetValue(rootJoint, out var rootChildren))
            {
                return null;
            }

            // Look for a child that has spine + left/right legs with sufficient depth
            foreach (var child in rootChildren)
            {
                if (hierarchy.ContainsKey(child) && hierarchy[child].Count >= 2)
                {
                    var grandChildren = hierarchy[child];
                    int validSpineCount = 0;
                    int validLegCount = 0;

                    foreach (var grandChild in grandChildren)
                    {
                        var name = grandChild.ToLower();
                        if (IsSpineJoint(name) && ValidateSpineDepth(grandChild, hierarchy))
                        {
                            validSpineCount++;
                        }
                        else if (IsLegJoint(name) && ValidateUpperLegDepth(grandChild, hierarchy))
                        {
                            validLegCount++;
                        }
                    }

                    // Should have at least 1 valid spine and 2 valid legs, or just 2 valid legs
                    if ((validSpineCount >= 1 && validLegCount >= 2) || validLegCount >= 2)
                    {
                        return child;
                    }
                }
            }

            // Fallback: look for joint with "hip" or "pelvis" in name, but still validate depth
            foreach (var child in rootChildren)
            {
                var name = child.ToLower();
                if ((name.Contains("hip") || name.Contains("pelvis")) && IsValidHipsWithDepth(child, rootJoint, hierarchy))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsValidHips(string candidate, string rootJoint, Dictionary<string, List<string>> hierarchy)
        {
            // Check if candidate is child of root or is root itself
            if (candidate == rootJoint)
            {
                return false;
            }

            // Should have children (legs and spine)
            return hierarchy.ContainsKey(candidate) && hierarchy[candidate].Count >= 2;
        }

        /// <summary>
        /// Validate that a hips candidate has valid children with sufficient depth for legs and spine
        /// </summary>
        /// <param name="candidate">The hips candidate to validate</param>
        /// <param name="rootJoint">The root joint</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <returns>True if the hips candidate has valid legs and spine with sufficient depth</returns>
        private static bool IsValidHipsWithDepth(string candidate, string rootJoint, Dictionary<string, List<string>> hierarchy)
        {
            // First check basic validity
            if (!IsValidHips(candidate, rootJoint, hierarchy))
            {
                return false;
            }

            // Check that children have sufficient depth
            var children = hierarchy[candidate];
            int validSpineCount = 0;
            int validLegCount = 0;

            foreach (var child in children)
            {
                var name = child.ToLower();
                if (IsSpineJoint(name) && ValidateSpineDepth(child, hierarchy))
                {
                    validSpineCount++;
                }
                else if (IsLegJoint(name) && ValidateUpperLegDepth(child, hierarchy))
                {
                    validLegCount++;
                }
            }

            // Should have at least 1 valid spine and 2 valid legs, or just 2 valid legs
            return (validSpineCount >= 1 && validLegCount >= 2) || validLegCount >= 2;
        }

        private static bool IsSpineJoint(string name)
        {
            return name.Contains("spine") || name.Contains("chest") || name.Contains("torso") ||
                   name.Contains("stomach") || name.Contains("back");
        }

        private static bool IsLegJoint(string name)
        {
            return name.Contains("leg") || name.Contains("thigh") || name.Contains("upleg") ||
                   name.Contains("hip") && (name.Contains("l_") || name.Contains("r_") ||
                                            name.Contains("left") || name.Contains("right") || name.Contains(".l") ||
                                            name.Contains(".r"));
        }

        private static string FindJointByNamePatterns(string[] jointNames, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                foreach (var joint in jointNames)
                {
                    if (joint.ToLower().Contains(pattern.ToLower()))
                    {
                        return joint;
                    }
                }
            }

            return null;
        }

        private static string FindChestJoint(string hipsJoint, Dictionary<string, List<string>> hierarchy,
            string[] jointNames)
        {
            if (string.IsNullOrEmpty(hipsJoint))
            {
                return null;
            }

            // First try direct name matching for chest/upper spine
            var chestCandidate = FindJointByNamePatterns(jointNames, new[]
            {
                "chest", "upperchest", "spine2", "spine_03", "spine3"
            });

            if (!string.IsNullOrEmpty(chestCandidate) && IsValidChest(chestCandidate, hierarchy) && ValidateSpineDepth(chestCandidate, hierarchy))
            {
                return chestCandidate;
            }

            // Traverse up the spine to find chest
            var current = hipsJoint;
            var visited = new HashSet<string>();
            string lastSpineJoint = null;

            while (!string.IsNullOrEmpty(current) && !visited.Contains(current))
            {
                visited.Add(current);

                if (hierarchy.TryGetValue(current, out var children))
                {
                    // Look for chest characteristics: has neck + left/right shoulders
                    foreach (var child in children)
                    {
                        var childName = child.ToLower();
                        if (IsSpineJoint(childName) || childName.Contains("chest"))
                        {
                            if (HasChestCharacteristics(child, hierarchy) && ValidateSpineDepth(child, hierarchy))
                            {
                                return child;
                            }

                            lastSpineJoint = child;
                            // Continue searching deeper
                            var deeperChest = FindChestJoint(child, hierarchy, jointNames);
                            if (!string.IsNullOrEmpty(deeperChest))
                            {
                                return deeperChest;
                            }
                        }
                    }
                }

                // Move to spine child if exists
                current = GetSpineChild(current, hierarchy);
            }

            // Fallback: return the last spine joint found if it has reasonable characteristics and sufficient depth
            if (!string.IsNullOrEmpty(lastSpineJoint) && hierarchy.ContainsKey(lastSpineJoint) &&
                hierarchy[lastSpineJoint].Count >= 2 && ValidateSpineDepth(lastSpineJoint, hierarchy))
            {
                return lastSpineJoint;
            }

            return null;
        }

        private static bool IsValidChest(string candidate, Dictionary<string, List<string>> hierarchy)
        {
            if (!hierarchy.TryGetValue(candidate, out var children))
            {
                return false;
            }

            if (children.Count < 2)
            {
                return false;
            }

            // Should have neck or shoulders as children
            bool hasNeckOrShoulders = false;
            foreach (var child in children)
            {
                var name = child.ToLower();
                if (name.Contains("neck") || name.Contains("shoulder") || name.Contains("clavicle") ||
                    name.Contains("arm"))
                {
                    hasNeckOrShoulders = true;
                    break;
                }
            }

            return hasNeckOrShoulders;
        }

        private static bool HasChestCharacteristics(string joint, Dictionary<string, List<string>> hierarchy)
        {
            if (!hierarchy.TryGetValue(joint, out var children))
            {
                return false;
            }

            if (children.Count < 3)
            {
                return false;
            }

            bool hasNeck = false;
            int shoulderCount = 0;

            foreach (var child in children)
            {
                var name = child.ToLower();
                if (name.Contains("neck") || name.Contains("head"))
                {
                    hasNeck = true;
                }
                else if (name.Contains("shoulder") || name.Contains("clavicle") || name.Contains("arm"))
                {
                    // Check if shoulder has at least 2 children (upper arm + others)
                    if (hierarchy.ContainsKey(child) && hierarchy[child].Count >= 1)
                    {
                        shoulderCount++;
                    }
                }
            }

            return hasNeck && shoulderCount >= 2;
        }

        private static string GetSpineChild(string joint, Dictionary<string, List<string>> hierarchy)
        {
            if (!hierarchy.TryGetValue(joint, out var children))
            {
                return null;
            }

            foreach (var child in children)
            {
                var name = child.ToLower();
                if (name.Contains("spine") || name.Contains("chest") || name.Contains("torso"))
                {
                    return child;
                }
            }

            return null;
        }

        private static string FindNeckJoint(string chestJoint, Dictionary<string, List<string>> hierarchy,
            string[] jointNames)
        {
            if (string.IsNullOrEmpty(chestJoint))
            {
                return null;
            }

            // First try direct name matching
            var neckCandidate = FindJointByNamePatterns(jointNames, new[]
            {
                "neck"
            });

            if (!string.IsNullOrEmpty(neckCandidate))
            {
                return neckCandidate;
            }

            // Look in chest children
            if (hierarchy.TryGetValue(chestJoint, out var children))
            {
                foreach (var child in children)
                {
                    var name = child.ToLower();
                    if (name.Contains("neck"))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Generic method to find paired left/right joints with pattern matching and hierarchy traversal
        /// </summary>
        private static (string left, string right) FindPairedJoints(string parentJoint,
            Dictionary<string, List<string>> hierarchy, string[] jointNames,
            string[] leftPatterns, string[] rightPatterns, Func<string, bool> jointValidator = null)
        {
            if (string.IsNullOrEmpty(parentJoint))
            {
                return (null, null);
            }

            // First try direct name matching
            var leftCandidate = FindJointByNamePatterns(jointNames, leftPatterns);
            var rightCandidate = FindJointByNamePatterns(jointNames, rightPatterns);

            if (!string.IsNullOrEmpty(leftCandidate) && !string.IsNullOrEmpty(rightCandidate))
            {
                return (leftCandidate, rightCandidate);
            }

            // Look in parent's children
            if (!hierarchy.ContainsKey(parentJoint))
                return (leftCandidate, rightCandidate);

            string leftJoint = leftCandidate;
            string rightJoint = rightCandidate;

            foreach (var child in hierarchy[parentJoint])
            {
                var name = child.ToLower();
                if (jointValidator == null || jointValidator(name))
                {
                    if (IsLeftSide(name) && string.IsNullOrEmpty(leftJoint))
                    {
                        leftJoint = child;
                    }
                    else if (IsRightSide(name) && string.IsNullOrEmpty(rightJoint))
                    {
                        rightJoint = child;
                    }
                }
            }

            return (leftJoint, rightJoint);
        }

        private static (string leftUpperLeg, string rightUpperLeg) FindUpperLegs(string hipsJoint,
            Dictionary<string, List<string>> hierarchy, string[] jointNames)
        {
            // Find candidates using existing logic
            var result = FindPairedJoints(hipsJoint, hierarchy, jointNames,
                new[] { "leftupleg", "left_upleg", "leftleg", "leftthigh" },
                new[] { "rightupleg", "right_upleg", "rightleg", "rightthigh" },
                IsLegJoint);

            // Find valid upper legs with depth validation
            string validatedLeftUpperLeg = FindValidUpperLegCandidate(result.left, hipsJoint, hierarchy, jointNames, true);
            string validatedRightUpperLeg = FindValidUpperLegCandidate(result.right, hipsJoint, hierarchy, jointNames, false);

            return (validatedLeftUpperLeg, validatedRightUpperLeg);
        }

        private static (string leftAnkle, string rightAnkle) FindAnkles(string leftUpperLeg, string rightUpperLeg,
            Dictionary<string, List<string>> hierarchy, string[] jointNames)
        {
            // First try direct name matching with preference for simpler names
            var leftCandidate = FindBestAnkleByName(jointNames, true);
            var rightCandidate = FindBestAnkleByName(jointNames, false);

            if (!string.IsNullOrEmpty(leftCandidate) && !string.IsNullOrEmpty(rightCandidate))
            {
                return (leftCandidate, rightCandidate);
            }

            // Traverse down leg hierarchy
            string leftAnkle = leftCandidate ?? FindAnkleForLeg(leftUpperLeg, hierarchy, jointNames);
            string rightAnkle = rightCandidate ?? FindAnkleForLeg(rightUpperLeg, hierarchy, jointNames);
            return (leftAnkle, rightAnkle);
        }

        private static bool IsLeftSide(string name)
        {
            return name.Contains("left") || name.Contains("l_") || name.Contains(".l") || name.Contains("_l");
        }

        private static bool IsRightSide(string name)
        {
            return name.Contains("right") || name.Contains("r_") || name.Contains(".r") || name.Contains("_r");
        }

        /// <summary>
        /// Generic method to find the best joint by name with configurable priorities and penalties
        /// </summary>
        private static string FindBestJointByName(string[] jointNames, bool isLeft,
            string[] includePatterns,
            (string pattern, int priority)[] priorityPatterns,
            string[] penalizedSuffixes)
        {
            var candidates = new List<string>();

            // Collect all potential candidates
            foreach (var joint in jointNames)
            {
                var name = joint.ToLower();
                bool matchesSide = isLeft ? IsLeftSide(name) : IsRightSide(name);

                if (matchesSide && includePatterns.Any(pattern => name.Contains(pattern)))
                {
                    candidates.Add(joint);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // Sort by priority patterns, exact matches, length, and penalized suffixes
            var sortedCandidates = candidates.OrderBy(candidate =>
            {
                var name = candidate.ToLower();
                int priority = 0;

                // Apply priority patterns
                foreach (var (pattern, patternPriority) in priorityPatterns)
                {
                    if (name.Contains(pattern))
                    {
                        priority += patternPriority;
                        break; // Only apply the first matching priority pattern
                    }
                }

                // Prefer exact matches
                string sidePrefix = isLeft ? "left" : "right";
                foreach (var pattern in includePatterns)
                {
                    if (name == sidePrefix + pattern)
                    {
                        priority -= 1000;
                        break;
                    }
                }

                // Prefer shorter names
                priority += name.Length;

                // Penalize certain suffixes
                foreach (var suffix in penalizedSuffixes)
                {
                    if (name.Contains(suffix))
                    {
                        priority += 500;
                        break;
                    }
                }

                return priority;
            }).ToList();

            return sortedCandidates[0];
        }

        private static string FindBestAnkleByName(string[] jointNames, bool isLeft)
        {
            return FindBestJointByName(jointNames, isLeft,
                new[] { "foot", "ankle" },
                new[] { ("ankle", -2000), ("foot", -1500) },
                new[] { "ball", "toe", "heel", "twist", "end", "tip", "roll", "bend", "ctrl", "control" });
        }

        /// <summary>
        /// Generic method to traverse hierarchy and find joints matching target patterns
        /// </summary>
        private static string TraverseHierarchyForJoint(string startJoint, Dictionary<string, List<string>> hierarchy,
            string[] targetPatterns, string[] continuePatterns = null, string[] penalizedSuffixes = null)
        {
            if (string.IsNullOrEmpty(startJoint))
            {
                return null;
            }

            var candidates = new List<string>();
            var current = startJoint;
            var visited = new HashSet<string>();

            while (!string.IsNullOrEmpty(current) && !visited.Contains(current))
            {
                visited.Add(current);

                if (hierarchy.TryGetValue(current, out var children))
                {
                    foreach (var child in children)
                    {
                        var name = child.ToLower();

                        // Check if this child matches target patterns
                        if (targetPatterns.Any(pattern => name.Contains(pattern)))
                        {
                            candidates.Add(child);
                        }

                        // Continue searching deeper if child matches continue patterns
                        if (continuePatterns != null && continuePatterns.Any(pattern => name.Contains(pattern)))
                        {
                            var deeperResult = TraverseHierarchyForJoint(child, hierarchy, targetPatterns,
                                continuePatterns, penalizedSuffixes);
                            if (!string.IsNullOrEmpty(deeperResult))
                            {
                                candidates.Add(deeperResult);
                            }
                        }
                    }
                }

                // Move to first child if no specific part found
                current = hierarchy.ContainsKey(current) && hierarchy[current].Count > 0 ? hierarchy[current][0] : null;
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // Sort candidates by priority
            var sortedCandidates = candidates.OrderBy(candidate =>
            {
                var name = candidate.ToLower();
                int priority = name.Length; // Prefer shorter names

                // Apply penalties
                if (penalizedSuffixes != null)
                {
                    foreach (var suffix in penalizedSuffixes)
                    {
                        if (name.Contains(suffix))
                        {
                            priority += 500;
                            break;
                        }
                    }
                }

                return priority;
            }).ToList();

            return sortedCandidates[0];
        }

        private static string FindAnkleForLeg(string upperLeg, Dictionary<string, List<string>> hierarchy,
            string[] jointNames)
        {
            return TraverseHierarchyForJoint(upperLeg, hierarchy,
                new[] { "foot", "ankle" },
                new[] { "leg", "calf", "shin" },
                new[] { "twist", "end", "tip", "roll", "bend", "ctrl", "control" });
        }

        private static (string leftShoulder, string rightShoulder) FindShoulders(string chestJoint,
            Dictionary<string, List<string>> hierarchy, string[] jointNames)
        {
            return FindPairedJoints(chestJoint, hierarchy, jointNames,
                new[] { "leftshoulder", "left_shoulder", "leftclavicle" },
                new[] { "rightshoulder", "right_shoulder", "rightclavicle" },
                name => name.Contains("shoulder") || name.Contains("clavicle"));
        }

        private static (string leftUpperArm, string rightUpperArm) FindUpperArms(string leftShoulder,
            string rightShoulder, string chestJoint, Dictionary<string, List<string>> hierarchy, string[] jointNames)
        {
            // Find valid upper arms with depth validation, trying multiple approaches
            string validatedLeftUpperArm = FindValidUpperArmCandidate(leftShoulder, chestJoint, hierarchy, jointNames, true);
            string validatedRightUpperArm = FindValidUpperArmCandidate(rightShoulder, chestJoint, hierarchy, jointNames, false);

            return (validatedLeftUpperArm, validatedRightUpperArm);
        }

        private static string FindBestUpperArmByName(string[] jointNames, bool isLeft)
        {
            var candidates = new List<string>();

            // Collect all potential upper arm candidates
            foreach (var joint in jointNames)
            {
                var name = joint.ToLower();
                bool matchesSide = isLeft ? IsLeftSide(name) : IsRightSide(name);

                if (matchesSide && ((name.Contains("arm") && !name.Contains("forearm") && !name.Contains("lower")) ||
                                    name.Contains("shoulder")))
                {
                    candidates.Add(joint);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // Check if we have both arm and shoulder candidates
            var armCandidates = candidates.Where(c => c.ToLower().Contains("arm")).ToList();
            var shoulderCandidates = candidates
                .Where(c => c.ToLower().Contains("shoulder") && !c.ToLower().Contains("arm")).ToList();

            // If we have arm candidates, prefer them over shoulder-only candidates
            // This handles cases where shoulder is a separate joint from the actual upper arm
            if (armCandidates.Count > 0)
            {
                candidates = armCandidates;
            }

            // Sort by: 1) "UpperArm" first, 2) "ArmUpper" second, 3) Generic "Arm", 4) "Shoulder", 5) Shorter names, 6) Penalize certain suffixes
            var sortedCandidates = candidates.OrderBy(candidate =>
            {
                var name = candidate.ToLower();
                int priority = 0;

                // Prefer "upperarm" over other options
                if (name.Contains("upperarm"))
                {
                    priority -= 2000;
                }
                // Prefer "armupper"
                else if (name.Contains("armupper"))
                {
                    priority -= 1500;
                }
                // Prefer generic "arm" over "shoulder" when both are available
                else if (name.Contains("arm") && !name.Contains("upper"))
                {
                    priority -= 1000;
                }
                // Use "shoulder" as fallback when no arm joints are available
                else if (name.Contains("shoulder"))
                {
                    priority -= 500;
                }

                // Prefer shorter names
                priority += name.Length;

                // Penalize names with certain suffixes that indicate they're not the main joint
                var penalizedSuffixes = new[] { "twist", "end", "tip", "roll", "bend", "ctrl", "control" };
                foreach (var suffix in penalizedSuffixes)
                {
                    if (name.Contains(suffix))
                        priority += 500;
                }

                return priority;
            }).ToList();

            return sortedCandidates[0];
        }

        private static (string leftArm, string rightArm) FindArmsFromChest(string chestJoint,
            Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(chestJoint) || !hierarchy.ContainsKey(chestJoint))
            {
                return (null, null);
            }

            string leftArm = null;
            string rightArm = null;

            foreach (var child in hierarchy[chestJoint])
            {
                var name = child.ToLower();
                if (name.Contains("arm") && !name.Contains("forearm") && !name.Contains("lower"))
                {
                    if (IsLeftSide(name))
                    {
                        leftArm = child;
                    }
                    else if (IsRightSide(name))
                    {
                        rightArm = child;
                    }
                }
            }

            return (leftArm, rightArm);
        }

        private static string FindUpperArmForShoulder(string shoulder, Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(shoulder) || !hierarchy.TryGetValue(shoulder, out var children))
            {
                return null;
            }

            foreach (var child in children)
            {
                var name = child.ToLower();
                if (name.Contains("arm") && !name.Contains("forearm") && !name.Contains("lower"))
                {
                    return child;
                }
            }

            return null;
        }

        private static string FindBestWristByName(string[] jointNames, bool isLeft)
        {
            return FindBestJointByName(jointNames, isLeft,
                new[] { "hand", "wrist" },
                new[] { ("wrist", -1500), ("hand", -1000) },
                new[] { "twist", "end", "tip", "roll", "bend", "ctrl", "control" });
        }

        private static (string leftWrist, string rightWrist) FindWrists(string leftUpperArm, string rightUpperArm,
            Dictionary<string, List<string>> hierarchy, string[] jointNames)
        {
            // First try direct name matching with preference for simpler names
            var leftCandidate = FindBestWristByName(jointNames, true);
            var rightCandidate = FindBestWristByName(jointNames, false);

            if (!string.IsNullOrEmpty(leftCandidate) && !string.IsNullOrEmpty(rightCandidate))
            {
                return (leftCandidate, rightCandidate);
            }

            // Traverse down arm hierarchy
            string leftWrist = leftCandidate ?? FindWristForArm(leftUpperArm, hierarchy);
            string rightWrist = rightCandidate ?? FindWristForArm(rightUpperArm, hierarchy);
            return (leftWrist, rightWrist);
        }

        private static string FindWristForArm(string upperArm, Dictionary<string, List<string>> hierarchy)
        {
            // First try the generic traversal method
            var result = TraverseHierarchyForJoint(upperArm, hierarchy,
                new[] { "hand", "wrist" },
                new[] { "forearm", "elbow", "arm" },
                new[] { "twist", "end", "tip", "roll", "bend", "ctrl", "control" });

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Fallback: check for structural hand characteristics
            return TraverseHierarchyForStructuralHand(upperArm, hierarchy);
        }

        private static string TraverseHierarchyForStructuralHand(string startJoint,
            Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(startJoint))
            {
                return null;
            }

            var current = startJoint;
            var visited = new HashSet<string>();

            while (!string.IsNullOrEmpty(current) && !visited.Contains(current))
            {
                visited.Add(current);

                if (hierarchy.TryGetValue(current, out var children))
                {
                    foreach (var child in children)
                    {
                        // Check if this joint has structural characteristics of a hand/wrist
                        if (HasHandStructure(child, hierarchy))
                        {
                            return child;
                        }
                    }
                }

                // Move to first child
                current = hierarchy.ContainsKey(current) && hierarchy[current].Count > 0 ? hierarchy[current][0] : null;
            }

            return null;
        }

        private static bool HasHandStructure(string joint, Dictionary<string, List<string>> hierarchy)
        {
            if (!hierarchy.TryGetValue(joint, out var children))
            {
                return false;
            }

            // A hand/wrist should have multiple children (fingers)
            if (children.Count < 3)
            {
                return false;
            }

            // Count how many children also have children (finger segments)
            int childrenWithChildren = 0;
            foreach (var child in children)
            {
                if (hierarchy.ContainsKey(child) && hierarchy[child].Count > 0)
                {
                    childrenWithChildren++;
                }
            }

            // At least 3 fingers should have segments
            return childrenWithChildren >= 3;
        }

        /// <summary>
        /// Calculate the maximum depth of children for a given joint
        /// </summary>
        /// <param name="joint">The joint to calculate depth for</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <returns>Maximum depth of children (0 if no children, 1 if direct children only, etc.)</returns>
        private static int CalculateChildDepth(string joint, Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(joint) || !hierarchy.TryGetValue(joint, out var children) || children.Count == 0)
            {
                return 0;
            }

            int maxDepth = 0;
            foreach (var child in children)
            {
                int childDepth = CalculateChildDepth(child, hierarchy);
                maxDepth = Math.Max(maxDepth, childDepth + 1);
            }

            return maxDepth;
        }

        /// <summary>
        /// Validate that an upper leg joint has at least 2 levels of children (e.g., upper leg > lower leg > foot)
        /// </summary>
        /// <param name="upperLeg">The upper leg joint to validate</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <returns>True if the upper leg has sufficient child depth, false otherwise</returns>
        private static bool ValidateUpperLegDepth(string upperLeg, Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(upperLeg))
            {
                return false;
            }

            int depth = CalculateChildDepth(upperLeg, hierarchy);
            return depth >= 2;
        }

        /// <summary>
        /// Validate that an upper arm joint has at least 2 levels of children (e.g., upper arm > forearm > hand)
        /// </summary>
        /// <param name="upperArm">The upper arm joint to validate</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <returns>True if the upper arm has sufficient child depth, false otherwise</returns>
        private static bool ValidateUpperArmDepth(string upperArm, Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(upperArm))
            {
                return false;
            }

            int depth = CalculateChildDepth(upperArm, hierarchy);
            return depth >= 2;
        }

        /// <summary>
        /// Validate that a spine joint has at least 2 levels of children (e.g., spine > chest > neck)
        /// </summary>
        /// <param name="spine">The spine joint to validate</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <returns>True if the spine has sufficient child depth, false otherwise</returns>
        private static bool ValidateSpineDepth(string spine, Dictionary<string, List<string>> hierarchy)
        {
            if (string.IsNullOrEmpty(spine))
            {
                return false;
            }

            int depth = CalculateChildDepth(spine, hierarchy);
            return depth >= 2;
        }

        /// <summary>
        /// Find a valid upper leg candidate that meets depth requirements, trying multiple approaches
        /// </summary>
        /// <param name="primaryCandidate">The primary candidate from initial search</param>
        /// <param name="hipsJoint">The hips joint to search from</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <param name="jointNames">All joint names</param>
        /// <param name="isLeft">Whether to search for left or right leg</param>
        /// <returns>Valid upper leg joint or null if none found</returns>
        private static string FindValidUpperLegCandidate(string primaryCandidate, string hipsJoint,
            Dictionary<string, List<string>> hierarchy, string[] jointNames, bool isLeft)
        {
            // First check if primary candidate is valid
            if (ValidateUpperLegDepth(primaryCandidate, hierarchy))
            {
                return primaryCandidate;
            }

            // Collect all potential leg candidates from hips children
            var candidates = new List<string>();

            if (!string.IsNullOrEmpty(hipsJoint) && hierarchy.ContainsKey(hipsJoint))
            {
                foreach (var child in hierarchy[hipsJoint])
                {
                    var name = child.ToLower();
                    bool matchesSide = isLeft ? IsLeftSide(name) : IsRightSide(name);

                    if (matchesSide && IsLegJoint(name))
                    {
                        candidates.Add(child);
                    }
                }
            }

            // Also try direct name matching for additional candidates
            var patterns = isLeft ?
                new[] { "leftupleg", "left_upleg", "leftleg", "leftthigh" } :
                new[] { "rightupleg", "right_upleg", "rightleg", "rightthigh" };

            foreach (var pattern in patterns)
            {
                var candidate = FindJointByNamePatterns(jointNames, new[] { pattern });
                if (!string.IsNullOrEmpty(candidate) && !candidates.Contains(candidate))
                {
                    candidates.Add(candidate);
                }
            }

            // Sort candidates by priority and find first valid one
            var sortedCandidates = candidates.OrderBy(candidate =>
            {
                var name = candidate.ToLower();
                int priority = name.Length; // Prefer shorter names

                // Prefer specific patterns
                if (name.Contains("upleg") || name.Contains("upperleg"))
                    priority -= 1000;
                else if (name.Contains("thigh"))
                    priority -= 500;

                return priority;
            }).ToList();

            // Return first candidate that meets depth requirement
            foreach (var candidate in sortedCandidates)
            {
                if (ValidateUpperLegDepth(candidate, hierarchy))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a valid upper arm candidate that meets depth requirements, trying multiple approaches
        /// </summary>
        /// <param name="shoulder">The shoulder joint to search from</param>
        /// <param name="chestJoint">The chest joint to search from</param>
        /// <param name="hierarchy">The joint hierarchy</param>
        /// <param name="jointNames">All joint names</param>
        /// <param name="isLeft">Whether to search for left or right arm</param>
        /// <returns>Valid upper arm joint or null if none found</returns>
        private static string FindValidUpperArmCandidate(string shoulder, string chestJoint,
            Dictionary<string, List<string>> hierarchy, string[] jointNames, bool isLeft)
        {
            var candidates = new List<string>();

            // Try direct name matching first
            var nameCandidate = FindBestUpperArmByName(jointNames, isLeft);
            if (!string.IsNullOrEmpty(nameCandidate))
            {
                candidates.Add(nameCandidate);
            }

            // Try finding from shoulder
            var shoulderCandidate = FindUpperArmForShoulder(shoulder, hierarchy);
            if (!string.IsNullOrEmpty(shoulderCandidate) && !candidates.Contains(shoulderCandidate))
            {
                candidates.Add(shoulderCandidate);
            }

            // Try finding from chest
            var armsFromChest = FindArmsFromChest(chestJoint, hierarchy);
            var chestCandidate = isLeft ? armsFromChest.leftArm : armsFromChest.rightArm;
            if (!string.IsNullOrEmpty(chestCandidate) && !candidates.Contains(chestCandidate))
            {
                candidates.Add(chestCandidate);
            }

            // Collect additional candidates from chest children
            if (!string.IsNullOrEmpty(chestJoint) && hierarchy.ContainsKey(chestJoint))
            {
                foreach (var child in hierarchy[chestJoint])
                {
                    var name = child.ToLower();
                    bool matchesSide = isLeft ? IsLeftSide(name) : IsRightSide(name);

                    if (matchesSide && (name.Contains("arm") || name.Contains("shoulder")) && !candidates.Contains(child))
                    {
                        candidates.Add(child);
                    }
                }
            }

            // Sort candidates by priority
            var sortedCandidates = candidates.OrderBy(candidate =>
            {
                var name = candidate.ToLower();
                int priority = name.Length; // Prefer shorter names

                // Prefer specific patterns
                if (name.Contains("upperarm"))
                    priority -= 2000;
                else if (name.Contains("armupper"))
                    priority -= 1500;
                else if (name.Contains("arm") && !name.Contains("forearm") && !name.Contains("lower"))
                    priority -= 1000;
                else if (name.Contains("shoulder"))
                    priority -= 500;

                return priority;
            }).ToList();

            // Return first candidate that meets depth requirement
            foreach (var candidate in sortedCandidates)
            {
                if (ValidateUpperArmDepth(candidate, hierarchy))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
