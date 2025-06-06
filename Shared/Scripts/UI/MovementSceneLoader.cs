// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Load different scenes.
    /// Copied from Meta XR Interaction SDK OVR Samples.
    /// </summary>
    public class MovementSceneLoader : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when loading a scene.
        /// </summary>
        public Action<string> WhenLoadingScene = delegate { };

        /// <summary>
        /// Delegate for when a scene is loaded.
        /// </summary>
        public Action<string> WhenSceneLoaded = delegate { };

        [SerializeField]
        private string _editorSceneName;

        [SerializeField, InspectorButton("LoadEditorScene")]
        private bool _loadEditorScene;

        private bool _loading = false;
        private int _waitingCount = 0;

        /// <summary>
        /// Loads a scene by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void Load(string sceneName)
        {
            if (SceneUtility.GetBuildIndexByScenePath(sceneName) == -1)
            {
                Debug.LogError($"Could not load {sceneName}, did you build with it?");
                return;
            }

            if (_loading)
            {
                return;
            }
            _loading = true;

            // make sure we wait for all parties concerned to let us know they're ready to load
            _waitingCount = WhenLoadingScene.GetInvocationList().Length - 1;  // remove the count for the blank delegate
            if (_waitingCount == 0)
            {
                // if nobody else cares just set the preload to go directly to the loading of the scene
                HandleReadyToLoad(sceneName);
            }
            else
            {
                WhenLoadingScene.Invoke(sceneName);
            }
        }

        /// <summary>
        /// Handles ready to load. This should be called after handling any pre-load tasks (e.g. fade to white)
        /// by anyone who registered with WhenLoadingScene in order for the loading to proceed
        /// </summary>
        /// <param name="sceneName"></param>
        public void HandleReadyToLoad(string sceneName)
        {
            _waitingCount--;
            if (_waitingCount <= 0)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (asyncLoad is { isDone: false })
            {
                yield return null;
            }

            WhenSceneLoaded.Invoke(sceneName);
        }

        private void LoadEditorScene()
        {
            Load(_editorSceneName);
        }
    }
}
