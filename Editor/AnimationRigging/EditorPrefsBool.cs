// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Saves a bool value to editor prefs.
    /// </summary>
    public class EditorPrefsBool
    {
        private bool _initialized;
        private bool _value;
        private string _name;

        /// <summary>
        /// Get/Set the value of this editor preference.
        /// </summary>
        public bool Value
        {
            get
            {
                if (!_initialized)
                {
                    _value = EditorPrefs.GetBool(_name, _value);
                    _initialized = true;
                }
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    EditorPrefs.SetBool(_name, _value = value);
                }
            }
        }

        /// <summary>
        /// Creates a new editor prefs bool to be saved.
        /// </summary>
        /// <param name="name">The name of the editor prefs entry.</param>
        /// <param name="initialValue">The initial value of the editor prefs bool.</param>
        /// <typeparam name="T">The class for this specific editor prefs.</typeparam>
        /// <returns></returns>
        public static EditorPrefsBool Create<T>(string name, bool initialValue) =>
            new()
            {
                _name = $"{typeof(T)}.{name}",
                _value = initialValue
            };

        private EditorPrefsBool() {}
    }
}
