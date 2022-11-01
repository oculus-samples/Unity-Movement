// Copyright (c) Meta Platforms, Inc. and affiliates.

/*
 * MIT License

Copyright (c) 2020 TheDudeFromCI

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

// Unity Interface Support based on
// https://github.com/TheDudeFromCI/Unity-Interface-Support/
*/

using UnityEngine;
using System;

namespace Oculus.Movement.Attributes
{
    /// <summary>
    /// When this attribute is attached to a MonoBehaviour field within a
    /// Unity Object, this allows an interface to be specified in to to
    /// entire only a specific type of MonoBehaviour can be attached.
    /// </summary>
    public class InterfaceAttribute : PropertyAttribute
    {
        public Type[] Types = null;
        public string TypeFromFieldName;

        /// <summary>
        /// Creates a new Interface attribute.
        /// </summary>
        /// <param name="type">The type of interface which is allowed.</param>
        public InterfaceAttribute(Type type, params Type[] types)
        {
            Types = new Type[types.Length + 1];
            Types[0] = type;
            for (int i = 0; i < types.Length; i++)
            {
                Types[i + 1] = types[i];
            }
        }

        public InterfaceAttribute(string typeFromFieldName)
        {
            this.TypeFromFieldName = typeFromFieldName;
        }
    }
}
