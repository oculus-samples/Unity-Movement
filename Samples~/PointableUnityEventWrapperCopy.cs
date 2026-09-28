// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    public class PointableUnityEventWrapperCopy : MonoBehaviour
    {
        [SerializeField]
        private PointableUnityEventWrapper _copyFrom;
        [SerializeField, Interface(typeof(IPointable))]
        private Object _pointable;

        private void Awake()
        {
            var copy = gameObject.AddComponent<PointableUnityEventWrapper>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_copyFrom), copy);
            copy.InjectPointable(_pointable as IPointable);
        }
    }
}
