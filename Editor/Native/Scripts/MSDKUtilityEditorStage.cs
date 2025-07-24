// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Stage for the Movement SDK utility editor.
    /// </summary>
    public class MSDKUtilityEditorStage : PreviewSceneStage
    {
        /// <summary>
        /// Scene view for alignment in the Movement SDK utility editor.
        /// </summary>
        public class AlignmentSceneView : SceneView
        {
            /// <summary>
            /// Creates an instance of the alignment scene view.
            /// </summary>
            /// <param name="relatedStage">The related preview scene stage.</param>
            /// <returns>The created alignment scene view.</returns>
            public static AlignmentSceneView CreateInstance(PreviewSceneStage relatedStage)
            {
                var instance = ScriptableObject.CreateInstance<AlignmentSceneView>();
                instance.customScene = relatedStage.scene;
                return instance;
            }
        }

        /// <summary>
        /// Gets or sets the original asset path.
        /// </summary>
        public string OriginalAssetPath
        {
            get => _originalAssetPath;
            set => _originalAssetPath = value;
        }

        /// <summary>
        /// Gets or sets the editor window toolbar.
        /// </summary>
        public MSDKUtilityEditorWindow Toolbar { get; set; }

        [SerializeField]
        private string _originalAssetPath;

        [SerializeField]
        private GameObject _instantiatedCharacter = null;

        [SerializeField]
        private AlignmentSceneView _alignmentSceneView = null;

        /// <summary>
        /// Creates an instance of the editor stage.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <param name="customDataSourceName">Optional custom data source name.</param>
        /// <returns>The created editor stage.</returns>
        public static MSDKUtilityEditorStage CreateInstanceOfStage(string assetPath, string customDataSourceName = null)
        {
            var newAlignmentStage = CreateInstance<MSDKUtilityEditorStage>();
            newAlignmentStage.OriginalAssetPath = assetPath;

            var nativeUtilityConfigToolbar = MSDKUtilityEditorWindow.CreateDockedInspector();
            newAlignmentStage.Toolbar = nativeUtilityConfigToolbar;
            return newAlignmentStage;
        }

        /// <inheritdoc cref="PreviewSceneStage.OnOpenStage"/>
        protected override bool OnOpenStage()
        {
            if (!base.OnOpenStage())
            {
                return false;
            }

            bool sceneValid = scene.IsValid();
            // should be false, because scene doesn't exist yet.
            // if true, that means one is being loaded.
            if (sceneValid)
            {
                DestroyCurrentScene(scene);
                // check again after destroying the current scene.
                if (scene.IsValid())
                {
                    Debug.LogError("Could not clear previous stage. Bailing.");
                    return false;
                }
            }

            bool sceneCreated;
            (scene, sceneCreated) = CreateScene();

            if (!sceneCreated)
            {
                return false;
            }

            // create a scene view so that OnFirstTimeOpenStageInSceneView is called.
            _alignmentSceneView = AlignmentSceneView.CreateInstance(this);

            return true;
        }

        private (Scene, bool) CreateScene()
        {
            Scene alignmentScene = EditorSceneManager.NewPreviewScene();

            var assetNameSansExtension = Path.GetFileNameWithoutExtension(_originalAssetPath);
            var assetObject = AssetDatabase.LoadAssetAtPath(_originalAssetPath, typeof(GameObject)) as GameObject;

            Toolbar.UtilityConfig.MetadataAssetPath = _originalAssetPath;
            alignmentScene.name = assetNameSansExtension + " Config";
            try
            {
                _instantiatedCharacter = Instantiate(assetObject);
                _instantiatedCharacter.name = _instantiatedCharacter.name.Replace("(Clone)", "");
            }
            catch (Exception e)
            {
                Debug.LogError("Could not move instantiated GameObject into" + $" preview scene. Error: {e.Message}.");
                return (alignmentScene, false);
            }

            var rootJoint = _instantiatedCharacter.transform.GetAllChildren().FirstOrDefault(child =>
                child.childCount > 0 && child.GetComponent<SkinnedMeshRenderer>() == null);
            var importer = AssetImporter.GetAtPath(_originalAssetPath) as ModelImporter;
            var globalScale = importer != null ? importer.globalScale : 1.0f;
            if (importer != null && rootJoint != null && rootJoint.localScale != Vector3.one)
            {
                Debug.LogWarning(
                    "Character joints must be uniform scale for retargeting! Setting root joint scale to uniform scale.");

                if (!Mathf.Approximately(globalScale, rootJoint.localScale.x))
                {
                    var choice = EditorUtility.DisplayDialogComplex("Scaling",
                        "Character has scaling. Retargeting requires all the character joints to have uniform scaling. Update and reimport?",
                        "Yes", "No", "Cancel");
                    if (choice == 0)
                    {
                        var scale = Mathf.Min(rootJoint.localScale.x,
                            Mathf.Min(rootJoint.localScale.y, rootJoint.localScale.z));
                        importer.globalScale = scale;
                        globalScale = scale;
                        importer.SaveAndReimport();
                    }
                    else
                    {
                        rootJoint.localScale = Vector3.one;
                        return (new Scene(), false);
                    }
                }
                _instantiatedCharacter.transform.localScale = Vector3.one * globalScale;
                rootJoint.localScale = Vector3.one;
            }

            SceneManager.MoveGameObjectToScene(_instantiatedCharacter, alignmentScene);
            Toolbar.Previewer.SceneViewCharacter = _instantiatedCharacter;
            Toolbar.PreviewStage = this;
            Toolbar.Init();
            return (alignmentScene, true);
        }

        /// <inheritdoc cref="PreviewSceneStage.OnCloseStage"/>
        protected override void OnCloseStage()
        {
            base.OnCloseStage();
            if (_instantiatedCharacter != null)
            {
                DestroyImmediate(_instantiatedCharacter);
            }

            if (_alignmentSceneView != null)
            {
                DestroyImmediate(_alignmentSceneView);
            }

            if (Toolbar != null)
            {
                Toolbar.Close();
                Toolbar = null;
            }
        }

        /// <summary>
        /// Destroys the current scene.
        /// </summary>
        /// <param name="currentScene">The scene to destroy.</param>
        public static void DestroyCurrentScene(Scene currentScene)
        {
            if (!currentScene.IsValid())
            {
                return;
            }

            // TODO need to record undo somehow, Undo.ClearUndoSceneHandle(scene); is an internal function!
            EditorSceneManager.ClosePreviewScene(currentScene);
        }

        protected override void OnFirstTimeOpenStageInSceneView(SceneView sceneView)
        {
            Selection.activeGameObject = _instantiatedCharacter;

            // Frame in scene view
            bool framedSelected = sceneView.FrameSelected(false, true);
            if (!framedSelected)
            {
                Debug.LogError($"Could not frame {_instantiatedCharacter}.");
            }

            // Setup Scene view state
            sceneView.sceneViewState.showFlares = false;
            sceneView.sceneViewState.alwaysRefresh = false;
            sceneView.sceneViewState.showFog = false;
            sceneView.sceneViewState.showSkybox = false;
            sceneView.sceneViewState.showImageEffects = false;
            sceneView.sceneViewState.showParticleSystems = false;
            sceneView.sceneLighting = false;
        }

        protected override GUIContent CreateHeaderContent()
        {
            var contentName = scene.name;
            GUIContent headerContent = new GUIContent(contentName,
                EditorGUIUtility.IconContent("GameObject Icon").image);
            return headerContent;
        }
    }
}
