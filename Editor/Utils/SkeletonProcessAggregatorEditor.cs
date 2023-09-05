// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Oculus.Movement.AnimationRigging
{
    [CustomEditor(typeof(SkeletonProcessAggregator))]
    public class SkeletonProcessAggregatorEditor : OVRSkeletonEditor
    {
        public override void OnInspectorGUI()
        {
            var aggregator = (SkeletonProcessAggregator)target;
            aggregator.EditTimeItemValidateCheck();
            DrawDefaultInspector();
        }
    }
}
