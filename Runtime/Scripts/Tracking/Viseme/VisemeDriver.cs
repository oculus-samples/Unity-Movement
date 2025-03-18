// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRFaceExpressions;

namespace Meta.XR.Movement.FaceTracking
{
    /// <summary>
    /// This component drives the blendshapes from the <see cref="SkinnedMeshRenderer"/>
    /// based on the weights that we we get for each visemes using <see cref="OVRFaceExpressions"/>.
    /// The blendshapes are mapped with an array of Visemes via <see cref="FaceViseme"/>.
    /// The fields are accessible with the help of <see cref="VisemeDriverEditor"/>.
    /// </summary>
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class VisemeDriver : MonoBehaviour
    {
        /// <summary>
        /// This mesh is accessed and set on enable in the <see cref="VisemeDriverEditor"/>
        /// </summary>
        public SkinnedMeshRenderer VisemeMesh
        {
            get { return _mesh; }
            set { _mesh = value; }
        }

        /// <summary>
        /// Checks if visemes are valid and gets the weights received from the visemes.
        /// </summary>
        [SerializeField]
        [Tooltip(VisemeDriverTooltips.OvrFaceExpression)]
        protected OVRFaceExpressions _ovrFaceExpressions;

        /// <summary>
        /// The array is populated based on the number of blendshapes in the <see cref="SkinnedMeshRenderer"/>.
        /// The blendshapes get assigned to the closest <see cref="FaceViseme"/> based on the
        /// blendshape name using <see cref="GetClosestViseme(string)"/>.
        /// </summary>
        [SerializeField]
        [Tooltip(VisemeDriverTooltips.VisemeMapping)]
        protected FaceViseme[] _visemeMapping;

        /// <summary>
        /// The mesh that should contain viseme-compatible blendshapes.
        /// </summary>
        [SerializeField]
        [Tooltip(VisemeDriverTooltips.Mesh)]
        protected SkinnedMeshRenderer _mesh;

        private const string _VISEME_PREFIX = "viseme_";

        private void Awake()
        {
            Assert.IsNotNull(_mesh);
            Assert.IsNotNull(_ovrFaceExpressions);
        }

        private void Update()
        {
            if (_ovrFaceExpressions.AreVisemesValid)
            {
                UpdateVisemes();
            }
        }

        /// <summary>
        /// Map the <see cref="FaceViseme"/> to the blendshapes in the <see cref="SkinnedMeshRenderer"/>
        /// after pressing the "Auto Generate Mapping" button in the <see cref="VisemeDriverEditor"/>.
        /// </summary>
        public void AutoMapBlendshapes()
        {
            _visemeMapping = new FaceViseme[_mesh.sharedMesh.blendShapeCount];

            for (int i = 0; i < _mesh.sharedMesh.blendShapeCount; i++)
            {
                _visemeMapping[i] = GetClosestViseme(_mesh.sharedMesh.GetBlendShapeName(i).ToLower());
            }
        }

        /// <summary>
        /// Clears blendshapes by turning all the <see cref="_visemeMapping"/> to <see cref=" OVRFaceExpressions.FaceViseme.Invalid"/>.
        /// </summary>
        public void ClearBlendshapes()
        {
            if (_mesh == null || _mesh.sharedMesh.blendShapeCount == 0)
            {
                return;
            }

            for (int i = 0; i < _mesh.sharedMesh.blendShapeCount; ++i)
            {
                _visemeMapping[i] = OVRFaceExpressions.FaceViseme.Invalid;
            }
        }

        private FaceViseme GetClosestViseme(string blendshapeName)
        {
            foreach (FaceViseme viseme in Enum.GetValues(typeof(FaceViseme)))
            {
                if (viseme == FaceViseme.Invalid || viseme == FaceViseme.Count)
                {
                    continue;
                }

                string visemeName = viseme.ToString().ToLower();

                if (blendshapeName == visemeName)
                {
                    return viseme;
                }

                string prefixedName = _VISEME_PREFIX + visemeName;

                if (blendshapeName == prefixedName)
                {
                    return viseme;
                }

                char firstChar = visemeName[0];
                prefixedName = _VISEME_PREFIX + firstChar;
                if (blendshapeName == prefixedName)
                {
                    return viseme;
                }

                if (visemeName.Length > 1 && visemeName.Length <= 2)
                {
                    char secondChar = visemeName[1];
                    prefixedName = _VISEME_PREFIX + secondChar;
                    if (blendshapeName == prefixedName)
                    {
                        return viseme;
                    }
                }
            }
            return OVRFaceExpressions.FaceViseme.Invalid;
        }

        private void UpdateVisemes()
        {
            if (_mesh == null || _visemeMapping.Length == 0 || _ovrFaceExpressions == null)
            {
                return;
            }

            for (int i = 0; i < _visemeMapping.Length; i++)
            {
                if (_visemeMapping[i] == FaceViseme.Invalid || _visemeMapping[i] == FaceViseme.Count)
                {
                    continue;
                }

                _ovrFaceExpressions.TryGetFaceViseme(_visemeMapping[i], out float visemeWeight);
                _mesh.SetBlendShapeWeight(i, visemeWeight);
            }
        }
    }
}
