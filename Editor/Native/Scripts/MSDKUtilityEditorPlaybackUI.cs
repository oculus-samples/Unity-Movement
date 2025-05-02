// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// UI element for playback controls in the Movement SDK utility editor.
    /// </summary>
    public class MSDKUtilityEditorPlaybackUI : VisualElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MSDKUtilityEditorPlaybackUI"/> class.
        /// </summary>
        /// <param name="config">The editor window configuration.</param>
        public MSDKUtilityEditorPlaybackUI(MSDKUtilityEditorWindow config) => _config = config;

        /// <summary>
        /// Gets the editor window configuration.
        /// </summary>
        public MSDKUtilityEditorWindow Config => _config;

        private readonly MSDKUtilityEditorWindow _config;

        private readonly Texture2D _playImage =
            EditorGUIUtility.IconContent("Packages/com.meta.xr.sdk.movement/Editor/Native/UI/Play.png")
                .image as Texture2D;

        private readonly Texture2D _stopImage =
            EditorGUIUtility.IconContent("Packages/com.meta.xr.sdk.movement/Editor/Native/UI/Stop.png")
                .image as Texture2D;

        private readonly Texture2D _pauseImage =
            EditorGUIUtility.IconContent("Packages/com.meta.xr.sdk.movement/Editor/Native/UI/Pause.png")
                .image as Texture2D;

        private PlaybackSlider _slider;
        private EditorToolbarButton _playButton;
        private EditorToolbarButton _stopButton;
        private Label _timestamp;

        private const int _buttonSizeHeight = 24;
        private const int _singleLineSpace = 8;

        /// <summary>
        /// Starts the playback.
        /// </summary>
        public void StartPlayback()
        {
            _slider?.StartPlay();
        }

        /// <summary>
        /// Initializes the UI elements.
        /// </summary>
        public void Init()
        {
            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.SpaceBetween
                }
            };
            var middleRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart
                }
            };

            _slider = new PlaybackSlider(_config);
            _slider.Init();
            _timestamp = new Label();
            topRow.Add(_timestamp);
            topRow.Add(_slider);

            _playButton = new EditorToolbarButton(_pauseImage, PlayPause);
            _stopButton = new EditorToolbarButton(_stopImage, Stop);
            _playButton.style.width = _stopButton.style.width = _buttonSizeHeight;
            _playButton.style.height = _stopButton.style.height = _buttonSizeHeight;
            _playButton.style.marginLeft = _playButton.style.marginRight =
                _stopButton.style.marginLeft = _stopButton.style.marginRight = 4;
            _playButton.style.borderLeftWidth = _playButton.style.borderRightWidth =
                _stopButton.style.borderLeftWidth = _stopButton.style.borderRightWidth = 0;
            middleRow.Add(_playButton);
            middleRow.Add(_stopButton);

            Add(topRow);
            Add(middleRow);
            Add(new VisualElement()
            {
                style =
                {
                    height = _singleLineSpace,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart
                }
            });
        }

        /// <summary>
        /// Updates the UI elements.
        /// </summary>
        public void Update()
        {
            _slider.Update();
            _timestamp.text = "Playback Time: " + System.Math.Truncate(_config.FileReader.PlaybackTime * 100f) / 100f;
        }

        private void PlayPause()
        {
            _config.FileReader.SetPauseState(_config.FileReader.IsPlaying);
            _config.FileReader.UserActivelyScrubbing = false;
            _playButton.icon = _config.FileReader.IsPlaying ? _pauseImage : _playImage;
            _playButton.MarkDirtyRepaint();
        }

        private void Stop()
        {
            _config.FileReader.ClosePlaybackFile();
            _slider.StopPlay();
            _playButton.icon = _config.FileReader.IsPlaying ? _pauseImage : _playImage;
            _playButton.MarkDirtyRepaint();
            Clear();
        }
    }

    /// <summary>
    /// Slider control for playback in the Movement SDK utility editor.
    /// </summary>
    public class PlaybackSlider : VisualElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackSlider"/> class.
        /// </summary>
        /// <param name="config">The editor window configuration.</param>
        public PlaybackSlider(MSDKUtilityEditorWindow config) => _config = config;

        private readonly MSDKUtilityEditorWindow _config;
        private Slider _slider;
        private float _seekMax = 1.0f;

        /// <summary>
        /// Initializes the slider.
        /// </summary>
        public void Init()
        {
            _slider = new Slider(0.0f, 1.0f)
            {
                style =
                {
                    height = 24
                }
            };

            Add(_slider);
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            //Workaround to solve issue that slider does not respond to PointerUp/MouseUp events
            _slider.Q("unity-drag-container").RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Updates the slider.
        /// </summary>
        public void Update()
        {
            if (_config != null)
            {
                if (!_config.FileReader.UserActivelyScrubbing)
                {
                    _slider.value = _config.FileReader.SnapshotIndex;
                }
                else
                {
                    if (_slider.value != _config.FileReader.SnapshotIndex)
                    {
                        Seek((int)_slider.value);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the number of snapshots for the slider.
        /// </summary>
        public void InitNumSnapshots()
        {
            _seekMax = _config.FileReader.NumSnapshots - 1;
            _slider.highValue = _seekMax;
        }

        /// <summary>
        /// Sets the value of the slider.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetValue(float value)
        {
            _slider.value = value;
        }

        /// <summary>
        /// Handles the pointer down event.
        /// </summary>
        /// <param name="e">The pointer down event.</param>
        public void OnPointerDown(PointerDownEvent e)
        {
            StartSeek();
        }

        /// <summary>
        /// Handles the pointer up event.
        /// </summary>
        /// <param name="e">The pointer up event.</param>
        public void OnPointerUp(PointerUpEvent e)
        {
            StopSeek();
        }

        /// <summary>
        /// Starts seeking in the playback.
        /// </summary>
        public void StartSeek()
        {
            _config.FileReader.UserActivelyScrubbing = true;
        }

        /// <summary>
        /// Stops seeking in the playback.
        /// </summary>
        public void StopSeek()
        {
            _config.FileReader.UserActivelyScrubbing = false;
            _config.FileReader.SetPauseState(!_config.FileReader.IsPlaying);
        }

        /// <summary>
        /// Stops the playback.
        /// </summary>
        public void StopPlay()
        {
            _seekMax = 1.0f;
            _slider.highValue = _seekMax;
        }

        /// <summary>
        /// Starts the playback.
        /// </summary>
        public void StartPlay()
        {
            InitNumSnapshots();
        }

        private void Seek(int seekTime)
        {
            _config.FileReader.Seek(seekTime);
        }
    }
}
