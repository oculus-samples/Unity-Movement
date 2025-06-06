// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static Oculus.Movement.FaceTrackingTooltips;
using static OVRFaceExpressions;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Provides weights from face tracking.
    /// </summary>
    public class OVRWeightsProvider : WeightsProvider
    {
        /// <summary>
        /// The face expressions provider to source from.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRWeightsProviderTooltips.OvrFaceExpressions)]
        protected OVRFaceExpressions _ovrFaceExpressions;
        /// <inheritdoc cref="_ovrFaceExpressions"/>
        public OVRFaceExpressions OVRFaceExpressionComp
        {
            get => _ovrFaceExpressions;
            set => _ovrFaceExpressions = value;
        }

        float[] _allWeights = null;
        string[] _weightNames = null;

        private void Awake()
        {
            Assert.IsNotNull(_ovrFaceExpressions);
        }

        private void EnsureInitialize()
        {
            if (_allWeights == null)
            {
                _allWeights = new float[(int)FaceExpression.Max];
            }
            if (_weightNames == null)
            {
                _weightNames = new string[(int)FaceExpression.Max];
                for (int i = (int)FaceExpression.BrowLowererL; i < (int)FaceExpression.Max; i++)
                {
                    _weightNames[i] = ((FaceExpression)i).ToString();
                }
            }
        }

        /// <inheritdoc />
        public override bool IsValid =>
            _ovrFaceExpressions.enabled &&
            _ovrFaceExpressions.FaceTrackingEnabled &&
            _ovrFaceExpressions.ValidExpressions;

        /// <inheritdoc />
        public override float[] GetWeights()
        {
            EnsureInitialize();
            return _allWeights;
        }

        /// <inheritdoc />
        public override string[] GetWeightNames()
        {
            EnsureInitialize();
            return _weightNames;
        }

        private void Update()
        {
            EnsureInitialize();
            if (!IsValid)
            {
                return;
            }
            for (int i = (int)FaceExpression.BrowLowererL; i < (int)FaceExpression.Max; i++)
            {
                _allWeights[i] = _ovrFaceExpressions[(FaceExpression)i];
            }
        }
    }
}
