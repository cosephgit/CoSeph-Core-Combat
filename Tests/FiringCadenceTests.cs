using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CoSeph.Core.Combat.Tests
{
    /// <summary>
    /// Cadence is where a weapon's timing bugs hide: a frame hitch that double-damages, a burst that
    /// charges its pause twice, a sustained beam that strobes while the weapon tracks.
    ///
    /// Interval 0.5 and step 0.25 throughout - both exact in binary floating point, so accumulating
    /// them never drifts off an interval boundary.
    /// </summary>
    public class FiringCadenceTests
    {
        private const float Interval = 0.5f;
        private const float Step = 0.25f;

        /// <summary>Runs the cadence for a number of steps, returning the elapsed time of each application.</summary>
        private static List<float> ApplicationTimes(FiringCadence cadence, int steps, bool targetPresent = true)
        {
            List<float> times = new();
            for (int i = 1; i <= steps; i++)
            {
                if (cadence.Update(Step, targetPresent).Applies)
                    times.Add(i * Step);
            }
            return times;
        }

        /// <summary>Runs the cadence for a number of steps, returning what each one reported.</summary>
        private static List<CadenceStep> ReportedSteps(FiringCadence cadence, int steps, bool targetPresent = true)
        {
            List<CadenceStep> reported = new();
            for (int i = 1; i <= steps; i++)
                reported.Add(cadence.Update(Step, targetPresent));
            return reported;
        }

        // ---- Construction ----

        [Theory]
        [InlineData(0f)]
        [InlineData(-0.5f)]
        public void Constructor_NonPositiveFireInterval_Throws(float fireInterval)
        {
            // an interval of zero would apply on every tick - a mistyped fire_delay silently
            // becoming a weapon that fires at the tick rate
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringCadence(fireInterval, 0, 0f));
        }

        // ---- Sustained fire ----

        [Fact]
        public void Update_DeltaShorterThanInterval_AppliesNothing()
        {
            FiringCadence cadence = new(Interval, burstSize: 0, burstDelay: 0f);

            CadenceStep step = cadence.Update(Step, targetPresent: true);

            Assert.False(step.Applies);
        }

        [Fact]
        public void Update_DeltaSpanningTwoIntervals_AppliesOnce()
        {
            // the frame-hitch case: a long delta must not bank a catch-up shot
            FiringCadence cadence = new(Interval, burstSize: 0, burstDelay: 0f);

            Assert.True(cadence.Update(Interval * 3f, targetPresent: true).Applies);
            Assert.False(cadence.Update(Step, targetPresent: true).Applies);
            Assert.True(cadence.Update(Step, targetPresent: true).Applies);
        }

        [Fact]
        public void Update_ContinuousTarget_AppliesOnlyOnIntervalBoundaries()
        {
            FiringCadence cadence = new(Interval, burstSize: 0, burstDelay: 0f);

            List<float> times = ApplicationTimes(cadence, steps: 8);

            Assert.Equal(new[] { 0.5f, 1.0f, 1.5f, 2.0f }, times);
        }

        [Fact]
        public void Update_TargetLost_StopsApplyingAndReleases()
        {
            FiringCadence cadence = new(Interval, burstSize: 4, burstDelay: 2f);
            ApplicationTimes(cadence, steps: 2);
            Assert.True(cadence.IsLit);

            CadenceStep step = cadence.Update(Step, targetPresent: false);

            Assert.False(step.Applies);
            Assert.True(step.Released);
            Assert.False(cadence.IsLit);
        }

        [Fact]
        public void Release_AfterAnEarlierRelease_ReportsNothingToRelease()
        {
            // sold-then-target-lost: both routes run, only the first is a real release
            FiringCadence cadence = new(Interval, burstSize: 4, burstDelay: 2f);
            ApplicationTimes(cadence, steps: 2);

            Assert.True(cadence.Release());
            Assert.False(cadence.Release());
            Assert.False(cadence.Update(Step, targetPresent: false).Released);
        }

        // ---- Bursts ----

        [Fact]
        public void Update_BurstSizeReached_WaitsBurstDelayBeforeNextBurst()
        {
            FiringCadence cadence = new(Interval, burstSize: 3, burstDelay: 2f);

            List<float> times = ApplicationTimes(cadence, steps: 20);

            // three shots on the interval, then the 2.0s pause measured from the last of them
            Assert.Equal(new[] { 0.5f, 1.0f, 1.5f, 3.5f, 4.0f, 4.5f }, times);
        }

        [Fact]
        public void Update_BurstDelayShorterThanInterval_FiresOnIntervalNotInstantly()
        {
            // the delay replaces the inter-shot gap rather than adding to it, so a delay under the
            // interval changes nothing
            FiringCadence cadence = new(Interval, burstSize: 2, burstDelay: 0.25f);

            List<float> times = ApplicationTimes(cadence, steps: 8);

            Assert.Equal(new[] { 0.5f, 1.0f, 1.5f, 2.0f }, times);
        }

        [Fact]
        public void Update_BurstSizeZero_FiresIndefinitelyWithoutPause()
        {
            // a weapon with no burst configured: an unbroken stream at the interval
            FiringCadence cadence = new(Interval, burstSize: 0, burstDelay: 2f);

            List<float> times = ApplicationTimes(cadence, steps: 12);

            Assert.Equal(new[] { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f }, times);
        }

        [Fact]
        public void Update_TargetLostMidBurst_NextAcquisitionStartsAFreshBurst()
        {
            FiringCadence cadence = new(Interval, burstSize: 3, burstDelay: 2f);
            ApplicationTimes(cadence, steps: 4); // two of three shots away
            cadence.Update(Step, targetPresent: false);

            List<float> times = ApplicationTimes(cadence, steps: 6);

            // a full fresh burst of three. Had the count survived the loss, only its last shot would
            // fire here and the 2.0s delay would swallow the rest of the window
            Assert.Equal(3, times.Count);
        }

        [Fact]
        public void Update_BurstDelayZero_IsIndistinguishableFromNoBurst()
        {
            FiringCadence bursting = new(Interval, burstSize: 3, burstDelay: 0f);
            FiringCadence continuous = new(Interval, burstSize: 0, burstDelay: 0f);

            Assert.Equal(ApplicationTimes(continuous, steps: 16), ApplicationTimes(bursting, steps: 16));
        }

        // ---- Lit and released ----
        //
        // These edges bound a burst, not an engagement. Getting them wrong is not a damage bug and so
        // never shows up in the timings above - it shows up as a beam that strobes per shot, or one
        // that never goes out at all.

        [Fact]
        public void Update_BurstBegins_RaisesLitOnItsFirstApplicationOnly()
        {
            // Lit is an edge, not a level - a looping sound started on it must not restart per shot
            FiringCadence cadence = new(Interval, burstSize: 3, burstDelay: 2f);

            List<CadenceStep> steps = ReportedSteps(cadence, 4); // t=0.25..1.00, shots at 0.50 and 1.00

            Assert.False(steps[0].Lit);
            Assert.True(steps[1].Lit);  // t=0.50, the burst's first shot
            Assert.False(steps[2].Lit);
            Assert.False(steps[3].Lit); // t=1.00, its second - already lit
        }

        [Fact]
        public void Update_BurstCompletes_ReleasesAndStaysDarkForTheDelay()
        {
            FiringCadence cadence = new(Interval, burstSize: 3, burstDelay: 2f);

            List<CadenceStep> steps = ReportedSteps(cadence, 8); // t=0.25..2.00; the burst ends at 1.50

            Assert.True(steps[5].Applies);  // t=1.50, the third and last shot of the burst
            Assert.True(steps[5].Released);
            Assert.False(cadence.IsLit);    // still dark at t=2.00, mid-delay
        }

        [Fact]
        public void Update_BurstDelayElapses_RelightsForTheNextBurst()
        {
            FiringCadence cadence = new(Interval, burstSize: 3, burstDelay: 2f);

            List<CadenceStep> steps = ReportedSteps(cadence, 14); // t=0.25..3.50

            int lightings = steps.Count(s => s.Lit);

            Assert.True(steps[13].Applies); // t=3.50, the next burst's first shot
            Assert.True(steps[13].Lit);
            Assert.Equal(2, lightings); // one lighting per burst, not one per shot
        }

        [Fact]
        public void Update_BurstSizeZero_LitOnceAndNeverReleased()
        {
            // a weapon with no burst configured: no burst to complete, so nothing but losing the
            // target puts it out
            FiringCadence cadence = new(Interval, burstSize: 0, burstDelay: 2f);

            List<CadenceStep> steps = ReportedSteps(cadence, 12); // t=0.25..3.00, six applications

            int lightings = steps.Count(s => s.Lit);

            Assert.Equal(1, lightings);
            Assert.True(steps[1].Lit); // t=0.50, the first application
            Assert.DoesNotContain(steps, s => s.Released);
            Assert.True(cadence.IsLit);
        }

        [Fact]
        public void Update_BurstSizeOne_LitAndReleasedOnTheSameTick()
        {
            // a consumer that starts an effect on Lit and stops it on Released would otherwise hold
            // the beam for ever
            FiringCadence cadence = new(Interval, burstSize: 1, burstDelay: 2f);

            List<CadenceStep> steps = ReportedSteps(cadence, 2); // t=0.25, 0.50

            Assert.True(steps[1].Applies);
            Assert.True(steps[1].Lit);
            Assert.True(steps[1].Released);
            Assert.False(cadence.IsLit);
        }

        // ---- The firing gate ----

        [Fact]
        public void PermitsFire_ResidualInsideTolerance_Permits()
        {
            Assert.True(FiringCadence.PermitsFire(0.05f, 0.1f));
            Assert.True(FiringCadence.PermitsFire(-0.05f, 0.1f)); // sign is which way it is off, not how far
            Assert.False(FiringCadence.PermitsFire(0.2f, 0.1f));
        }

        [Fact]
        public void PermitsFire_ZeroTolerance_PermitsOnlyAnExactLock()
        {
            // impulse weapons pass tolerance 0, which must stay exactly today's lock-on behaviour
            Assert.True(FiringCadence.PermitsFire(0f, 0f));
            Assert.False(FiringCadence.PermitsFire(0.001f, 0f));
        }

        [Fact]
        public void ToleranceFor_CloserTarget_IsWider()
        {
            Assert.True(FiringCadence.ToleranceFor(0.5f, 2f) > FiringCadence.ToleranceFor(0.5f, 10f));
        }

        [Fact]
        public void ToleranceFor_WiderBeam_IsWider()
        {
            Assert.True(FiringCadence.ToleranceFor(1f, 5f) > FiringCadence.ToleranceFor(0.25f, 5f));
        }
    }
}
