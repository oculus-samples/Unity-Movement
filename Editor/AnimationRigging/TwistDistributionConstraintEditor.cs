// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Custom editor for the twist distribution constraint.
    /// </summary>
    [CustomEditor(typeof(TwistDistributionConstraint)), CanEditMultipleObjects]
    public class TwistDistributionConstraintEditor : Editor
    {
        private readonly string[] _segmentEndBoneNames = new string[]
        {
            "Lower", "Wrist", "Hand", "Ankle", "Foot"
        };

        private readonly string[] _segmentUpBoneNames = new string[]
        {
            "Shoulder", "Hips"
        };

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var constraint = (TwistDistributionConstraint)target;
            ITwistDistributionData twistDistributionData = constraint.data;
            bool shouldShowButton = false;

            if (twistDistributionData.ConstraintSkeleton == null)
            {
                if (GUILayout.Button("Find OVR Skeleton"))
                {
                    Undo.RecordObject(constraint, "Find OVR Skeleton");
                    var skeleton = constraint.GetComponentInParent<OVRCustomSkeleton>();
                    constraint.data.AssignOVRSkeleton(skeleton);
                }
                shouldShowButton = true;
            }

            if (twistDistributionData.SegmentStart != null)
            {
                if (GUILayout.Button("Find Twist Nodes"))
                {
                    Undo.RecordObject(constraint, "Find Twist Nodes");
                    FindTwistNodes(constraint);
                }
                if (twistDistributionData.SegmentUp == null &&
                    twistDistributionData.SegmentEnd == null &&
                    GUILayout.Button("Find Segment Joints"))
                {
                    Undo.RecordObject(constraint, "Find Segment Joints");
                    FindSegments(constraint);
                }
                shouldShowButton = true;
            }

            if (shouldShowButton)
            {
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }
            DrawDefaultInspector();
        }

        private void FindTwistNodes(TwistDistributionConstraint constraint)
        {
            ITwistDistributionData twistDistributionData = constraint.data;
            Transform segmentStart = twistDistributionData.SegmentStart;
            List<Transform> twistNodes = new List<Transform>();
            for (int i = 0; i < segmentStart.childCount; i++)
            {
                var segmentChild = segmentStart.GetChild(i);
                if (segmentChild.name.Contains("Twist"))
                {
                    twistNodes.Add(segmentChild);
                }
            }
            constraint.data.AssignTwistNodes(twistNodes.ToArray());
        }

        private void FindSegments(TwistDistributionConstraint constraint)
        {
            ITwistDistributionData twistDistributionData = constraint.data;
            Transform segmentStart = twistDistributionData.SegmentStart;
            Transform segmentEnd = null;
            for (int i = 0; i < segmentStart.childCount; i++)
            {
                var segmentChild = segmentStart.GetChild(i);
                if (!segmentChild.name.Contains("Twist") &&
                    _segmentEndBoneNames.Any(segmentChild.name.Contains))
                {
                    segmentEnd = segmentChild;
                    break;
                }
            }
            var segmentStartParent = segmentStart.parent;
            Transform segmentUp = _segmentUpBoneNames.Any(segmentStartParent.name.Contains) ?
                segmentStartParent : segmentEnd;
            constraint.data.AssignSegments(segmentStart, segmentEnd, segmentUp);
        }
    }
}
