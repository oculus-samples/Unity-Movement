// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects
{
    /*
    *
    * The following code was taken from: http://schemingdeveloper.com
    *
    * Visit our game studio website: http://stopthegnomes.com
    *
    * License: You may use this code however you see fit, as long as you include this notice
    *          without any modifications.
    *
    *          You may not publish a paid asset on Unity store if its main function is based on
    *          the following code, but you may publish a paid asset that uses this code.
    *
    *          If you intend to use this in a Unity store asset or a commercial project, it would
    *          be appreciated, but not required, if you let me know with a link to the asset. If I
    *          don't get back to you just go ahead and use it anyway!
    */

    /// <summary>
    /// Class used to hash vertices reliably. CompareVectorHashes class
    /// is used to test its reliability against Unity's default hashing
    /// of vertices.
    /// </summary>
    public struct VertexKey
    {
        private readonly long _x;
        private readonly long _y;
        private readonly long _z;

        // Change this if you require a different precision.
        private const long Tolerance = 1000000;

        // Magic FNV values. Do not change these.
        private const long FNV32Init = 0x811c9dc5;
        private const long FNV32Prime = 0x01000193;

        /// <summary>
        /// VertexKey constructor.
        /// </summary>
        /// <param name="position">Input position.</param>
        public VertexKey(Vector3 position)
        {
            _x = (long)(Mathf.Round(position.x * Tolerance));
            _y = (long)(Mathf.Round(position.y * Tolerance));
            _z = (long)(Mathf.Round(position.z * Tolerance));
        }

        /// <summary>
        /// Override for Equals.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var key = (VertexKey)obj;
            return _x == key._x && _y == key._y && _z == key._z;
        }

        /// <summary>
        /// Override for getting hash code.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            long rv = FNV32Init;
            rv ^= _x;
            rv *= FNV32Prime;
            rv ^= _y;
            rv *= FNV32Prime;
            rv ^= _z;
            rv *= FNV32Prime;

            return rv.GetHashCode();
        }
    }
}
