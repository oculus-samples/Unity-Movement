// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Custom Editor for <see cref="ARKitFace"/> component.
    /// Created so that the <see cref="ARKitFace"/> component has a proper editor
    /// class that inherits from <see cref="CorrectivesFaceEditor"/>.
    /// </summary>
    [CustomEditor(typeof(ARKitFace))]
    public class ARKitFaceEditor : CorrectivesFaceEditor
    {
    }
}
