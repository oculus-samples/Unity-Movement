// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;

namespace Meta.XR.Movement.Editor.Tests
{
    /// <summary>
    /// Unrelated user asset that happens to sit at a model's default MSDK metadata path.
    /// </summary>
    internal class MetadataDecoyAsset : ScriptableObject
    {
        public string Payload;
    }
}
