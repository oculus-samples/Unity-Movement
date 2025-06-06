// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Aligns scene selection icon based on current scene.
    /// </summary>
    public class MovementSceneSelectIcon : MonoBehaviour
    {
        /// <summary>
        /// Information about the icon used to select button.
        /// </summary>
        [Serializable]
        protected class IconPositionInformation
        {
            /// <summary>
            /// Button to highlight.
            /// </summary>
            public Transform ButtonTransform;

            /// <summary>
            /// Scene name to check for.
            /// </summary>
            public string SceneName;

            /// <summary>
            /// Valids fields on class using asserts.
            /// </summary>
            public void Validate()
            {
                Assert.IsNotNull(ButtonTransform);
                Assert.IsFalse(String.IsNullOrEmpty(SceneName));
            }
        }

        /// <summary>
        /// Icon positions array.
        /// </summary>
        [SerializeField]
        protected IconPositionInformation[] _iconInformationArray;

        /// <summary>
        /// Icon transform to affect.
        /// </summary>
        [SerializeField]
        protected Transform _iconTransform;

        /// <summary>
        /// Offset the icon so that it is centered around the image of each button,
        /// and enforce a z-value to stay above buttons. These values are based
        /// on trial and error.
        /// </summary>
        private float _iconYOffset = 0.0171f;
        private float _iconZValue = -0.04f;

        private void Awake()
        {
            Assert.IsTrue(_iconInformationArray != null &&
                _iconInformationArray.Length > 0);
            foreach (var iconInfo in _iconInformationArray)
            {
                iconInfo.Validate();
            }
        }

        private void Start()
        {
            bool scenePosSet = false;
            foreach (var iconPosInfo in _iconInformationArray)
            {
                if (iconPosInfo.SceneName == SceneManager.GetActiveScene().name)
                {
                    var buttonTransformLocalPosition = iconPosInfo.ButtonTransform.localPosition;
                    _iconTransform.localPosition =
                        new Vector3(buttonTransformLocalPosition.x,
                            buttonTransformLocalPosition.y + _iconYOffset,
                            _iconZValue);
                    scenePosSet = true;
                    break;
                }
            }

            if (!scenePosSet)
            {
                Debug.LogWarning("Scene selection icon's position not set properly.");
            }
        }
    }
}
