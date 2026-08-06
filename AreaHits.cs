using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoSeph.Core.Combat
{
    /// <summary>
    /// One target inside an area, with how far into the area it sits.
    /// </summary>
    public readonly struct AreaHit<T>
    {
        public T Target { get; }
        /// <summary>
        /// Distance from the area's origin: along the axis for a beam, from the centre for a circle.
        /// For a beam this is the axial distance, never the straight-line one.
        /// </summary>
        public float Distance { get; }

        public AreaHit(T target, float distance)
        {
            Target = target;
            Distance = distance;
        }
    }

    /// <summary>
    /// Which targets stand inside a 2D area, ordered nearest first. Flat shapes tested against target
    /// origins, with no physics server involved: nothing caps the result, and the same inputs give the
    /// same list in the same order every run.
    ///
    /// Targets are points. A target's own radius is not modelled, so size is the area's property alone.
    /// </summary>
    public static class AreaHits
    {
        /// <summary>
        /// Every candidate inside a beam - a rectangle of the given half-width running from
        /// <paramref name="origin"/> along <paramref name="direction"/> for <paramref name="length"/> -
        /// ordered from nearest to furthest along it.
        ///
        /// Both boundaries are inclusive. An area of no size hits nothing at all - a zero length and a
        /// zero <paramref name="halfWidth"/> alike.
        /// </summary>
        /// <param name="direction">Need not be unit length; it is normalised internally.</param>
        /// <param name="halfWidth">Half the beam's full width - a target this far off the axis still hits.</param>
        /// <param name="positionOf">
        /// Reads one candidate's flat position - the only route from a candidate to a coordinate,
        /// which is what leaves <typeparamref name="T"/> unconstrained.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A negative <paramref name="length"/> or <paramref name="halfWidth"/>, or a zero-length
        /// <paramref name="direction"/>. Zero size is a defined answer; these are not answers at all.
        /// </exception>
        public static List<AreaHit<T>> InBeam<T>(
            Vector2 origin,
            Vector2 direction,
            float length,
            float halfWidth,
            IReadOnlyList<T> candidates,
            Func<T, Vector2> positionOf)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Every candidate inside a circle, ordered from nearest to furthest from its centre.
        /// The splash counterpart to <see cref="InBeam"/>, sharing its rules: the edge is inclusive,
        /// a zero radius hits nothing, and equal distances resolve in candidate order.
        /// </summary>
        /// <param name="positionOf">Reads one candidate's flat position.</param>
        /// <exception cref="ArgumentOutOfRangeException">A negative <paramref name="radius"/>.</exception>
        public static List<AreaHit<T>> InCircle<T>(
            Vector2 centre,
            float radius,
            IReadOnlyList<T> candidates,
            Func<T, Vector2> positionOf)
        {
            throw new NotImplementedException();
        }
    }
}
