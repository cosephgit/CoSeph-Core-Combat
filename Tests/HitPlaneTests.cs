using System;
using System.Numerics;
using Xunit;

namespace CoSeph.Core.Combat.Tests
{
    /// <summary>
    /// Axis mapping is trivial-looking and easy to get wrong: a transposed position is a bug that
    /// presents as bad aim, miles from its cause. Here to pin the mapping, not because it is hard.
    /// </summary>
    public class HitPlaneTests
    {
        private static readonly Vector3 Point = new(1f, 2f, 3f);

        [Fact]
        public void Flatten_Xz_DropsTheYAxis()
        {
            Assert.Equal(new Vector2(1f, 3f), HitPlane.Flatten(Point, GroundPlane.Xz));
        }

        [Fact]
        public void Flatten_Xy_DropsTheZAxis()
        {
            Assert.Equal(new Vector2(1f, 2f), HitPlane.Flatten(Point, GroundPlane.Xy));
        }

        [Fact]
        public void Flatten_Yz_DropsTheXAxis()
        {
            Assert.Equal(new Vector2(2f, 3f), HitPlane.Flatten(Point, GroundPlane.Yz));
        }

        [Theory]
        [InlineData(GroundPlane.Xz)]
        [InlineData(GroundPlane.Xy)]
        [InlineData(GroundPlane.Yz)]
        public void HeightOf_IsTheAxisFlattenDropped(GroundPlane plane)
        {
            // whatever Flatten kept, HeightOf holds the rest - together they lose nothing
            Vector2 flat = HitPlane.Flatten(Point, plane);
            float height = HitPlane.HeightOf(Point, plane);

            Assert.Equal(Point, HitPlane.Restore(flat, height, plane));
        }

        [Fact]
        public void Restore_PutsAFlatResultBackAtAChosenHeight()
        {
            // the drawing case: hits resolve flat, the visual is drawn at the muzzle's height
            Vector3 restored = HitPlane.Restore(new Vector2(4f, 5f), 1f, GroundPlane.Xz);

            Assert.Equal(new Vector3(4f, 1f, 5f), restored);
        }

        [Fact]
        public void Flatten_UnknownPlane_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HitPlane.Flatten(Point, (GroundPlane)99));
        }

        [Fact]
        public void Restore_UnknownPlane_Throws()
        {
            // the drawing path reaches Restore without necessarily going through Flatten, so it
            // cannot lean on Flatten's guard having already rejected the value
            Assert.Throws<ArgumentOutOfRangeException>(
                () => HitPlane.Restore(new Vector2(4f, 5f), 1f, (GroundPlane)99));
        }

        [Fact]
        public void HeightOf_UnknownPlane_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HitPlane.HeightOf(Point, (GroundPlane)99));
        }
    }
}
