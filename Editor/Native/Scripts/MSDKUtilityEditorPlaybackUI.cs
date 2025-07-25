// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

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
        private Button _playButton;
        private Button _stopButton;
        private Label _timestamp;

        // UI styling constants
        private const int _buttonSizeHeight = 24;
        private const int _singleLineSpace = 4;
        private static readonly Color _primaryColor = new Color(0.0f, 0.47f, 0.95f, 1.0f);
        private static readonly Color _cardBackgroundColor = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color _borderColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Color _buttonHoverColor = new Color(0.0f, 0.47f, 0.95f, 0.2f);

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
            // Main container with card-like styling - more compact
            style.backgroundColor = _cardBackgroundColor;
            style.borderBottomWidth = style.borderTopWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderBottomColor = style.borderTopColor = style.borderLeftColor = style.borderRightColor = _borderColor;
            style.borderTopLeftRadius = style.borderTopRightRadius = style.borderBottomLeftRadius = style.borderBottomRightRadius = 3;
            style.paddingTop = style.paddingBottom = 4;
            style.paddingLeft = style.paddingRight = 8;
            style.marginTop = 2;
            style.marginBottom = 2;

            // Combined header and timestamp in one row
            var headerContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 2
                }
            };

            var headerLabel = new Label("Playback")
            {
                style =
                {
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            _timestamp = new Label()
            {
                style =
                {
                    fontSize = 12,
                    color = new Color(0.8f, 0.8f, 0.8f, 1.0f)
                }
            };

            headerContainer.Add(headerLabel);
            headerContainer.Add(_timestamp);
            Add(headerContainer);

            // Slider with minimal margins
            _slider = new PlaybackSlider(_config);
            _slider.Init();
            _slider.style.marginBottom = 4;
            Add(_slider);

            // Controls in a more compact row
            var controlsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };

            // Create smaller buttons
            _playButton = CreateControlButton(_pauseImage, PlayPause, "Play/Pause");
            _stopButton = CreateControlButton(_stopImage, Stop, "Stop");

            controlsContainer.Add(_playButton);
            controlsContainer.Add(_stopButton);
            Add(controlsContainer);
        }

        private Button CreateControlButton(Texture2D icon, System.Action clickAction, string tooltipInfo)
        {
            var button = new Button(clickAction)
            {
                tooltip = tooltipInfo,
                style =
                {
                    width = _buttonSizeHeight + 6,
                    height = _buttonSizeHeight,
                    marginLeft = 2,
                    marginRight = 2,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                }
            };

            // Add hover effect
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.backgroundColor = _buttonHoverColor;
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            });

            // Add icon
            var image = new Image
            {
                image = icon,
                style =
                {
                    width = 16,
                    height = 16
                }
            };

            button.Add(image);
            return button;
        }

        /// <summary>
        /// Updates the UI elements.
        /// </summary>
        public void Update()
        {
            _slider.Update();
            _timestamp.text = $"Playback Time: {_config.FileReader.PlaybackTime:F2}s";
        }

        private void PlayPause()
        {
            _config.FileReader.SetPauseState(_config.FileReader.IsPlaying);
            _config.FileReader.UserActivelyScrubbing = false;

            // Update button icon
            var playButtonImage = _playButton.Q<Image>();
            if (playButtonImage != null)
            {
                playButtonImage.image = _config.FileReader.IsPlaying ? _pauseImage : _playImage;
            }
        }

        private void Stop()
        {
            _config.FileReader.ClosePlaybackFile();
            _slider.StopPlay();

            // Update button icon
            var playButtonImage = _playButton.Q<Image>();
            if (playButtonImage != null)
            {
                playButtonImage.image = _config.FileReader.IsPlaying ? _pauseImage : _playImage;
            }

            // Clear the playback UI reference in the window
            _config.ClearPlaybackUI();

            // Remove this UI element from its parent
            if (parent != null)
            {
                parent.Remove(this);
            }
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
        private Label _currentTimeLabel;
        private float _seekMax = 1.0f;

        // UI styling constants
        private static readonly Color _sliderTrackColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        private static readonly Color _sliderThumbColor = new Color(0.0f, 0.47f, 0.95f, 1.0f);

        /// <summary>
        /// Initializes the slider.
        /// </summary>
        public void Init()
        {
            // Main container styling - more compact
            style.marginTop = 2;
            style.marginBottom = 4;

            // Percentage label
            _currentTimeLabel = new Label("0%")
            {
                style =
                {
                    fontSize = 11,
                    color = new Color(0.7f, 0.7f, 0.7f, 1.0f),
                    alignSelf = Align.FlexStart,
                    marginBottom = 1
                }
            };
            Add(_currentTimeLabel);

            // Create slider with modern styling
            _slider = new Slider(0.0f, 1.0f)
            {
                style =
                {
                    height = 18
                }
            };

            // Style the slider track
            var sliderTrack = _slider.Q("unity-tracker");
            if (sliderTrack != null)
            {
                sliderTrack.style.backgroundColor = _sliderTrackColor;
                sliderTrack.style.borderTopLeftRadius = sliderTrack.style.borderTopRightRadius =
                    sliderTrack.style.borderBottomLeftRadius = sliderTrack.style.borderBottomRightRadius = 2;
            }

            // Style the slider dragger (thumb)
            var sliderDragger = _slider.Q("unity-dragger");
            if (sliderDragger != null)
            {
                sliderDragger.style.backgroundColor = _sliderThumbColor;
                sliderDragger.style.borderTopLeftRadius = sliderDragger.style.borderTopRightRadius =
                    sliderDragger.style.borderBottomLeftRadius = sliderDragger.style.borderBottomRightRadius = 6;
                sliderDragger.style.width = 12;
                sliderDragger.style.height = 12;
            }

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

                // Update percentage label
                if (_config.FileReader.NumSnapshots > 0)
                {
                    float percentage = (float)_config.FileReader.SnapshotIndex / _config.FileReader.NumSnapshots * 100f;
                    _currentTimeLabel.text = $"{Mathf.RoundToInt(percentage)}%";
                }
                else
                {
                    _currentTimeLabel.text = "0%";
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
