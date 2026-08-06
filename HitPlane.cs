using System;
using System.Numerics;

namespace CoSeph.Core.Combat
{
    /// <summary>
    /// Which pair of axes a 3D world's gameplay actually happens on. The third is the one dropped.
    /// </summary>
    public enum GroundPlane
    {
        /// <summary>Y is up. The common case: Godot, Unity, and most engines built on them.</summary>
        Xz,
        /// <summary>Z is up. Unreal, and most CAD-derived conventions.</summary>
        Xy,
        /// <summary>X is up. Here for completeness rather than because it is common.</summary>
        Yz,
    }

    /// <summary>
    /// The reduction every other type in this package is built on: a 3D game whose gameplay is planar
    /// answers its hit questions in 2D.
    ///
    /// Which axis is dropped is stated here once. Doing it by hand at each call site is how a
    /// transposed position gets loose, and a transposed position is a bug that looks like bad aim.
    /// </summary>
    public static class HitPlane
    {
        /// <summary>Drops the up axis, giving the 2D position every area test works in.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="plane"/> is outside the enum.</exception>
        public static Vector2 Flatten(Vector3 point, GroundPlane plane)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts a flattened position back into 3D at a chosen height, for drawing a result that was
        /// resolved flat.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="plane"/> is outside the enum. Guarded in its own right: the drawing path
        /// reaches this without necessarily having passed through <see cref="Flatten"/> first.
        /// </exception>
        public static Vector3 Restore(Vector2 flat, float height, GroundPlane plane)
        {
            throw new NotImplementedException();
        }

        /// <summary>The height that <see cref="Flatten"/> discarded.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="plane"/> is outside the enum.</exception>
        public static float HeightOf(Vector3 point, GroundPlane plane)
        {
            throw new NotImplementedException();
        }
    }
}
