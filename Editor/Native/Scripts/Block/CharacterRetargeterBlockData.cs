// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    /// <summary>
    /// The character retargeter building block.
    /// </summary>
    public class CharacterRetargeterBlockData : BlockData
    {
        protected override bool UsesPrefab => false;

        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            selectedGameObject = Selection.activeGameObject;
            MSDKUtilityEditor.AddCharacterRetargeter(selectedGameObject);
            return new List<GameObject> { selectedGameObject };
        }
    }
}
