// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Trigger an audio clip on this audio source.
    /// Copied from Meta XR Interaction SDK OVR Samples.
    /// </summary>
    public class MovementAudioTrigger : MonoBehaviour
    {
        /// <summary>
        /// Struct that contains data about a min max pair of floats.
        /// </summary>
        [System.Serializable]
        public struct MinMaxPair
        {
            /// <inheritdoc cref="_useRandomRange" />
            public bool UseRandomRange => _useRandomRange;

            /// <inheritdoc cref="_min" />
            public float Min => _min;

            /// <inheritdoc cref="_max" />
            public float Max => _max;

            /// <summary>
            /// True if random range should be used.
            /// </summary>
            [SerializeField]
            private bool _useRandomRange;

            /// <summary>
            /// The minimum float.
            /// </summary>
            [SerializeField]
            private float _min;

            /// <summary>
            /// The maximum float.
            /// </summary>
            [SerializeField]
            private float _max;
        }

        /// <summary>
        /// The audio source used to trigger audio.
        /// </summary>
        [SerializeField]
        private AudioSource _audioSource;

        /// <summary>
        /// Audio clip arrays with a value greater than 1 will have randomized playback.
        /// </summary>
        [Tooltip("Audio clip arrays with a value greater than 1 will have randomized playback.")]
        [SerializeField]
        private AudioClip[] _audioClips;

        /// <summary>
        /// Volume set here will override the volume set on the attached sound source component.
        /// </summary>
        [Tooltip("Volume set here will override the volume set on the attached sound source component.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 0.7f;
        /// <inheritdoc cref="_volume" />
        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }

        /// <summary>
        /// Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.
        /// </summary>
        [Tooltip("Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.")]
        [SerializeField]
        private MinMaxPair _volumeRandomization;
        /// <inheritdoc cref="_volumeRandomization" />
        public MinMaxPair VolumeRandomization
        {
            get => _volumeRandomization;
            set => _volumeRandomization = value;
        }

        /// <summary>
        /// Pitch set here will override the volume set on the attached sound source component.
        /// </summary>
        [Tooltip("Pitch set here will override the volume set on the attached sound source component.")]
        [SerializeField]
        [Range(-3f, 3f)]
        [Space(10)]
        private float _pitch = 1f;
        /// <inheritdoc cref="_pitch" />
        public float Pitch
        {
            get => _pitch;
            set => _pitch = value;
        }

        /// <summary>
        /// Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.
        /// </summary>
        [Tooltip("Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.")]
        [SerializeField]
        private MinMaxPair _pitchRandomization;
        /// <inheritdoc cref="_pitchRandomization" />
        public MinMaxPair PitchRandomization
        {
            get => _pitchRandomization;
            set => _pitchRandomization = value;
        }

        /// <summary>
        /// True by default. Set to false for sounds to bypass the spatializer plugin. Will override settings on attached audio source.
        /// </summary>
        [Tooltip("True by default. Set to false for sounds to bypass the spatializer plugin. Will override settings on attached audio source.")]
        [SerializeField]
        [Space(10)]
        private bool _spatialize = true;
        /// <inheritdoc cref="_spatialize" />
        public bool Spatialize
        {
            get => _spatialize;
            set => _spatialize = value;
        }

        /// <summary>
        /// False by default. Set to true to enable looping on this sound. Will override settings on attached audio source.
        /// </summary>
        [Tooltip("False by default. Set to true to enable looping on this sound. Will override settings on attached audio source.")]
        [SerializeField]
        private bool _loop = false;
        /// <inheritdoc cref="_loop" />
        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        /// <summary>
        /// 100% by default. Sets likelyhood sample will actually play when called.
        /// </summary>
        [Tooltip("100% by default. Sets likelyhood sample will actually play when called.")]
        [SerializeField]
        private float _chanceToPlay = 100;
        /// <inheritdoc cref="_chanceToPlay" />
        public float ChanceToPlay
        {
            get => _chanceToPlay;
            set => _chanceToPlay = value;
        }

        /// <summary>
        /// If enabled, audio will play automatically when this gameobject is enabled.
        /// </summary>
        [Tooltip("If enabled, audio will play automatically when this gameobject is enabled.")]
        [SerializeField]
        private bool _playOnStart = false;

        private int _previousAudioClipIndex = -1;

        protected virtual void Start()
        {
            if (_audioSource == null)
            {
                _audioSource = gameObject.GetComponent<AudioSource>();
            }

            // Play audio on start if enabled
            if (_playOnStart)
            {
                PlayAudio();
            }
        }

        /// <summary>
        /// Play the audio clip on this audio source.
        /// </summary>
        public void PlayAudio()
        {
            // Check if random chance is set
            float pick = Random.Range(0.0f, 100.0f);
            if (_chanceToPlay < 100 && pick > _chanceToPlay)
            {
                return;
            }

            // Check if volume randomization is set
            if (_volumeRandomization.UseRandomRange == true)
            {
                _audioSource.volume = Random.Range(_volumeRandomization.Min, _volumeRandomization.Max);
            }
            else
            {
                _audioSource.volume = _volume;
            }

            // Check if pitch randomization is set
            if (_pitchRandomization.UseRandomRange == true)
            {
                _audioSource.pitch = Random.Range(_pitchRandomization.Min, _pitchRandomization.Max);
            }
            else
            {
                _audioSource.pitch = _pitch;
            }

            _audioSource.spatialize = _spatialize;
            _audioSource.loop = _loop;

            _audioSource.clip = RandomClipWithoutRepeat();

            _audioSource.Play();
        }

        /// <summary>
        /// Choose a random clip without repeating the last clip.
        /// </summary>
        private AudioClip RandomClipWithoutRepeat()
        {
            if (_audioClips.Length == 1)
            {
                return _audioClips[0];
            }

            int randomOffset = Random.Range(1, _audioClips.Length);
            int index = (_previousAudioClipIndex + randomOffset) % _audioClips.Length;
            _previousAudioClipIndex = index;
            return _audioClips[index];
        }

        #region Inject

        public void InjectAllAudioTrigger(AudioSource audioSource, AudioClip[] audioClips)
        {
            InjectAudioSource(audioSource);
            InjectAudioClips(audioClips);
        }

        public void InjectAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }
        public void InjectAudioClips(AudioClip[] audioClips)
        {
            _audioClips = audioClips;
        }

        public void InjectOptionalPlayOnStart(bool playOnStart)
        {
            _playOnStart = playOnStart;
        }

        #endregion
    }
}
