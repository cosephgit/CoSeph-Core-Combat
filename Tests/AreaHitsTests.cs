using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace CoSeph.Core.Combat.Tests
{
    /// <summary>
    /// Every interesting case is a boundary - behind the origin, exactly at the length, exactly on
    /// the edge - and none of them are catchable by eye in a running game.
    ///
    /// Every value here is exact in binary floating point, so a boundary case tests the comparison
    /// rather than the test's own accumulated error.
    /// </summary>
    public class AreaHitsTests
    {
        private static readonly Vector2 Origin = Vector2.Zero;
        private static readonly Vector2 AlongX = new(1f, 0f);
        private const float Length = 10f;
        private const float HalfWidth = 0.5f;

        private static List<AreaHit<Vector2>> InBeam(params Vector2[] candidates)
        {
            return AreaHits.InBeam(Origin, AlongX, Length, HalfWidth, candidates, p => p);
        }

        // ---- InBeam ----

        [Fact]
        public void InBeam_TargetBehindOrigin_NotHit()
        {
            Assert.Empty(InBeam(new Vector2(-3f, 0f)));
        }

        [Fact]
        public void InBeam_TargetPastLength_NotHit()
        {
            Assert.Empty(InBeam(new Vector2(12f, 0f)));
        }

        [Fact]
        public void InBeam_TargetExactlyAtLength_Hit()
        {
            // the far end is inclusive: a beam stopped by a wall still damages what stands against it
            List<AreaHit<Vector2>> hits = InBeam(new Vector2(Length, 0f));

            Assert.Single(hits);
            Assert.Equal(Length, hits[0].Distance);
        }

        [Fact]
        public void InBeam_TargetExactlyAtHalfWidth_Hit()
        {
            Assert.Single(InBeam(new Vector2(4f, HalfWidth)));
        }

        [Fact]
        public void InBeam_TargetJustOutsideHalfWidth_NotHit()
        {
            Assert.Empty(InBeam(new Vector2(4f, HalfWidth + 0.0001f)));
        }

        [Fact]
        public void InBeam_ThreeTargetsOnLine_ReturnsAllNearToFar()
        {
            // fed far-first, so passing this cannot be an accident of input order
            List<AreaHit<Vector2>> hits = InBeam(
                new Vector2(7f, 0f),
                new Vector2(2f, 0f),
                new Vector2(5f, 0f));

            Assert.Equal(3, hits.Count);
            Assert.Equal(2f, hits[0].Distance);
            Assert.Equal(5f, hits[1].Distance);
            Assert.Equal(7f, hits[2].Distance);
        }

        [Fact]
        public void InBeam_ZeroLength_HitsNothing()
        {
            // a weapon flush against a wall: nothing is hit, not even a target on the origin
            List<AreaHit<Vector2>> hits = AreaHits.InBeam(
                Origin, AlongX, 0f, HalfWidth,
                new[] { Vector2.Zero, new Vector2(1f, 0f) },
                p => p);

            Assert.Empty(hits);
        }

        [Fact]
        public void InBeam_EmptyCandidates_ReturnsEmpty()
        {
            Assert.Empty(InBeam());
        }

        [Fact]
        public void InBeam_SameInputsTwice_ReturnsIdenticalOrder()
        {
            Vector2[] candidates =
            {
                new(7f, 0.25f),
                new(2f, -0.25f),
                new(5f, 0f),
            };

            List<AreaHit<Vector2>> first = AreaHits.InBeam(Origin, AlongX, Length, HalfWidth, candidates, p => p);
            List<AreaHit<Vector2>> second = AreaHits.InBeam(Origin, AlongX, Length, HalfWidth, candidates, p => p);

            Assert.Equal(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].Target, second[i].Target);
                Assert.Equal(first[i].Distance, second[i].Distance);
            }
        }

        [Fact]
        public void InBeam_NonUnitDirection_TreatedAsUnit()
        {
            // callers hand over a basis vector; silently scaling the beam's length by its magnitude
            // would be a trap
            List<AreaHit<Vector2>> hits = AreaHits.InBeam(
                Origin, new Vector2(4f, 0f), Length, HalfWidth,
                new[] { new Vector2(8f, 0f) },
                p => p);

            Assert.Single(hits);
            Assert.Equal(8f, hits[0].Distance);
        }

        [Fact]
        public void InBeam_TargetsAtEqualDistance_OrderedByCandidateIndex()
        {
            // ordering has to be total, not merely sorted, or a seeded run stops reproducing itself
            // the first time damage runs out mid-list
            Vector2[] abreast = { new(5f, 0.25f), new(5f, -0.25f) };

            List<AreaHit<Vector2>> hits = AreaHits.InBeam(Origin, AlongX, Length, HalfWidth, abreast, p => p);

            Assert.Equal(abreast[0], hits[0].Target);
            Assert.Equal(abreast[1], hits[1].Target);
        }

        [Fact]
        public void InBeam_ZeroHalfWidth_HitsNothing()
        {
            // no size means no hit, so a weapon with zero area size misses a target dead on its axis
            List<AreaHit<Vector2>> hits = AreaHits.InBeam(
                Origin, AlongX, Length, 0f,
                new[] { new Vector2(4f, 0f) },
                p => p);

            Assert.Empty(hits);
        }

        [Fact]
        public void InBeam_NegativeLength_Throws()
        {
            // zero is a defined answer; a negative is not, and quietly selecting nothing for it
            // would hide the caller's arithmetic slip behind a beam that never connects
            Assert.Throws<ArgumentOutOfRangeException>(() => AreaHits.InBeam(
                Origin, AlongX, -5f, HalfWidth,
                new[] { new Vector2(1f, 0f) },
                p => p));
        }

        [Fact]
        public void InBeam_NegativeHalfWidth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AreaHits.InBeam(
                Origin, AlongX, Length, -0.5f,
                new[] { new Vector2(1f, 0f) },
                p => p));
        }

        [Fact]
        public void InBeam_ZeroDirection_Throws()
        {
            // normalising a zero vector gives NaN, and every comparison against NaN is false - so
            // without a guard an uninitialised facing silently misses everything
            Assert.Throws<ArgumentOutOfRangeException>(() => AreaHits.InBeam(
                Origin, Vector2.Zero, Length, HalfWidth,
                new[] { new Vector2(1f, 0f) },
                p => p));
        }

        // ---- InCircle ----

        [Fact]
        public void InCircle_TargetInside_HitAndOutsideNot()
        {
            List<AreaHit<Vector2>> hits = AreaHits.InCircle(
                Vector2.Zero, 2f,
                new[] { new Vector2(1f, 0f), new Vector2(5f, 0f) },
                p => p);

            Assert.Single(hits);
            Assert.Equal(new Vector2(1f, 0f), hits[0].Target);
        }

        [Fact]
        public void InCircle_TargetExactlyOnRadius_Hit()
        {
            // inclusive at the edge, matching InBeam's far end - one rule for the whole family
            List<AreaHit<Vector2>> hits = AreaHits.InCircle(
                Vector2.Zero, 2f, new[] { new Vector2(0f, 2f) }, p => p);

            Assert.Single(hits);
            Assert.Equal(2f, hits[0].Distance);
        }

        [Fact]
        public void InCircle_Targets_OrderedNearToFarFromCentre()
        {
            Vector2[] candidates = { new(3f, 0f), new(0f, 1f), new(2f, 0f) };

            List<AreaHit<Vector2>> hits = AreaHits.InCircle(Vector2.Zero, 4f, candidates, p => p);

            Assert.Equal(new[] { 1f, 2f, 3f }, new[] { hits[0].Distance, hits[1].Distance, hits[2].Distance });
        }

        [Fact]
        public void InCircle_ZeroRadius_HitsNothing()
        {
            // matches InBeam's zero length: no area means no hit, including a target on the centre
            List<AreaHit<Vector2>> hits = AreaHits.InCircle(
                Vector2.Zero, 0f, new[] { Vector2.Zero }, p => p);

            Assert.Empty(hits);
        }

        [Fact]
        public void InCircle_EmptyCandidates_ReturnsEmpty()
        {
            Assert.Empty(AreaHits.InCircle(Vector2.Zero, 5f, new Vector2[0], p => p));
        }

        [Fact]
        public void InCircle_NegativeRadius_Throws()
        {
            // the same rule as the beam's negative length: zero is an answer, negative is a mistake
            Assert.Throws<ArgumentOutOfRangeException>(() => AreaHits.InCircle(
                Vector2.Zero, -2f, new[] { new Vector2(1f, 0f) }, p => p));
        }
    }
}
