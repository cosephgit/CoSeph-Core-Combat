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
            if (length < 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "A beam cannot be shorter than nothing.");
            if (halfWidth < 0f)
                throw new ArgumentOutOfRangeException(nameof(halfWidth), halfWidth, "A beam cannot be narrower than nothing.");

            // squared length, so a direction too small to normalise is caught alongside an outright
            // zero one - and written as !(> 0) so a NaN facing throws rather than missing everything
            float directionLengthSq = direction.LengthSquared();
            if (!(directionLengthSq > 0f))
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "A beam needs a direction to point in.");

            List<AreaHit<T>> hits = new();

            // no area means no hit, so neither dimension can be zero - checked before the loop
            // because a target on the origin would otherwise pass every comparison below
            if (length == 0f || halfWidth == 0f)
                return hits;

            Vector2 axis = direction / MathF.Sqrt(directionLengthSq);

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 offset = positionOf(candidates[i]) - origin;
                float along = Vector2.Dot(offset, axis);
                if (along < 0f || along > length)
                    continue;

                // cross product against a unit axis is the signed distance off it, without building
                // a perpendicular vector to project onto
                float across = (offset.X * axis.Y) - (offset.Y * axis.X);
                if (MathF.Abs(across) > halfWidth)
                    continue;

                hits.Add(new AreaHit<T>(candidates[i], along));
            }

            return SortedNearestFirst(hits);
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
            if (radius < 0f)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "A circle cannot be smaller than nothing.");

            List<AreaHit<T>> hits = new();

            if (radius == 0f)
                return hits;

            for (int i = 0; i < candidates.Count; i++)
            {
                // the distance is the hit's own answer as well as the test, so there is nothing to
                // save by comparing squares here
                float distance = (positionOf(candidates[i]) - centre).Length();
                if (distance > radius)
                    continue;

                hits.Add(new AreaHit<T>(candidates[i], distance));
            }

            return SortedNearestFirst(hits);
        }

        /// <summary>
        /// Every candidate inside an axis-aligned rectangle, ordered from nearest to furthest from its
        /// centre. The room counterpart to <see cref="InCircle"/> - a shape that is a place rather than
        /// a reach - and it shares the same rules: the edge is inclusive, an area of no size hits
        /// nothing, and equal distances resolve in candidate order.
        ///
        /// <paramref name="min"/> and <paramref name="max"/> are opposite corners, not a corner and a
        /// size. A caller holding a grid rect owns the conversion, including whichever of its own
        /// half-open conventions applies - this knows only about the flat plane it is given.
        /// </summary>
        /// <param name="min">The lower corner on both axes, inclusive.</param>
        /// <param name="max">The upper corner on both axes, inclusive.</param>
        /// <param name="positionOf">Reads one candidate's flat position.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="max"/> below <paramref name="min"/> on either axis. Zero size is a defined
        /// answer; an inside-out rectangle is not an answer at all.
        /// </exception>
        public static List<AreaHit<T>> InRect<T>(
            Vector2 min,
            Vector2 max,
            IReadOnlyList<T> candidates,
            Func<T, Vector2> positionOf)
        {
            if (max.X < min.X || max.Y < min.Y)
                throw new ArgumentOutOfRangeException(nameof(max), max, "A rectangle's upper corner cannot sit below its lower one.");

            List<AreaHit<T>> hits = new();

            // flat on either axis is a line rather than a rectangle, so there is no inside to be in
            if (max.X == min.X || max.Y == min.Y)
                return hits;

            // the corners are how the rectangle is given, not what it is measured from - nearest-first
            // is only worth paying for if it means nearest to the middle of the area
            Vector2 centre = (min + max) * 0.5f;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 position = positionOf(candidates[i]);

                // both axes, never either: an "or" would take in the whole cross through the rectangle
                if (position.X < min.X || position.X > max.X || position.Y < min.Y || position.Y > max.Y)
                    continue;

                hits.Add(new AreaHit<T>(candidates[i], (position - centre).Length()));
            }

            return SortedNearestFirst(hits);
        }

        /// <summary>
        /// Nearest first, with equal distances left in the order their candidates were given in.
        ///
        /// The tie-break is stated rather than borrowed from a stable sort. LINQ's <c>OrderBy</c> would
        /// satisfy it and <see cref="List{T}.Sort"/> would not, so leaning on either makes a documented
        /// contract depend on which one a later edit reaches for - and that edit compiles.
        /// </summary>
        private static List<AreaHit<T>> SortedNearestFirst<T>(List<AreaHit<T>> hits)
        {
            // hits were appended in candidate order, so a hit's position here is its candidate rank.
            // Sorting the ranks rather than the hits is what keeps that rank readable during the
            // comparison, which is the only place it is needed.
            int[] ranks = new int[hits.Count];
            for (int i = 0; i < ranks.Length; i++)
                ranks[i] = i;

            Array.Sort(ranks, (a, b) =>
            {
                int byDistance = hits[a].Distance.CompareTo(hits[b].Distance);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });

            List<AreaHit<T>> sorted = new(hits.Count);
            for (int i = 0; i < ranks.Length; i++)
                sorted.Add(hits[ranks[i]]);

            return sorted;
        }
    }
}
