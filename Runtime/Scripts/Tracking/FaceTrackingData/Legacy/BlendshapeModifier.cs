// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Tracking.Deprecated
{
    /// <summary>
    /// Calculates the modified blendshape weight for a facial expression.
    /// </summary>
    public class BlendshapeModifier : MonoBehaviour
    {
        /// <summary>
        /// The modifier data for a specific set of facial expressions.
        /// </summary>
        [System.Serializable]
        public class FaceExpressionModifier
        {
            /// <summary>
            /// The facial expressions that will be modified.
            /// </summary>
            [Tooltip(BlendshapeModifierTooltips.FaceExpressionModifier.FaceExpressions)]
            public OVRFaceExpressions.FaceExpression[] FaceExpressions = new OVRFaceExpressions.FaceExpression[2];

            /// <summary>
            /// The minimum clamped blendshape weight for this set of facial expressions.
            /// </summary>
            [Range(0.0f, 2.0f)]
            [Tooltip(BlendshapeModifierTooltips.FaceExpressionModifier.MinValue)]
            public float MinValue = 0.0f;

            /// <summary>
            /// The maximum clamped blendshape weight for this set of facial expressions.
            /// </summary>
            [Range(0.0f, 2.0f)]
            [Tooltip(BlendshapeModifierTooltips.FaceExpressionModifier.MaxValue)]
            public float MaxValue = 1.0f;

            /// <summary>
            /// The blendshape weight multiplier for this set of facial expressions.
            /// </summary>
            [Range(0.0f, 2.0f)]
            [Tooltip(BlendshapeModifierTooltips.FaceExpressionModifier.Multiplier)]
            public float Multiplier = 1.0f;
        }

        /// <summary>
        /// Container class used for json serialization of face expression modifiers.
        /// </summary>
        [System.Serializable]
        private class FaceExpressionModifierArray
        {
            /// <summary>
            /// The array of serialized face expression modifiers.
            /// </summary>
            public FaceExpressionModifier[] FaceExpressionModifiers;

            /// <summary>
            /// Constructor that assigns FaceExpressionModifiers.
            /// </summary>
            /// <param name="faceExpressionModifiers">The array to initialize with.</param>
            public FaceExpressionModifierArray(FaceExpressionModifier[] faceExpressionModifiers)
            {
                FaceExpressionModifiers = faceExpressionModifiers;
            }
        }

        /// <inheritdoc cref="_faceExpressionModifiers"/>
        public IReadOnlyCollection<FaceExpressionModifier> Modifiers => _faceExpressionModifiers;

        /// <inheritdoc cref="_faceExpressionModifierMap"/>
        public IReadOnlyDictionary<OVRFaceExpressions.FaceExpression, FaceExpressionModifier>
            FaceExpressionModifierMap => _faceExpressionModifierMap;

        /// <summary>
        /// The array of facial expression modifier data to be used.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeModifierTooltips.FaceExpressionsModifiers)]
        protected FaceExpressionModifier[] _faceExpressionModifiers;

        /// <summary>
        /// Optional text asset containing the array of face expression modifier data to be used.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(BlendshapeModifierTooltips.DefaultBlendshapeModifierPreset)]
        protected TextAsset _defaultBlendshapeModifierPreset;

        /// <summary>
        /// Global blendshape multiplier.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeModifierTooltips.GlobalMultiplier)]
        protected float _globalMultiplier = 1.0f;

        /// <summary>
        /// Global blendshape clamp min.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeModifierTooltips.GlobalMin)]
        protected float _globalClampMin = 0.0f;

        /// <summary>
        /// Global blendshape clamp max.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeModifierTooltips.GlobalMax)]
        protected float _globalClampMax = 2.0f;

        /// <summary>
        /// Apply global clamping to non-mapped values.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeModifierTooltips.ApplyGlobalClampingNonMapped)]
        protected bool _applyGlobalClampingNonMapped = false;

        /// <summary>
        /// The dictionary mapping a OVRFaceExpressions.FaceExpression to a FaceExpressionModifier.
        /// </summary>
        private Dictionary<OVRFaceExpressions.FaceExpression, FaceExpressionModifier> _faceExpressionModifierMap;

        private void Awake()
        {
            _faceExpressionModifierMap = new Dictionary<OVRFaceExpressions.FaceExpression, FaceExpressionModifier>();
            SetupBlendshapeModifierMapping();
            if (_defaultBlendshapeModifierPreset != null)
            {
                LoadPreset(_defaultBlendshapeModifierPreset.text);
            }
        }

        private void SetupBlendshapeModifierMapping()
        {
            _faceExpressionModifierMap.Clear();
            foreach (var faceExpressionModifier in _faceExpressionModifiers)
            {
                Assert.IsTrue(faceExpressionModifier.FaceExpressions.Length > 0);
                foreach (var faceExpression in faceExpressionModifier.FaceExpressions)
                {
                    Assert.IsFalse(_faceExpressionModifierMap.ContainsKey(faceExpression));
                    _faceExpressionModifierMap.Add(faceExpression, faceExpressionModifier);
                }
            }
        }

        private void AddFaceExpressionModifier(OVRFaceExpressions.FaceExpression faceExpression)
        {
            Debug.LogWarning($"Missing modifier setup for {faceExpression}, creating a modifier.");
            var faceExpressionModifier = new FaceExpressionModifier
            {
                FaceExpressions = new OVRFaceExpressions.FaceExpression[2],
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Multiplier = 1.0f
            };
            _faceExpressionModifierMap.Add(faceExpression, faceExpressionModifier);
        }

        private string SerializeToJson()
        {
            foreach (var faceExpressionModifier in _faceExpressionModifiers)
            {
                var currentModifier = _faceExpressionModifierMap[faceExpressionModifier.FaceExpressions[0]];
                faceExpressionModifier.MinValue = currentModifier.MinValue;
                faceExpressionModifier.MaxValue = currentModifier.MaxValue;
                faceExpressionModifier.Multiplier = currentModifier.Multiplier;
            }
            var serializedFaceExpressionModifiers =
                new FaceExpressionModifierArray(faceExpressionModifiers: _faceExpressionModifiers);
            return JsonUtility.ToJson(serializedFaceExpressionModifiers, true);
        }

        /// <summary>
        /// Returns the modified weight for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <param name="currentWeight">The unmodified weight of the facial expression.</param>
        /// <returns></returns>
        public float GetModifiedWeight(OVRFaceExpressions.FaceExpression faceExpression, float currentWeight)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                if (_applyGlobalClampingNonMapped)
                {
                    currentWeight = Mathf.Clamp(currentWeight * _globalMultiplier,
                        _globalClampMin, _globalClampMax);
                }

                return currentWeight;
            }
            var faceExpressionModifier = _faceExpressionModifierMap[faceExpression];
            float modifiedWeight = Mathf.Clamp(Mathf.Clamp(currentWeight * faceExpressionModifier.Multiplier,
                faceExpressionModifier.MinValue, faceExpressionModifier.MaxValue) * _globalMultiplier, _globalClampMin, _globalClampMax);
            return modifiedWeight;
        }

        /// <summary>
        /// Update the minimum clamped value for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <param name="val">The updated minimum value for the facial expression.</param>
        public void UpdateMinValue(OVRFaceExpressions.FaceExpression faceExpression, float val)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            _faceExpressionModifierMap[faceExpression].MinValue = val;
            if (faceExpression == OVRFaceExpressions.FaceExpression.Max)
            {
                _globalClampMin = val;
            }
        }

        /// <summary>
        /// Update the maximum clamped value for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <param name="val">The updated maximum value for the facial expression.</param>
        public void UpdateMaxValue(OVRFaceExpressions.FaceExpression faceExpression, float val)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            _faceExpressionModifierMap[faceExpression].MaxValue = val;
            if (faceExpression == OVRFaceExpressions.FaceExpression.Max)
            {
                _globalClampMax = val;
            }
        }

        /// <summary>
        /// Update the multiplier value for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <param name="val">The updated multiplier value for the facial expression.</param>
        public void UpdateMultiplierValue(OVRFaceExpressions.FaceExpression faceExpression, float val)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            _faceExpressionModifierMap[faceExpression].Multiplier = val;
            if (faceExpression == OVRFaceExpressions.FaceExpression.Max)
            {
                _globalMultiplier = val;
            }
        }

        /// <summary>
        /// Returns the multiplier value for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <returns>Multiplier modifier for a facial expression.</returns>
        public float GetMultiplierValue(OVRFaceExpressions.FaceExpression faceExpression)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            return _faceExpressionModifierMap[faceExpression].Multiplier;
        }

        /// <summary>
        /// Returns the minimum clamped value for a facial expression.
        /// </summary>
        /// <param name="faceExpression">The facial expression.</param>
        /// <returns>Minimum clamped value for a facial expression.</returns>
        public float GetMinValue(OVRFaceExpressions.FaceExpression faceExpression)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            return _faceExpressionModifierMap[faceExpression].MinValue;
        }

        /// <summary>
        /// Returns the maximum clamped value for a facial expression.
        /// </summary>
        /// <param name="faceExpression"></param>
        /// <returns>Maximum clamped value for a facial expression.</returns>
        public float GetMaxValue(OVRFaceExpressions.FaceExpression faceExpression)
        {
            if (!_faceExpressionModifierMap.ContainsKey(faceExpression))
            {
                AddFaceExpressionModifier(faceExpression);
            }
            return _faceExpressionModifierMap[faceExpression].MaxValue;
        }

        /// <summary>
        /// Saves the current facial expression modifiers to a timestamped json file.
        /// </summary>
        public void SavePreset()
        {
            var saveJson = SerializeToJson();
            System.IO.File.WriteAllText($"{Application.persistentDataPath}/{System.DateTime.Now:yyyyMMddTHHmmss}.json", saveJson);
        }

        /// <summary>
        /// Loads the facial expression modifiers from text.
        /// </summary>
        /// <param name="presetJson">The json containing the serialized facial expression modifiers.</param>
        public void LoadPreset(string presetJson)
        {
            var faceExpressionModifierArray = JsonUtility.FromJson<FaceExpressionModifierArray>(presetJson);
            _faceExpressionModifiers = faceExpressionModifierArray.FaceExpressionModifiers;
            SetupBlendshapeModifierMapping();
        }
    }
}
