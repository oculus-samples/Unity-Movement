// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Aligns scene selection icon based on current scene.
    /// </summary>
    public class SceneSelectIcon : MonoBehaviour
    {
        /// <summary>
        /// Information about the icon position.
        /// </summary>
        [Serializable]
        protected class IconPositionInformation
        {
            /// <summary>
            /// Local position to set.
            /// </summary>
            [Tooltip(SceneSelectIconTooltips.IconPositionInformation.LocalPosition)]
            public Vector3 LocalPosition;

            /// <summary>
            /// Scene name to check for.
            /// </summary>
            [Tooltip(SceneSelectIconTooltips.IconPositionInformation.SceneName)]
            public string SceneName;
        }

        /// <summary>
        /// Icon positions array.
        /// </summary>
        [SerializeField]
        [Tooltip(SceneSelectIconTooltips.IconInformationArray)]
        protected IconPositionInformation[] _iconInformationArray;

        /// <summary>
        /// Icon transform to affect.
        /// </summary>
        [SerializeField]
        [Tooltip(SceneSelectIconTooltips.IconTransform)]
        protected Transform _iconTransform;

        private void Awake()
        {
            Assert.IsTrue(_iconInformationArray != null &&
                _iconInformationArray.Length > 0);
        }

        private void Start()
        {
            bool scenePosSet = false;
            foreach(var iconPosInfo in _iconInformationArray)
            {
                if (iconPosInfo.SceneName == SceneManager.GetActiveScene().name)
                {
                    _iconTransform.localPosition = iconPosInfo.LocalPosition;
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
